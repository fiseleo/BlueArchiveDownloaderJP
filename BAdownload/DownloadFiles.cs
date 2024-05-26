
internal class DownloadFiles
{
    private static string BaseUrl;
    public static async Task downloadMain(string[] args)
    {
        string rootDirectory = Directory.GetCurrentDirectory();
        string downloadDirectory = Path.Combine(rootDirectory, "Downloads");
        string jsonFolderPath = Path.Combine(downloadDirectory, "json");
        string bundleDownloadInfoPath = Path.Combine(jsonFolderPath, "bundleDownloadInfo.json");
        string mediaCatalogPath = Path.Combine(jsonFolderPath, "MediaCatalog.json");
        string tableCatalogPath = Path.Combine(jsonFolderPath, "TableCatalog.json");
        string addressablesCatalogUrlRootPath = Path.Combine(rootDirectory, "python", "APK", "AddressablesCatalogUrlRoot.txt");
        BaseUrl = File.ReadAllText(addressablesCatalogUrlRootPath).Trim();

        if (!Directory.Exists(downloadDirectory))
        {
            Directory.CreateDirectory(downloadDirectory);
        }

        var bundleFiles = GetBundleFiles(bundleDownloadInfoPath);
        var mediaResources = GetMediaResources(mediaCatalogPath);
        var tableBundle = GetTableEntries(tableCatalogPath);

        int totalFiles = bundleFiles.Count + mediaResources.Count + tableBundle.Count;

        // HttpClient initialization
        using var httpClient = new HttpClient();

        // Download and process bundle files
        DownloadBundleFiles(bundleFiles, downloadDirectory, httpClient, totalFiles);

        // Download and process media resources
        DownloadMediaResources(mediaResources, downloadDirectory, httpClient, totalFiles);

        // Download and process table bundles
        DownloadTableBundle(tableBundle, downloadDirectory, httpClient, totalFiles);

        Console.WriteLine("Download completed.");
    }

    private static void DownloadBundleFiles(List<(string Name, long Crc)> files, string downloadDirectory, HttpClient httpClient, int totalFiles)
    {
        int currentFile = 0; // Local variable to track current file

        foreach (var file in files)
        {
            string fileName = file.Name;
            string url = $"{BaseUrl}/Android/{fileName}";
            string destination = Path.Combine(downloadDirectory, "BundleFile", fileName);
            if (File.Exists(destination))
            {
                Console.Write($"\rFile already exists locally: {destination}. Skipping download.".PadRight(Console.WindowWidth - 1));
                Console.Out.Flush();
                currentFile++;
                continue;
            }
            DownloadFile(url, destination, file.Crc, httpClient, totalFiles, currentFile);

            currentFile++;
        }
    }

    private static async Task DownloadMediaResources(List<(string FileName, long Crc, string Path)> mediaResources, string downloadDirectory, HttpClient httpClient, int totalFiles)
    {
        int currentFile = 0; // Local variable to track current file

        foreach (var mediaResource in mediaResources)
        {
            
            string mediaUrl = $"{BaseUrl}/MediaResources/{mediaResource.Path}";
            string destinationDirectory = Path.Combine(downloadDirectory, "MediaResources", mediaResource.Path);
            string destination = Path.Combine(destinationDirectory, mediaResource.FileName);
            if (File.Exists(destination))
            {
                Console.Write($"\rFile already exists locally: {destination}. Skipping download.".PadRight(Console.WindowWidth - 1));
                Console.Out.Flush();
                currentFile++;
                continue;
            }
            // Create directory if it doesn't exist
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
    
            if (DownloadFile(mediaUrl, destination, mediaResource.Crc, httpClient, totalFiles, currentFile))
            {
                currentFile++;
            }
            else
            {
                Console.WriteLine($"File {mediaResource.FileName} CRC mismatch. Deleting and re-downloading.");
                File.Delete(destination);
                DownloadFile(mediaUrl, destination, mediaResource.Crc, httpClient, totalFiles, currentFile);
            }
            currentFile++;
        }
    }
    private static void DownloadTableBundle(List<(string Name, long Crc)> files, string downloadDirectory, HttpClient httpClient, int totalFiles)
    {
        int currentFile = 0;
        foreach (var file in files)
        {
            string fileName = file.Name;
            string url = $"{BaseUrl}/TableBundles/{fileName}";
            string destination = Path.Combine(downloadDirectory, "TableBundle", fileName);
            DownloadFile(url, destination, file.Crc, httpClient, totalFiles, currentFile);
            currentFile++;
            if (File.Exists(destination))
            {
                Console.Write($"\rFile already exists locally: {destination}. Skipping download.".PadRight(Console.WindowWidth - 1));
                Console.Out.Flush();
                currentFile++;
                continue;
            }
        }

    }

     private static bool DownloadFile(string url, string destination, long expectedCrc, HttpClient httpClient, int totalFiles, int currentFile)
    {
        string directory = Path.GetDirectoryName(destination);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var response = httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result;
        response.EnsureSuccessStatusCode();
        using (var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using var httpStream = response.Content.ReadAsStreamAsync().Result;
            const int bufferSize = 8192;
            var buffer = new byte[bufferSize];
            var totalBytesRead = 0L;
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            int bytesRead;
            while ((bytesRead = httpStream.ReadAsync(buffer, 0, buffer.Length).Result) > 0)
            {
                fileStream.WriteAsync(buffer, 0, bytesRead).Wait();
                totalBytesRead += bytesRead;
                if (ConsoleIsAvailable())
                {
                    int progress = totalBytes > 0 ? (int)((totalBytesRead * 100) / totalBytes) : 100;
                    UpdateConsoleProgress(currentFile, totalFiles, progress);
                }
            }
        }
        // Check CRC
        long actualCrc = CalculateFileCrc(destination);
        if (actualCrc != expectedCrc)
        {
            Console.WriteLine($"\nCRC mismatch. Expected: {expectedCrc}, Actual: {actualCrc}. Deleting file.");
            File.Delete(destination);
            return false;
        }

        return true;
    }
    private static void UpdateConsoleProgress(int currentFile, int totalFiles, int progress)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write($"Progress ({progress}%) Total Progress ({currentFile}/{totalFiles})".PadRight(Console.WindowWidth - 1));
        Console.Out.Flush();
    }
    private static bool ConsoleIsAvailable()
    {
        if (!Console.IsOutputRedirected && !Console.IsInputRedirected && !Console.IsErrorRedirected)
        {
            try
            {
                var CursorTop = Console.CursorTop;
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    private static long CalculateFileCrc(string filePath)
    {
        using (var fileStream = File.OpenRead(filePath))
        {
            using (var crc32 = new Crc32())
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    crc32.Update(buffer, 0, bytesRead);
                }
                return crc32.Value;
            }
        }
    }

    private static List<(string Name, long Crc)> GetBundleFiles(string jsonFilePath)
    {
        List<(string, long)> data = new List<(string, long)>();

        string jsonContent = File.ReadAllText(jsonFilePath);
        dynamic jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);

        if (jsonData != null && jsonData["BundleFiles"] != null)
        {
            foreach (var bundleFile in jsonData["BundleFiles"])
            {
                string name = bundleFile["Name"];
                long crc = bundleFile["Crc"];
                data.Add((name, crc));
            }
        }

        return data;
    }

    private static List<(string FileName, long Crc, string Path)> GetMediaResources(string jsonFilePath)
    {
        List<(string, long, string)> data = new List<(string, long, string)>();

        string jsonContent = File.ReadAllText(jsonFilePath);
        dynamic jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);

        if (jsonData != null && jsonData["Table"] != null)
        {
            foreach (var mediaResource in jsonData["Table"])
            {
                string fileName = mediaResource.Value["FileName"];
                long crc = mediaResource.Value["Crc"];
                string path = mediaResource.Value["path"];
                data.Add((fileName, crc, path));
            }
        }

        return data;
    }

    private static List<(string Name, long Crc)> GetTableEntries(string jsonFilePath)
    {
        List<(string, long)> data = new List<(string, long)>();

        string jsonContent = File.ReadAllText(jsonFilePath);
        dynamic jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);

        if (jsonData != null && jsonData["Table"] != null)
        {
            foreach (var tableEntry in jsonData["Table"])
            {
                string name = tableEntry.Value["Name"];
                long crc = tableEntry.Value["Crc"];
                data.Add((name, crc));
            }
        }

        return data;
    }
}
