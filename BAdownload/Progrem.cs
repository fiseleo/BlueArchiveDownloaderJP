using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

internal class Program
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task ProgremMain(string[] args)
    {
        try
        {
            // Set console output encoding to UTF-8
            Console.OutputEncoding = Encoding.UTF8;
            string addressablesCatalogUrlRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "APK", "AddressablesCatalogUrlRoot.txt");
            string baseURL = File.ReadAllText(addressablesCatalogUrlRootPath).Trim();

            var fileMappings = new Dictionary<string, string>
            {
                { "TableCatalog.bytes", "TableBundles/TableCatalog.bytes" },
                { "bundleDownloadInfo.json", "Android/bundleDownloadInfo.json" },
                { "MediaCatalog.bytes", "MediaResources/MediaCatalog.bytes" }
            };

            var downloadTasks = new List<Task>();

            foreach (var fileMapping in fileMappings)
            {
                string fileUrl = $"{baseURL}/{fileMapping.Value}";
                string localFilePath = GetLocalFilePath(fileMapping.Key);
                Console.WriteLine($"Preparing to download file: {fileUrl}");
                Console.WriteLine($"Local file path: {localFilePath}");

                if (File.Exists(localFilePath))
                {
                    File.Delete(localFilePath);
                    Console.WriteLine($"File {localFilePath} already exists, deleted.");
                }
                else
                {
                    Console.WriteLine($"File {localFilePath} does not exist, no need to delete.");
                }

                var downloadTask = Task.Run(async () => await DownloadFileWithTimeout(fileUrl, localFilePath, TimeSpan.FromMinutes(5)));
                Console.WriteLine($"Download task added: {fileUrl}");
                downloadTasks.Add(downloadTask);
            }
            Thread.Sleep(15000);

            // Check if all files exist
            bool allFilesExist = true;
            foreach (var fileMapping in fileMappings)
            {
                string localFilePath = GetLocalFilePath(fileMapping.Key);
                if (!File.Exists(localFilePath))
                {
                    Console.WriteLine($"File {localFilePath} does not exist.");
                    string fileUrl = $"{baseURL}/{fileMapping.Value}";
                    allFilesExist = false;
                    break;
                }
            }

            if (allFilesExist)
            {
                Console.WriteLine("All files downloaded successfully.");
                await Task.WhenAll(downloadTasks);
                // Assuming Bytes.bytesMain is a valid method in your context
                Bytes.bytesMain(args);
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

    private static async Task DownloadFileWithTimeout(string fileUrl, string localFilePath, TimeSpan timeout)
    {
        try
        {
            HttpResponseMessage response = await Task.Run(() => client.GetAsync(fileUrl)).ConfigureAwait(false);
            Console.WriteLine($"Starting to download file: {fileUrl}");
            Console.WriteLine($"HTTP request status code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Processing file path: {localFilePath}");
                string directoryPath = Path.GetDirectoryName(localFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Console.WriteLine($"Directory does not exist, creating: {directoryPath}");
                    Directory.CreateDirectory(directoryPath);
                }

                Console.WriteLine($"Creating file stream: {localFilePath}");
                using (FileStream fileStream = File.Create(localFilePath))
                {
                    Console.WriteLine($"Starting to copy content to file: {localFilePath}");
                    await Task.Run(() => response.Content.CopyToAsync(fileStream)).ConfigureAwait(false);
                }

                Console.WriteLine($"File {localFilePath} downloaded successfully.");
            }
            else
            {
                Console.WriteLine($"File {localFilePath} download failed, error code: {response.StatusCode}");
            }
        }
        catch (HttpRequestException hre)
        {
            Console.WriteLine($"HTTP request error: {hre.Message}");
            Console.WriteLine(hre.StackTrace);
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
