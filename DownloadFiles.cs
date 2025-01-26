using System.Net;
using Newtonsoft.Json;
using RestSharp;
using ShellProgressBar;

namespace DownloadGameData
{
    internal class DownloadFiles
    {
        private static readonly RestClient client = new RestClient();

        public static async Task DownloadMain(string[] args)
        {
            // 提升每個主機的最大連線數量（根據需求調整）
            ServicePointManager.DefaultConnectionLimit = 100;

            // 1) 準備目錄、路徑與 baseUrl
            string rootDirectory = Directory.GetCurrentDirectory();
            string downloadDirectory = Path.Combine(rootDirectory, "Downloads");
            Directory.CreateDirectory(downloadDirectory);

            // 讀取 AddressablesCatalogUrlRoot.txt 取得 baseUrl
            string addressablesCatalogUrlRootPath = Path.Combine(rootDirectory, "Downloads", "XAPK", "Processed", "AddressablesCatalogUrlRoot.txt");
            if (!File.Exists(addressablesCatalogUrlRootPath))
            {
                Console.WriteLine($"[ERROR] 找不到檔案：{addressablesCatalogUrlRootPath}");
                return;
            }
            string baseUrl = File.ReadAllText(addressablesCatalogUrlRootPath).Trim();

            // 2) 讀取三個 JSON 拿到檔案清單
            string jsonFolderPath = Path.Combine(downloadDirectory, "json");
            string bundleDownloadInfoPath = Path.Combine(jsonFolderPath, "bundleDownloadInfo.json");
            string mediaCatalogPath = Path.Combine(jsonFolderPath, "MediaCatalog.json");
            string tableCatalogPath = Path.Combine(jsonFolderPath, "TableCatalog.json");

            var bundleFiles = GetBundleFiles(bundleDownloadInfoPath);       // (Name, Crc)
            var mediaResources = GetMediaResources(mediaCatalogPath);         // (FileName, Crc, Path)
            var tableBundles = GetTableEntries(tableCatalogPath);             // (Name, Crc)

            Console.WriteLine($"[INFO] BundleFiles: {bundleFiles.Count} items");
            Console.WriteLine($"[INFO] MediaResources: {mediaResources.Count} items");
            Console.WriteLine($"[INFO] TableBundles: {tableBundles.Count} items");

            // 3) 建立父層進度條，再建立三個子進度條
            var mainBarOptions = new ProgressBarOptions
            {
                DisplayTimeInRealTime = true,
                CollapseWhenFinished = false,
                ForegroundColor = ConsoleColor.White,
                BackgroundColor = ConsoleColor.DarkGray,
                ProgressCharacter = '─'
            };

            using var mainBar = new ProgressBar(3, "Downloading All Categories...", mainBarOptions);

            var bundleBar = mainBar.Spawn(
                maxTicks: bundleFiles.Count,
                message: "BundleFiles",
                options: new ProgressBarOptions { ForegroundColor = ConsoleColor.Yellow }
            );

            var mediaBar = mainBar.Spawn(
                maxTicks: mediaResources.Count,
                message: "MediaResources",
                options: new ProgressBarOptions { ForegroundColor = ConsoleColor.Cyan }
            );

            var tableBar = mainBar.Spawn(
                maxTicks: tableBundles.Count,
                message: "TableBundles",
                options: new ProgressBarOptions { ForegroundColor = ConsoleColor.Green }
            );

            // 4) 同時下載三大類檔案
            var downloadTasks = new List<Task>
            {
                DownloadBundleFilesAsync(bundleFiles, baseUrl, downloadDirectory, bundleBar),
                DownloadMediaResourcesAsync(mediaResources, baseUrl, downloadDirectory, mediaBar),
                DownloadTableBundlesAsync(tableBundles, baseUrl, downloadDirectory, tableBar)
            };

            await Task.WhenAll(downloadTasks);

            // 補滿父進度條
            mainBar.Tick(3);
            Console.WriteLine("[INFO] All downloads finished.");
        }

        // -----------------------------------------------------
        // (A) 下載 Bundle Files (利用並行方式與 semaphore 控制同時下載數量)
        // -----------------------------------------------------
        private static async Task DownloadBundleFilesAsync(
            List<(string Name, long Crc)> bundleFiles,
            string baseUrl,
            string downloadDirectory,
            IProgressBar bundleBar
        )
        {
            int maxParallelDownloads = 10; // 根據網路環境調整同時下載數量
            using var semaphore = new SemaphoreSlim(maxParallelDownloads);
            object progressLock = new object();

            var tasks = bundleFiles.Select(async file =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string fileUrl = $"{baseUrl}/Android/{file.Name}";
                    string localDir = Path.Combine(downloadDirectory, "BundleFile");
                    Directory.CreateDirectory(localDir);
                    string localPath = Path.Combine(localDir, file.Name);

                    lock (progressLock)
                    {
                        bundleBar.Message = $"Downloading: {file.Name}";
                    }

                    bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, file.Crc);
                    if (!ok)
                    {
                        lock (progressLock)
                        {
                            bundleBar.Message = $"[WARN] {file.Name} download or CRC failed.";
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                    lock (progressLock)
                    {
                        bundleBar.Tick();
                    }
                }
            });
            await Task.WhenAll(tasks);
        }

        // -----------------------------------------------------
        // (B) 下載 Media Resources
        // -----------------------------------------------------
        private static async Task DownloadMediaResourcesAsync(
            List<(string FileName, long Crc, string Path)> mediaResources,
            string baseUrl,
            string downloadDirectory,
            IProgressBar mediaBar
        )
        {
            int maxParallelDownloads = 10;
            using var semaphore = new SemaphoreSlim(maxParallelDownloads);
            object progressLock = new object();

            var tasks = mediaResources.Select(async media =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string fileUrl = $"{baseUrl}/MediaResources/{media.Path}";
                    string subDir = Path.GetDirectoryName(media.Path) ?? "";
                    string localDir = Path.Combine(downloadDirectory, "MediaResources", subDir);
                    Directory.CreateDirectory(localDir);
                    string localPath = Path.Combine(localDir, media.FileName);

                    lock (progressLock)
                    {
                        mediaBar.Message = $"Downloading: {media.FileName}";
                    }

                    bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, media.Crc);
                    if (!ok)
                    {
                        lock (progressLock)
                        {
                            mediaBar.Message = $"[WARN] {media.FileName} download or CRC failed.";
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                    lock (progressLock)
                    {
                        mediaBar.Tick();
                    }
                }
            });
            await Task.WhenAll(tasks);
        }

        // -----------------------------------------------------
        // (C) 下載 Table Bundles
        // -----------------------------------------------------
        private static async Task DownloadTableBundlesAsync(
            List<(string Name, long Crc)> tableBundles,
            string baseUrl,
            string downloadDirectory,
            IProgressBar tableBar
        )
        {
            int maxParallelDownloads = 10;
            using var semaphore = new SemaphoreSlim(maxParallelDownloads);
            object progressLock = new object();

            var tasks = tableBundles.Select(async tb =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string fileUrl = $"{baseUrl}/TableBundles/{tb.Name}";
                    string localDir = Path.Combine(downloadDirectory, "TableBundle");
                    Directory.CreateDirectory(localDir);
                    string localPath = Path.Combine(localDir, tb.Name);

                    lock (progressLock)
                    {
                        tableBar.Message = $"Downloading: {tb.Name}";
                    }

                    bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, tb.Crc);
                    if (!ok)
                    {
                        lock (progressLock)
                        {
                            tableBar.Message = $"[WARN] {tb.Name} download or CRC failed.";
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                    lock (progressLock)
                    {
                        tableBar.Tick();
                    }
                }
            });
            await Task.WhenAll(tasks);
        }

        // -----------------------------------------------------
        // [核心函式] 下載檔案後檢查 CRC
        // -----------------------------------------------------
        private static async Task<bool> DownloadAndCheckCrcAsync(string fileUrl, string localPath, long expectedCrc)
        {
            // 如果檔案已存在則先檢查 CRC 是否正確
            if (File.Exists(localPath))
            {
                long existingCrc = CalculateFileCrc(localPath);
                if (existingCrc == expectedCrc)
                {
                    return true; // 正確則跳過下載
                }
                else
                {
                    File.Delete(localPath);
                }
            }

            var request = new RestRequest(fileUrl, Method.Get);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful || response.RawBytes == null)
            {
                Console.WriteLine($"[ERROR] Failed: {fileUrl}, Status={response.StatusCode}");
                return false;
            }

            await File.WriteAllBytesAsync(localPath, response.RawBytes);

            long actualCrc = CalculateFileCrc(localPath);
            if (actualCrc != expectedCrc)
            {
                Console.WriteLine($"[CRC mismatch] File={Path.GetFileName(localPath)} Exp={expectedCrc}, Got={actualCrc}");
                File.Delete(localPath);
                return false;
            }
            return true;
        }

        // -----------------------------------------------------
        // 計算檔案 CRC32（使用自訂的 Crc32 類別）
        // -----------------------------------------------------
        private static long CalculateFileCrc(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            using var crc32 = new Crc32();
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                crc32.Update(buffer, 0, bytesRead);
            }
            return crc32.Value;
        }

        // -----------------------------------------------------
        // 解析 JSON 取得 BundleFiles
        // -----------------------------------------------------
        private static List<(string Name, long Crc)> GetBundleFiles(string jsonFilePath)
        {
            var data = new List<(string, long)>();
            if (!File.Exists(jsonFilePath)) return data;

            string json = File.ReadAllText(jsonFilePath);
            dynamic root = JsonConvert.DeserializeObject(json);
            if (root?.BundleFiles == null) return data;

            foreach (var bf in root.BundleFiles)
            {
                string name = bf.Name;
                long crc = bf.Crc;
                data.Add((name, crc));
            }
            return data;
        }

        // -----------------------------------------------------
        // 解析 JSON 取得 MediaResources
        // -----------------------------------------------------
        private static List<(string FileName, long Crc, string Path)> GetMediaResources(string jsonFilePath)
        {
            var data = new List<(string, long, string)>();
            if (!File.Exists(jsonFilePath)) return data;

            string json = File.ReadAllText(jsonFilePath);
            dynamic root = JsonConvert.DeserializeObject(json);
            if (root?.Table == null) return data;

            foreach (var mr in root.Table)
            {
                string fileName = mr.Value.FileName;
                long crc = mr.Value.Crc;
                string path = mr.Value.path;
                data.Add((fileName, crc, path));
            }
            return data;
        }

        // -----------------------------------------------------
        // 解析 JSON 取得 TableBundles
        // -----------------------------------------------------
        private static List<(string Name, long Crc)> GetTableEntries(string jsonFilePath)
        {
            var data = new List<(string, long)>();
            if (!File.Exists(jsonFilePath)) return data;

            string json = File.ReadAllText(jsonFilePath);
            dynamic root = JsonConvert.DeserializeObject(json);
            if (root?.Table == null) return data;

            foreach (var entry in root.Table)
            {
                string name = entry.Value.Name;
                long crc = entry.Value.Crc;
                data.Add((name, crc));
            }
            return data;
        }
    }
}
