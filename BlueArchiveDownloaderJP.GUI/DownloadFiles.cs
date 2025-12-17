using System.Net;
using Newtonsoft.Json;
using RestSharp;
using BlueArchiveGUIDownloader; // Assuming this is the namespace for DownloadProgressForm and Crc32
using System.IO; // Added for Path operations
using System.Threading.Tasks; // Added for Task
using System.Collections.Generic; // Added for List
using System.Linq; // Added for Select
using System.Threading; // Added for SemaphoreSlim
using System; // Added for Console, Action

namespace DownloadGameData
{
    internal class DownloadFiles
    {
        private static readonly RestClient client = new RestClient();

        public static async Task DownloadMain()
        {
            // Increase the maximum number of connections per host (adjust as needed)
            ServicePointManager.DefaultConnectionLimit = 100;

            // 1) Prepare directories, paths, and baseUrl
            string rootDirectory = Directory.GetCurrentDirectory();
            string downloadDirectory = Path.Combine(rootDirectory, "Downloads");
            Directory.CreateDirectory(downloadDirectory);

            // Read AddressablesCatalogUrlRoot.txt to get baseUrl
            string addressablesCatalogUrlRootPath = Path.Combine(rootDirectory, "Downloads", "XAPK", "Processed", "AddressablesCatalogUrlRoot.txt");
            if (!File.Exists(addressablesCatalogUrlRootPath))
            {
                Console.WriteLine($"[ERROR] File not found: {addressablesCatalogUrlRootPath}");
                return;
            }
            string baseUrl = File.ReadAllText(addressablesCatalogUrlRootPath).Trim();

            // 2) Read three JSON files to get the file list
            string jsonFolderPath = Path.Combine(downloadDirectory, "json");
            string bundleDownloadInfoPath = Path.Combine(jsonFolderPath, "BundlePackingInfo.json");
            string mediaCatalogPath = Path.Combine(jsonFolderPath, "MediaCatalog.json");
            string tableCatalogPath = Path.Combine(jsonFolderPath, "TableCatalog.json");

            var bundleFiles = GetBundleFiles(bundleDownloadInfoPath);       // (Name, Crc)
            var mediaResources = GetMediaResources(mediaCatalogPath);         // (FileName, Crc, Path)
            var tableBundles = GetTableEntries(tableCatalogPath);             // (Name, Crc)

            Console.WriteLine($"[INFO] BundleFiles: {bundleFiles.Count} items");
            Console.WriteLine($"[INFO] MediaResources: {mediaResources.Count} items");
            Console.WriteLine($"[INFO] TableBundles: {tableBundles.Count} items");

            var progressForm = new DownloadProgressForm();
            // Corrected mediaFiles to mediaResources
            progressForm.InitMax(bundleFiles.Count, mediaResources.Count, tableBundles.Count);
            progressForm.Show();

            // 4) Download the three types of files concurrently
            var downloadTasks = new List<Task>
            {
                DownloadBundleFilesAsync(bundleFiles, baseUrl, downloadDirectory, progressForm),
                DownloadMediaResourcesAsync(mediaResources, baseUrl, downloadDirectory, progressForm),
                DownloadTableBundlesAsync(tableBundles, baseUrl, downloadDirectory, progressForm)
            };

            await Task.WhenAll(downloadTasks);

            if (!progressForm.IsDisposed)
            {
                // Ensure UI operations are done on the UI thread
                if (progressForm.InvokeRequired)
                {
                    progressForm.Invoke((Action)(() => progressForm.Close()));
                }
                else
                {
                    progressForm.Close();
                }
            }
            Console.WriteLine("[INFO] All downloads finished.");
        }

        // -----------------------------------------------------
        // (A) Download Bundle Files (using parallelism and semaphore to control simultaneous downloads)
        // -----------------------------------------------------
        private static async Task DownloadBundleFilesAsync(
            List<(string Name, long Crc)> bundleFiles,
            string baseUrl,
            string downloadDirectory,
            DownloadProgressForm form
        )
        {
            int maxParallel = 10; // Adjust simultaneous downloads based on network environment
            using var sem = new SemaphoreSlim(maxParallel);
            var tasks = new List<Task>();
            for (int i = 0; i < bundleFiles.Count; i++)
            {
                await sem.WaitAsync();
                int currentIndex = i; // Capture the current index for the closure
                (string name, long crc) = bundleFiles[currentIndex];

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        string fileUrl = $"{baseUrl}/Android_PatchPack/{name}";
                        string localPath = Path.Combine(downloadDirectory, "BundleFiles", name);
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath) ?? ""); // Ensure directory exists
                        bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, crc);
                        if (!ok)
                        {
                            Console.WriteLine($"[WARN] {name} download or CRC failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Downloading {name}: {ex.Message}");
                    }
                    finally
                    {
                        form.ReportBundle(currentIndex + 1, $"Bundle: {name} ({currentIndex + 1}/{bundleFiles.Count})");
                        sem.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks); // Wait for all spawned tasks in this category to complete
        }

        // -----------------------------------------------------
        // (B) Download Media Resources
        // -----------------------------------------------------
        private static async Task DownloadMediaResourcesAsync(
            List<(string FileName, long Crc, string Path)> mediaResources,
            string baseUrl,
            string downloadDirectory,
            DownloadProgressForm form
        )
        {
            int maxParallel = 10;
            using var sem = new SemaphoreSlim(maxParallel);
            var tasks = new List<Task>();
            for (int i = 0; i < mediaResources.Count; i++)
            {
                await sem.WaitAsync();
                int currentIndex = i; // Capture current index
                (string fileName, long crc, string filePath) = mediaResources[currentIndex];

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        string fileUrl = $"{baseUrl}/MediaResources/{filePath}";
                        string subDir = Path.GetDirectoryName(filePath) ?? "";
                        string localDir = Path.Combine(downloadDirectory, "MediaResources", subDir);
                        Directory.CreateDirectory(localDir); // Ensure directory exists
                        string localPath = Path.Combine(localDir, fileName);
                        bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, crc);
                        if (!ok)
                        {
                            Console.WriteLine($"[WARN] {fileName} download or CRC failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Downloading {fileName}: {ex.Message}");
                    }
                    finally
                    {
                        form.ReportMedia(currentIndex + 1, $"Media: {fileName} ({currentIndex + 1}/{mediaResources.Count})");
                        sem.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks); // Wait for all spawned tasks
        }

        // -----------------------------------------------------
        // (C) Download Table Bundles
        // -----------------------------------------------------
        private static async Task DownloadTableBundlesAsync(
            List<(string Name, long Crc)> tableBundles,
            string baseUrl,
            string downloadDirectory,
            DownloadProgressForm form // Changed parameter type
        )
        {
            int maxParallel = 10;
            using var sem = new SemaphoreSlim(maxParallel);
            var tasks = new List<Task>();
            for (int i = 0; i < tableBundles.Count; i++)
            {
                await sem.WaitAsync();
                int currentIndex = i; // Capture current index
                (string name, long crc) = tableBundles[currentIndex];

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        string fileUrl = $"{baseUrl}/TableBundles/{name}";
                        string localDir = Path.Combine(downloadDirectory, "TableBundles"); // Corrected to TableBundles
                        Directory.CreateDirectory(localDir); // Ensure directory exists
                        string localPath = Path.Combine(localDir, name);

                        bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, crc);
                        if (!ok)
                        {
                            Console.WriteLine($"[WARN] {name} download or CRC failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Downloading {name}: {ex.Message}");
                    }
                    finally
                    {
                        // Use ReportTable from DownloadProgressForm
                        form.ReportTable(currentIndex + 1, $"Table: {name} ({currentIndex + 1}/{tableBundles.Count})");
                        sem.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks); // Wait for all spawned tasks
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
        // 解析 JSON 取得 BundleFiles（修正為 FullPatchPacks）
        // -----------------------------------------------------
        private static List<(string Name, long Crc)> GetBundleFiles(string jsonFilePath)
        {
            var data = new List<(string, long)>();
            if (!File.Exists(jsonFilePath)) return data;

            string json = File.ReadAllText(jsonFilePath);
            dynamic root = JsonConvert.DeserializeObject(json);
            if (root?.FullPatchPacks == null) return data;

            foreach (var bf in root.FullPatchPacks)
            {
                if (bf == null || bf.PackName == null || bf.Crc == null) continue;
                string name = bf.PackName;
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
        // 解析 JSON 取得 TableBundles（合併 Table 與 TablePack）
        // -----------------------------------------------------
        private static List<(string Name, long Crc)> GetTableEntries(string jsonFilePath)
        {
            var data = new List<(string, long)>();
            if (!File.Exists(jsonFilePath)) return data;

            string json = File.ReadAllText(jsonFilePath);
            dynamic root = JsonConvert.DeserializeObject(json);
            // Table
            if (root?.Table != null)
            {
                foreach (var entry in root.Table)
                {
                    if (entry.Value.Name == null || entry.Value.Crc == null) continue;
                    string name = entry.Value.Name;
                    long crc = entry.Value.Crc;
                    data.Add((name, crc));
                }
            }
            // TablePack
            if (root?.TablePack != null)
            {
                foreach (var entry in root.TablePack)
                {
                    if (entry.Value.Name == null || entry.Value.Crc == null) continue;
                    string name = entry.Value.Name;
                    long crc = entry.Value.Crc;
                    data.Add((name, crc));
                }
            }
            return data;
        }
    }
}
