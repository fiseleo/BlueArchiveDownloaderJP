
using Newtonsoft.Json.Linq;

class Url
{
    public static async Task UrlMain(string[] args)
    {
        string urlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", "XAPK", "Processed","url.txt");
        try
        {
            string urlContent = File.ReadAllText(urlFilePath); 
            Console.WriteLine("URL Content:");
            Console.WriteLine(urlContent);
            
            Task.Run(async () =>
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(urlContent);
                        Console.WriteLine($"Response status code: {response.StatusCode}");

                        if (response.IsSuccessStatusCode)
                        {
                            string jsonContent = await response.Content.ReadAsStringAsync();
                            JObject json = JObject.Parse(jsonContent);
                            JToken overrideGroups = json.SelectToken("ConnectionGroups[0].OverrideConnectionGroups");

                            if (overrideGroups != null && overrideGroups.HasValues)
                            {
                                bool foundSecondRoot = false;
                                foreach (var group in overrideGroups)
                                {
                                    string addressablesCatalogUrlRoot = group.Value<string>("AddressablesCatalogUrlRoot");
                                    if (!string.IsNullOrEmpty(addressablesCatalogUrlRoot))
                                    {
                                        if (foundSecondRoot)
                                        {
                                            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", "XAPK" ,"Processed", "AddressablesCatalogUrlRoot.txt");
                                            await File.WriteAllTextAsync(filePath, addressablesCatalogUrlRoot);
                                            Console.WriteLine("AddressablesCatalogUrlRoot: " + addressablesCatalogUrlRoot);
                                            break; 
                                        }
                                        else
                                        {
                                            foundSecondRoot = true; 
                                        }
                                    }
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
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }).GetAwaiter().GetResult();
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

        try
        {
            await Downloadsource.DownloadsourceMain(args);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error calling verMain: " + e.Message);
        }
    }
}