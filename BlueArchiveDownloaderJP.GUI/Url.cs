
using Newtonsoft.Json.Linq;

class Url
{
    public static async Task UrlMain()
    {
        string urlFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Downloads", "XAPK", "Processed", "url.txt");

        try
        {
            // 1. 讀取 URL
            string urlContent = await File.ReadAllTextAsync(urlFilePath);
            Console.WriteLine("URL Content:");
            Console.WriteLine(urlContent);

            // 2. 用 HttpClient 取得 JSON
            using var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(urlContent);
            Console.WriteLine($"Response status code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                // 3. 解析 JSON、找第二個 AddressablesCatalogUrlRoot
                string jsonContent = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonContent);
                var overrideGroups = json.SelectToken("ConnectionGroups[0].OverrideConnectionGroups");

                if (overrideGroups?.HasValues == true)
                {
                    bool foundSecond = false;
                    foreach (var group in overrideGroups)
                    {
                        var root = group.Value<string>("AddressablesCatalogUrlRoot");
                        if (string.IsNullOrEmpty(root)) continue;

                        if (foundSecond)
                        {
                            string outPath = Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory,
                                "Downloads", "XAPK", "Processed",
                                "AddressablesCatalogUrlRoot.txt");
                            await File.WriteAllTextAsync(outPath, root);
                            Console.WriteLine("AddressablesCatalogUrlRoot: " + root);
                            break;
                        }
                        foundSecond = true;
                    }
                }
                else
                {
                    Console.WriteLine("OverrideConnectionGroups not found in JSON.");
                }
            }
            else
            {
                Console.WriteLine($"Error: Failed to get JSON data. Status code: {response.StatusCode}");
            }
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("Error: File not found: " + e.Message);
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine("Error: Unauthorized access: " + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }

        // 4. 呼叫下一階段
        try
        {
            await Downloadsource.DownloadsourceMain();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error calling DownloadsourceMain: " + e.Message);
        }
    }
}