using System.Text;
using RestSharp;


class Downloadsource
{
    private static readonly RestClient client = new RestClient();

    public static async Task DownloadsourceMain()
    {
        try
        {
            

            // 讀取基本 URL（假設已存在一個 AddressablesCatalogUrlRoot.txt ）
            string addressablesCatalogUrlRootPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Downloads",
                "XAPK",
                "Processed",
                "AddressablesCatalogUrlRoot.txt"
            );
            string baseURL = File.ReadAllText(addressablesCatalogUrlRootPath).Trim();

            // 準備要下載的檔案對應
            var fileMappings = new Dictionary<string, string>
                {
                    { "TableCatalog.bytes",      "TableBundles/TableCatalog.bytes" },
                    { "bundleDownloadInfo.json", "Android/bundleDownloadInfo.json" },
                    { "MediaCatalog.bytes",      "MediaResources/Catalog/MediaCatalog.bytes" }
                };

            // 建立一個串列來裝所有下載任務
            var downloadTasks = new List<Task>();

            // 逐一建立下載工作
            foreach (var fileMapping in fileMappings)
            {
                string fileUrl = $"{baseURL}/{fileMapping.Value}";
                string localFilePath = GetLocalFilePath(fileMapping.Key);

                Console.WriteLine($"Preparing to download file: {fileUrl}");
                Console.WriteLine($"Local file path: {localFilePath}");

                // 如果本地檔案已經存在，刪除
                if (File.Exists(localFilePath))
                {
                    File.Delete(localFilePath);
                    Console.WriteLine($"File {localFilePath} already exists, deleted.");
                }
                else
                {
                    Console.WriteLine($"File {localFilePath} does not exist, no need to delete.");
                }

                // 新增非同步下載任務
                downloadTasks.Add(DownloadFileWithRestSharp(fileUrl, localFilePath));
            }

            // 等待所有下載執行完畢
            Console.WriteLine("Waiting for all downloads to complete...");
            await Task.WhenAll(downloadTasks);

            // 確認檔案是否都下載成功
            bool allFilesExist = true;
            foreach (var fileMapping in fileMappings)
            {
                string localFilePath = GetLocalFilePath(fileMapping.Key);
                if (!File.Exists(localFilePath))
                {
                    Console.WriteLine($"File {localFilePath} does not exist.");
                    allFilesExist = false;
                }
            }

            if (allFilesExist)
            {
                Console.WriteLine("All files downloaded successfully.");
                
                await Processedbytes.ProcessedbytesMain();
            }
            else
            {
                Console.WriteLine("Downloaded files are incomplete, please check and retry.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static async Task DownloadFileWithRestSharp(string fileUrl, string localFilePath)
    {
        try
        {
            Console.WriteLine($"Starting to download file: {fileUrl}");

            var request = new RestRequest(fileUrl, Method.Get);
            RestResponse response = await client.ExecuteAsync(request);

            // 檢查是否下載成功
            if (response.IsSuccessful)
            {
                Console.WriteLine($"Processing file path: {localFilePath}");
                string directoryPath = Path.GetDirectoryName(localFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Console.WriteLine($"Directory does not exist, creating: {directoryPath}");
                    Directory.CreateDirectory(directoryPath);
                }

                Console.WriteLine($"Writing content to file: {localFilePath}");
                await File.WriteAllBytesAsync(localFilePath, response.RawBytes);
                Console.WriteLine($"File {localFilePath} downloaded successfully.");
            }
            else
            {
                Console.WriteLine($"File {localFilePath} download failed, error code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred while downloading file {fileUrl}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static string GetLocalFilePath(string fileName)
    {
        try
        {
            // 依副檔名決定存放在 bytes/ 或 json/ 資料夾
            string subDirectory = fileName.EndsWith(".bytes") ? "bytes" : "json";
            string localFilePath = Path.Combine("Downloads", subDirectory, fileName);

            string directoryPath = Path.GetDirectoryName(localFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            return localFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred while generating local file path: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }
}

