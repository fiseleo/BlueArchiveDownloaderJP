using Newtonsoft.Json;
using RestSharp;
using ShellProgressBar;

internal class DownloadFiles
{
    private static readonly RestClient client = new RestClient();

    public static async Task DownloadMain(string[] args)
    {
        // 1) 準備目錄、路徑與 baseUrl
        string rootDirectory = Directory.GetCurrentDirectory();
        string downloadDirectory = Path.Combine(rootDirectory, "Downloads");
        Directory.CreateDirectory(downloadDirectory);

        string addressablesCatalogUrlRootPath = Path.Combine(rootDirectory, "Downloads", "XAPK", "Processed", "AddressablesCatalogUrlRoot.txt");
        string baseUrl = File.ReadAllText(addressablesCatalogUrlRootPath).Trim();

        // 2) 讀三個 JSON 拿到檔案清單
        string jsonFolderPath        = Path.Combine(downloadDirectory, "json");
        string bundleDownloadInfoPath= Path.Combine(jsonFolderPath, "bundleDownloadInfo.json");
        string mediaCatalogPath      = Path.Combine(jsonFolderPath, "MediaCatalog.json");
        string tableCatalogPath      = Path.Combine(jsonFolderPath, "TableCatalog.json");

        var bundleFiles   = GetBundleFiles(bundleDownloadInfoPath);    // (Name, Crc)
        var mediaResources= GetMediaResources(mediaCatalogPath);       // (FileName, Crc, Path)
        var tableBundles  = GetTableEntries(tableCatalogPath);         // (Name, Crc)

        Console.WriteLine($"[INFO] BundleFiles: {bundleFiles.Count} items");
        Console.WriteLine($"[INFO] MediaResources: {mediaResources.Count} items");
        Console.WriteLine($"[INFO] TableBundles: {tableBundles.Count} items");

        // === 3) 用「父層進度條」生成 3 個「子層進度條」 ===
        // 如果不想要父層，可不使用，直接建立三個 ProgressBar 也行
        var mainBarOptions = new ProgressBarOptions
        {
            DisplayTimeInRealTime = true,
            CollapseWhenFinished  = false,
            ForegroundColor       = ConsoleColor.White,
            BackgroundColor       = ConsoleColor.DarkGray,
            ProgressCharacter     = '─'
        };

        // 父層的目標總數，可隨意設定(這裡用3代表3大類)
        using var mainBar = new ProgressBar(3, "Downloading All Categories...", mainBarOptions);

        // Spawn出三個子條
        var bundleBar = mainBar.Spawn(
            maxTicks: bundleFiles.Count, 
            message: "BundleFiles",
            new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow
            }
        );

        var mediaBar = mainBar.Spawn(
            maxTicks: mediaResources.Count, 
            message: "MediaResources",
            new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Cyan
            }
        );

        var tableBar = mainBar.Spawn(
            maxTicks: tableBundles.Count, 
            message: "TableBundles",
            new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Green
            }
        );

        // 4) 讓三類檔案「同時」下載 (平行執行)
        //   => 只要把對應的 childBar 傳進去即可
        var downloadTasks = new List<Task>
        {
            DownloadBundleFilesAsync(bundleFiles, baseUrl, downloadDirectory, bundleBar),
            DownloadMediaResourcesAsync(mediaResources, baseUrl, downloadDirectory, mediaBar),
            DownloadTableBundlesAsync(tableBundles, baseUrl, downloadDirectory, tableBar)
        };

        await Task.WhenAll(downloadTasks);

        // 所有任務完成後，可以把父層的進度補到 3/3
        mainBar.Tick(3);

        Console.WriteLine("[INFO] All downloads finished.");
    }

    // -----------------------------------------------------------------------------------
    // (A) 下載 BUNDLE FILES (以子進度條 bundleBar 來顯示進度)
    // -----------------------------------------------------------------------------------
    private static async Task DownloadBundleFilesAsync(
        List<(string Name, long Crc)> bundleFiles,
        string baseUrl,
        string downloadDirectory,
        IProgressBar bundleBar
    )
    {
        foreach (var file in bundleFiles)
        {
            string fileUrl  = $"{baseUrl}/Android/{file.Name}";
            string localDir = Path.Combine(downloadDirectory, "BundleFile");
            Directory.CreateDirectory(localDir);
            string localPath= Path.Combine(localDir, file.Name);

            bundleBar.Message = $"Downloading: {file.Name}"; // 顯示目前下載的檔名

            bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, file.Crc);
            if (!ok)
            {
                bundleBar.Message = $"[WARN] {file.Name} download or CRC failed.";
            }

            bundleBar.Tick(); // 下載完一個檔就 +1
        }
    }

    // -----------------------------------------------------------------------------------
    // (B) 下載 MEDIA RESOURCES
    // -----------------------------------------------------------------------------------
    private static async Task DownloadMediaResourcesAsync(
        List<(string FileName, long Crc, string Path)> mediaResources,
        string baseUrl,
        string downloadDirectory,
        IProgressBar mediaBar
    )
    {
        foreach (var media in mediaResources)
        {
            string fileUrl    = $"{baseUrl}/MediaResources/{media.Path}";
            string subDir     = Path.GetDirectoryName(media.Path) ?? "";
            string localDir   = Path.Combine(downloadDirectory, "MediaResources", subDir);
            Directory.CreateDirectory(localDir);
            string localPath  = Path.Combine(localDir, media.FileName);

            mediaBar.Message = $"Downloading: {media.FileName}"; // 顯示目前下載的檔名

            bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, media.Crc);
            if (!ok)
            {
                mediaBar.Message = $"[WARN] {media.FileName} download or CRC failed.";
            }

            mediaBar.Tick();
        }
    }

    // -----------------------------------------------------------------------------------
    // (C) 下載 TABLE BUNDLES
    // -----------------------------------------------------------------------------------
    private static async Task DownloadTableBundlesAsync(
        List<(string Name, long Crc)> tableBundles,
        string baseUrl,
        string downloadDirectory,
        IProgressBar tableBar
    )
    {
        foreach (var tb in tableBundles)
        {
            string fileUrl   = $"{baseUrl}/TableBundles/{tb.Name}";
            string localDir  = Path.Combine(downloadDirectory, "TableBundle");
            Directory.CreateDirectory(localDir);
            string localPath = Path.Combine(localDir, tb.Name);

            tableBar.Message = $"Downloading: {tb.Name}"; // 顯示目前下載的檔名

            bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, tb.Crc);
            if (!ok)
            {
                tableBar.Message = $"[WARN] {tb.Name} download or CRC failed.";
            }

            tableBar.Tick();
        }
    }

    // -----------------------------------------------------------------------------------
    // [核心] 用 RestSharp 抓下檔案，寫到 localPath，再檢查 CRC
    // -----------------------------------------------------------------------------------
    private static async Task<bool> DownloadAndCheckCrcAsync(string fileUrl, string localPath, long expectedCrc)
    {
        // 若已存在，先檢查 CRC 是否吻合
        if (File.Exists(localPath))
        {
            long existingCrc = CalculateFileCrc(localPath);
            if (existingCrc == expectedCrc)
            {
                return true; // 直接跳過下載
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

        // 寫入本地檔
        await File.WriteAllBytesAsync(localPath, response.RawBytes);

        // 檢查 CRC
        long actualCrc = CalculateFileCrc(localPath);
        if (actualCrc != expectedCrc)
        {
            Console.WriteLine($"[CRC mismatch] File={Path.GetFileName(localPath)} Exp={expectedCrc}, Got={actualCrc}");
            File.Delete(localPath);
            return false;
        }
        return true;
    }

    // -----------------------------------------------------------------------------------
    // 計算檔案 CRC32 (自行實作或使用你現有的類別)
    // -----------------------------------------------------------------------------------
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

    // -----------------------------------------------------------------------------------
    // 解析三個 JSON
    // -----------------------------------------------------------------------------------
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
            long crc    = bf.Crc;
            data.Add((name, crc));
        }
        return data;
    }

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
            long crc        = mr.Value.Crc;
            string path     = mr.Value.path;
            data.Add((fileName, crc, path));
        }
        return data;
    }

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
            long crc    = entry.Value.Crc;
            data.Add((name, crc));
        }
        return data;
    }
}
