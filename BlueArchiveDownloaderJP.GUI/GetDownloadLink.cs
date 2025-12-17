using System.Text.Json;
using Crypto;

class GetDownloadLink
{
    public static async Task GetDownloadLinkMain()
    {
        try
        {
            string url = ServerInfoDataUrl();
            Console.WriteLine("Extracted URL: " + url);
            await Url.UrlMain();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

    }

    public static string ServerInfoDataUrl()
    {
        // 1. 從 GameMainConfig 獲取資料
        byte[] data = GameMainConfig();

        // 2. 將資料進行 Base64 編碼
        string base64EncodedData = Convert.ToBase64String(data);

        // 3. 使用密鑰加密轉換
        string encryptedJson = TableEncryptionService.Convert(base64EncodedData, TableEncryptionService.CreateKey("GameMainConfig"));

        // 4. 解析 JSON 並提取目標值
        Dictionary<string, string> jsonObject = JsonSerializer.Deserialize<Dictionary<string, string>>(encryptedJson);
        string encryptedValue = jsonObject["X04YXBFqd3ZpTg9cKmpvdmpOElwnamB2eE4cXDZqc3ZgTg=="];

        // 5. 解密 URL
        string url = TableEncryptionService.Convert(encryptedValue, TableEncryptionService.CreateKey("ServerInfoDataUrl"));

        // 6. 將 URL 寫入檔案
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string UrlFolder = Path.Combine(currentDirectory, "Downloads", "XAPK", "Processed");
        if (!Directory.Exists(UrlFolder))
        {
            Directory.CreateDirectory(UrlFolder);
        }
        string filePath = Path.Combine(UrlFolder, "url.txt");
        File.WriteAllText(filePath, url);

        Console.WriteLine("URL extracted successfully and saved to url.txt!");
        return url;
    }

    // 模擬 GameMainConfig() 的功能，應替換為實際實現
    public static byte[] GameMainConfig()
    {
        string rootDirectory = Directory.GetCurrentDirectory();
        string GameMainConfigPath = Path.Combine(rootDirectory, "Downloads", "XAPK", "Processed");
        string GameMainConfigFile = Path.Combine(GameMainConfigPath, "GameMainConfig");

        // 模擬返回的 byte array，應替換為實際實現
        return File.ReadAllBytes(GameMainConfigFile);
    }
}