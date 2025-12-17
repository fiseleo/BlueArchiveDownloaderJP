
using System.IO.Compression;

class UnXAPK
{
    public static async Task UnXAPKMain(string[] args)
    {
        string rootDirectory = Directory.GetCurrentDirectory();
        string downloadPath = Path.Combine(rootDirectory, "Downloads", "XAPK");
        if (!Directory.Exists(downloadPath))
        {
            Console.WriteLine("XAPK download directory does not exist; please download the XAPK file first.");
            return;
        }

        try
        {
            // 解壓縮目的資料夾 (Unzip)
            // 建議只用 downloadPath 再加 "Unzip"，不需重複合併 rootDirectory
            string extractionPath = Path.Combine(downloadPath, "Unzip");
            if (!Directory.Exists(extractionPath))
            {
                Directory.CreateDirectory(extractionPath);
            }

            // 尋找第一個 .xapk 檔案
            string xapkFile = Directory
                .GetFiles(downloadPath, "*.xapk", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (xapkFile == null)
            {
                Console.WriteLine("No .xapk files found.");
                return;
            }

            // 解壓縮 XAPK
            if (await UnpackZip(xapkFile, extractionPath))
            {
                // 若 XAPK 解壓後出現 .apk，繼續解壓
                foreach (var apkFile in Directory.GetFiles(extractionPath, "*.apk", SearchOption.TopDirectoryOnly))
                {
                    if (!await UnpackZip(apkFile, extractionPath))
                    {
                        Console.WriteLine($"Error unpacking apk file: {apkFile}");
                        return;
                    }
                }
            }
            else
            {
                Console.WriteLine("Error unpacking xapk file");
                return;
            }

            Console.WriteLine("APK extracted successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        await Hex.HexMain(args);
    }

    private static async Task<bool> UnpackZip(string zipFile, string extractionPath)
    {
        try
        {
            Directory.CreateDirectory(extractionPath);

            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryFullName = Path.Combine(extractionPath, entry.FullName);
                    string entryDirectory = Path.GetDirectoryName(entryFullName);

                    if (string.IsNullOrEmpty(entryDirectory))
                        continue; // 若 entry 是純資料夾 (空字串) 就略過

                    if (!Directory.Exists(entryDirectory))
                        Directory.CreateDirectory(entryDirectory);

                    if (File.Exists(entryFullName))
                    {
                        Console.WriteLine($"Skipped: {entryFullName} already exists.");
                        continue;
                    }

                    entry.ExtractToFile(entryFullName);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
        finally
        {
            // 即使這裡用不上 async-await 實際非同步IO，但保留方法簽名以方便流程統一
            await Task.CompletedTask;
        }
    }
}
