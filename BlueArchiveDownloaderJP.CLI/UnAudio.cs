using Crypto;
using ICSharpCode.SharpZipLib.Zip;
namespace BAdownload
{
    public class UnAudio
    {
        public static async Task Audio()
        {
            string rootDirectory = Directory.GetCurrentDirectory();
            var AudioPath = Path.Combine(rootDirectory, "Downloads", "MediaResources", "GameData", "Audio", "VOC_JP");
            await Extract_Audio(AudioPath);
            if (!Directory.Exists(AudioPath))
            {
                Console.WriteLine("Audio path not found.");
                return;
            }

        }

        public static async Task Extract_Audio(string path)
        {
            var zipFiles = Directory.GetFiles(path, "*.zip");
            foreach ( var zipFile in zipFiles)
            {
                string fileNameNoExt = Path.GetFileName(zipFile).ToLowerInvariant();
                byte[] pwdBytes = TableService.CreatePassword(fileNameNoExt, 20);
                string password = Convert.ToBase64String(pwdBytes);
                using var fs = File.OpenRead(zipFile);
                using var zip = new ZipInputStream(fs)
                {

                    Password = password  // 設定解密密碼
                };
                Console.WriteLine($"Extracting {zipFile} ...");
                Console.WriteLine($"Password: {password}");
                var folderName = Path.GetFileNameWithoutExtension(zipFile);
                string extractRoot = Path.Combine(path, folderName);

                ZipEntry entry;
                while ((entry = zip.GetNextEntry()) != null)
                {
                    if (entry.IsDirectory)
                        continue;

                    string outFile = Path.Combine(extractRoot, entry.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);

                    // 非同步複製
                    await using var outFs = File.Create(outFile);
                    await zip.CopyToAsync(outFs);
                }

                // 關閉 ZipInputStream
                File.Delete(zipFile);
            }
        }
    }
}