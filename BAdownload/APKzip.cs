using System;
using System.IO;
using System.IO.Compression;

class APKzip
{
    public static async void zipMain(string[] args)
    {
        string downloadedApkRelativePath = @"python\APK\com.YostarJP.BlueArchive.apk";
        string extractionRelativePath = @"python\APK\unzip";

        try
        {
            string currentDirectory = Environment.CurrentDirectory;
            string downloadedApkFilePath = Path.Combine(currentDirectory, downloadedApkRelativePath);
            string extractionPath = Path.Combine(currentDirectory, extractionRelativePath);

            Directory.CreateDirectory(extractionPath);

            using (ZipArchive archive = ZipFile.OpenRead(downloadedApkFilePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryFullName = Path.Combine(extractionPath, entry.FullName);
                    string entryDirectory = Path.GetDirectoryName(entryFullName);
                    
                    if (string.IsNullOrEmpty(entryDirectory))
                        continue; // Skip if the entry is for directory

                    if (!Directory.Exists(entryDirectory))
                        Directory.CreateDirectory(entryDirectory);

                    if (File.Exists(entryFullName))
                    {
                        Console.WriteLine($"Skipped: {entryFullName} already exists.");
                        continue; // Skip if the file already exists
                    }

                    entry.ExtractToFile(entryFullName);
                }
            }

            Console.WriteLine("APK extracted successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        await APKver.verMain(args);
    }
}

