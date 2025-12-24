using System.Net;
using Newtonsoft.Json;
using RestSharp;
using BlueArchiveGUIDownloader;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;

namespace DownloadGameData
{
    internal class DownloadFiles
    {
        private static readonly RestClient client = new RestClient();

        public static async Task DownloadMain()
        {
            ServicePointManager.DefaultConnectionLimit = 100;

            string rootDirectory = Directory.GetCurrentDirectory();
            string downloadDirectory = Path.Combine(rootDirectory, "Downloads");
            Directory.CreateDirectory(downloadDirectory);

            string addressablesCatalogUrlRootPath = Path.Combine(rootDirectory, "Downloads", "XAPK", "Processed", "AddressablesCatalogUrlRoot.txt");
            if (!File.Exists(addressablesCatalogUrlRootPath))
            {
                Console.WriteLine($"[ERROR] File not found: {addressablesCatalogUrlRootPath}");
                return;
            }
            string baseUrl = File.ReadAllText(addressablesCatalogUrlRootPath).Trim();

            string jsonFolderPath = Path.Combine(downloadDirectory, "json");
            string bundleDownloadInfoPath = Path.Combine(jsonFolderPath, "BundlePackingInfo.json");
            string mediaCatalogPath = Path.Combine(jsonFolderPath, "MediaCatalog.json");
            string tableCatalogPath = Path.Combine(jsonFolderPath, "TableCatalog.json");

            var bundleFiles = GetBundleFiles(bundleDownloadInfoPath);
            var mediaResources = GetMediaResources(mediaCatalogPath);
            var tableBundles = GetTableEntries(tableCatalogPath);

            Console.WriteLine($"[INFO] BundleFiles: {bundleFiles.Count} items");
            Console.WriteLine($"[INFO] MediaResources: {mediaResources.Count} items");
            Console.WriteLine($"[INFO] TableBundles: {tableBundles.Count} items");

            var downloadTasks = new List<Task>
            {
                DownloadBundleFilesAsync(bundleFiles, baseUrl, downloadDirectory),
                DownloadMediaResourcesAsync(mediaResources, baseUrl, downloadDirectory),
                DownloadTableBundlesAsync(tableBundles, baseUrl, downloadDirectory)
            };

            await Task.WhenAll(downloadTasks);

            Console.WriteLine("[INFO] All downloads finished.");
        }

        private static async Task DownloadBundleFilesAsync(
            List<(string Name, long Crc)> bundleFiles,
            string baseUrl,
            string downloadDirectory
        )
        {
            int maxParallel = 10;
            using var sem = new SemaphoreSlim(maxParallel);
            var tasks = new List<Task>();
            for (int i = 0; i < bundleFiles.Count; i++)
            {
                await sem.WaitAsync();
                int currentIndex = i;
                (string name, long crc) = bundleFiles[currentIndex];

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        string fileUrl = $"{baseUrl}/Android_PatchPack/{name}";
                        string localPath = Path.Combine(downloadDirectory, "BundleFiles", name);
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath) ?? "");
                        bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, crc);
                        if (!ok)
                        {
                            Console.WriteLine($"[WARN] {name} download or CRC failed.");
                        }
                        else
                        {
                            Console.WriteLine($"[INFO] Bundle: {name} ({currentIndex + 1}/{bundleFiles.Count})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Downloading {name}: {ex.Message}");
                    }
                    finally
                    {
                        sem.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks);
        }

        private static async Task DownloadMediaResourcesAsync(
            List<(string FileName, long Crc, string Path)> mediaResources,
            string baseUrl,
            string downloadDirectory
        )
        {
            int maxParallel = 10;
            using var sem = new SemaphoreSlim(maxParallel);
            var tasks = new List<Task>();
            for (int i = 0; i < mediaResources.Count; i++)
            {
                await sem.WaitAsync();
                int currentIndex = i;
                (string fileName, long crc, string filePath) = mediaResources[currentIndex];

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        string fileUrl = $"{baseUrl}/MediaResources/{filePath}";
                        string subDir = Path.GetDirectoryName(filePath) ?? "";
                        string localDir = Path.Combine(downloadDirectory, "MediaResources", subDir);
                        Directory.CreateDirectory(localDir);
                        string localPath = Path.Combine(localDir, fileName);
                        bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, crc);
                        if (!ok)
                        {
                            Console.WriteLine($"[WARN] {fileName} download or CRC failed.");
                        }
                        else
                        {
                            Console.WriteLine($"[INFO] Media: {fileName} ({currentIndex + 1}/{mediaResources.Count})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Downloading {fileName}: {ex.Message}");
                    }
                    finally
                    {
                        sem.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks);
        }

        private static async Task DownloadTableBundlesAsync(
            List<(string Name, long Crc)> tableBundles,
            string baseUrl,
            string downloadDirectory
        )
        {
            int maxParallel = 10;
            using var sem = new SemaphoreSlim(maxParallel);
            var tasks = new List<Task>();
            for (int i = 0; i < tableBundles.Count; i++)
            {
                await sem.WaitAsync();
                int currentIndex = i;
                (string name, long crc) = tableBundles[currentIndex];

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        string fileUrl = $"{baseUrl}/TableBundles/{name}";
                        string localDir = Path.Combine(downloadDirectory, "TableBundles");
                        Directory.CreateDirectory(localDir);
                        string localPath = Path.Combine(localDir, name);

                        bool ok = await DownloadAndCheckCrcAsync(fileUrl, localPath, crc);
                        if (!ok)
                        {
                            Console.WriteLine($"[WARN] {name} download or CRC failed.");
                        }
                        else
                        {
                            Console.WriteLine($"[INFO] Table: {name} ({currentIndex + 1}/{tableBundles.Count})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Downloading {name}: {ex.Message}");
                    }
                    finally
                    {
                        sem.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks);
        }

        private static async Task<bool> DownloadAndCheckCrcAsync(string fileUrl, string localPath, long expectedCrc)
        {
            if (File.Exists(localPath))
            {
                long existingCrc = CalculateFileCrc(localPath);
                if (existingCrc == expectedCrc)
                {
                    return true;
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

        private static List<(string Name, long Crc)> GetTableEntries(string jsonFilePath)
        {
            var data = new List<(string, long)>();
            if (!File.Exists(jsonFilePath)) return data;

            string json = File.ReadAllText(jsonFilePath);
            dynamic root = JsonConvert.DeserializeObject(json);
            
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
