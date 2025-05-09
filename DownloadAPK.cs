using System.Diagnostics;
using PuppeteerSharp;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;


namespace BAdownload
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 解析參數：假設程式呼叫方式為
            // BAdownload.exe -f 1.456789
            // 則需要抓取 -f 後的值來組合下載連結。
            bool reDownload = false;
            string rootDirectory = Directory.GetCurrentDirectory();
            if (!Directory.Exists(Path.Combine(rootDirectory, "Downloads", "XAPK")))
            {
                Directory.CreateDirectory(Path.Combine(rootDirectory, "Downloads", "XAPK"));
            }

            var downloadPath = Path.Combine(rootDirectory, "Downloads", "XAPK");
            if (Directory.Exists(downloadPath))
            {
                var files = Directory.GetFiles(downloadPath);
                foreach (var file in files)
                {
                    if (file.EndsWith(".xapk"))
                    {
                        continue;
                    }
                    else
                    {
                        File.Delete(file);
                    }
                }
            }
            string versionArg = null;
            bool directDownload = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-f", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    versionArg = args[i + 1];
                    i++;
                }
                else if (args[i].Equals("-d", StringComparison.OrdinalIgnoreCase))
                {
                    directDownload = true;
                }
                else if (args[i].Equals("-r", StringComparison.OrdinalIgnoreCase))
                {
                    reDownload = true;
                }
            }
            if (directDownload && !reDownload)
            {
                Console.WriteLine(
                    "detected download releated argument attached,but not added redownload argument,so skip redownload apk if xapk existed"
                );
            }

            bool xapkExists = false;
            string existingXapkFile = null;
            if (Directory.Exists(downloadPath))
            {
                var xapkFiles = Directory.GetFiles(downloadPath, "*.xapk");
                if (xapkFiles.Length > 0)
                {
                    xapkExists = true;
                    existingXapkFile = xapkFiles[0];
                    Console.WriteLine($"發現已存在的XAPK檔案: {Path.GetFileName(existingXapkFile)}");

                    if (!reDownload)
                    {
                        Console.WriteLine("跳過下載，直接使用現有檔案。");
                        foreach (var dir in new[] { "Unzip", "Processed" })
                        {
                            var path = Path.Combine(rootDirectory, "Downloads", "XAPK", dir);
                            if (Directory.Exists(path))
                                Directory.Delete(path, true);
                        }
                        await UnXAPK.UnXAPKMain(args);
                        return;
                    }
                    else
                    {
                        Console.WriteLine("偵測到-r參數，將刪除現有XAPK檔案並重新下載。");
                        File.Delete(existingXapkFile);
                        //also delete Unzip and Processed folder
                        foreach (var dir in new[] { "Unzip", "Processed" })
                        {
                            var path = Path.Combine(rootDirectory, "Downloads", "XAPK", dir);
                            if (Directory.Exists(path))
                                Directory.Delete(path, true);
                        }
                    }
                }
            }

            // 根據是否有 versionArg 來決定組合的網址
            string downloadUrl;
            if (!string.IsNullOrEmpty(versionArg) && versionArg.StartsWith("1."))
            {
                // 取小數點後的部分。例如 "1.53.323417" 只取 "323417"
                var versionCode = versionArg.Substring(5); // 從 index=3 開始擷取
                downloadUrl = $"https://d.apkpure.com/b/XAPK/com.YostarJP.BlueArchive?versionCode={versionCode}&nc=arm64-v8a&sv=24";
            }
            else
            {
                // 沒有指定 -f 參數，或格式不符合，改用 latest
                downloadUrl = "https://d.apkpure.com/b/XAPK/com.YostarJP.BlueArchive?version=latest";
            }

            Console.WriteLine($"準備下載網址: {downloadUrl}");

            if (directDownload)
            {
                // 1. 下載並解析 Protobuf → JSON (這部分您已提供，此處示意性保留)
                const string pbUrl =
                    "https://api.pureapk.com/m/v3/cms/app_version?hl=en-US&package_name=com.YostarJP.BlueArchive";
                using var http = new HttpClient();
                http.Timeout = Timeout.InfiniteTimeSpan;
                http.DefaultRequestHeaders.Add("x-sv", "29");
                http.DefaultRequestHeaders.Add("x-abis", "arm64-v8a,armeabi-v7a,armeabi");
                http.DefaultRequestHeaders.Add("x-gp", "1");

                Console.WriteLine("Downloading response.pb ...");
                var pbBytes = await http.GetByteArrayAsync(pbUrl);
                // 假設 response.pb 已成功下載並寫入檔案
                // await File.WriteAllBytesAsync("response.pb", pbBytes); // 您的原始碼已有
                Console.WriteLine("Saved response.pb");

                var protocPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "protoc.exe");
                var psi = new ProcessStartInfo
                {
                    FileName = protocPath,
                    Arguments = "--decode_raw",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Console.WriteLine("Decoding with protoc --decode_raw ...");
                using var proc = Process.Start(psi);
                await proc.StandardInput.BaseStream.WriteAsync(pbBytes, 0, pbBytes.Length);
                proc.StandardInput.Close();
                string textProto = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();
                // await File.WriteAllTextAsync("response.txt", textProto); // 您的原始碼已有

                // 2. 解析到 JObject (ParseMessage 函數您已提供)
                var lines = textProto.Split('\n');
                int idx = 0;
                JObject root = ParseMessage(lines, ref idx, 0); // ParseMessage 是您提供的本地函數
                // await File.WriteAllTextAsync("response.json", root.ToString()); // 您的原始碼已有
                Console.WriteLine("Generated response.json");

                // 3. 從 JSON 中擷取 version_list
                var array = root.SelectToken("$['1']['7']['2']") as JArray;

                if (array == null)
                {
                    Console.Error.WriteLine("無法從 response.json 解析 version_list (路徑 $['1']['7']['2'] 無效或不存在)");
                    return;
                }

                var entries = array
                    .Select(item =>
                    {
                        var itemObj = item as JObject;
                        if (itemObj == null) return null;

                        var threeToken = itemObj["3"] as JObject;
                        if (threeToken == null) return null;
                        
                        var info = threeToken["2"] as JObject;
                        if (info == null) return null;

                        var versionToken = info["6"];
                        // 修正路徑以正確獲取 XAPK URL
                        var urlToken = info?["24"]?["9"]; 

                        if (versionToken == null || urlToken == null)
                        {
                            // Console.WriteLine("Warning: versionToken or urlToken is null. Skipping entry.");
                            return null;
                        }
                         if (urlToken.Type != JTokenType.String)
                        {
                            // Console.WriteLine($"Warning: urlToken is not a string: {urlToken.ToString()}. Skipping entry.");
                            return null;
                        }

                        string versionStr = null;
                        if (versionToken.Type == JTokenType.String)
                        {
                            versionStr = versionToken.Value<string>();
                        }
                        else
                        {
                            // 如果版本號不是直接的字串 (例如您 response.json 中看到的 {"6": ["0", "54"]} 結構)
                            // 則此處的簡易提取會失敗。您可以選擇跳過這些條目或實現更複雜的解析邏輯。
                            // 根據您的需求 (例如 "1.23.456789")，我們優先處理標準字串格式的版本。
                            // Console.WriteLine($"Warning: Version for an entry is not a direct string: {versionToken.ToString()}. Skipping entry.");
                            return null; 
                        }

                        if (string.IsNullOrEmpty(versionStr))
                        {
                            // Console.WriteLine("Warning: Extracted version string is null or empty. Skipping entry.");
                            return null;
                        }
                        
                        // 驗證版本號格式是否為 X.Y.Z... 且各部分為數字
                        var versionPartsTemp = versionStr.Split('.');
                        if (versionPartsTemp.Length < 2) { // 至少需要 主版本.次版本
                             // Console.WriteLine($"Warning: Version string '{versionStr}' does not have at least Major.Minor parts. Skipping.");
                             return null;
                        }
                        foreach(var part in versionPartsTemp) {
                            if (!int.TryParse(part, out _)) {
                                // Console.WriteLine($"Warning: Version part '{part}' in '{versionStr}' is not an integer. Skipping.");
                                return null;
                            }
                        }
                        
                        string url = urlToken.Value<string>();
                        return new { Version = versionStr, Url = url };
                    })
                    .Where(x => x != null && !string.IsNullOrEmpty(x.Url) && x.Url.StartsWith("https://download.pureapk.com/b/XAPK/"))
                    .ToList();

                if (!entries.Any())
                {
                    Console.Error.WriteLine("在 response.json 中找不到任何有效的 XAPK 下載連結或版本資訊。");
                    return;
                }

                string selectedUrl = null;
                string selectedVersion = null;

                if (!string.IsNullOrEmpty(versionArg)) // versionArg 是從 -f 參數獲取的
                {
                    // 使用者指定版本
                    var match = entries.FirstOrDefault(x => x.Version.Equals(versionArg, StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                    {
                        Console.Error.WriteLine($"指定的版本 {versionArg} 在列表中不存在或其 URL 不符合 XAPK 格式。");
                        return;
                    }
                    selectedUrl = match.Url;
                    selectedVersion = match.Version;
                    Console.WriteLine($"選擇指定版本 {selectedVersion}");
                }
                else
                {
                    // 自動選擇最新版本 (比較 主.次.修訂)
                    var latestEntry = entries
                        .Select(e => {
                            var parts = e.Version.Split('.');
                            // 此處我們已在上面確保 parts[0] 和 parts[1] 可以解析為 int
                            return new {
                                e.Url,
                                e.Version,
                                Major = int.Parse(parts[0]),
                                Minor = int.Parse(parts[1]),
                                Patch = parts.Length > 2 && int.TryParse(parts[2], out int pVal) ? pVal : 0 // 第三部分（修訂號）
                            };
                        })
                        .OrderByDescending(v => v.Major)
                        .ThenByDescending(v => v.Minor)
                        .ThenByDescending(v => v.Patch)
                        .FirstOrDefault();

                    if (latestEntry == null)
                    {
                        Console.Error.WriteLine("無法從可用列表中確定最新版本。");
                        return;
                    }
                    selectedUrl = latestEntry.Url;
                    selectedVersion = latestEntry.Version;
                    Console.WriteLine($"選擇最新版本 {selectedVersion}");
                }
                JObject ParseMessage(string[] lines, ref int idx, int indent)
                {
                    var obj = new JObject();
                    var fieldRe = new Regex(@"^\s*(\d+):\s*(?:""([^""\\]*)""|(\d+))");
                    while (idx < lines.Length)
                    {
                        var line = lines[idx];
                        int curIndent = line.TakeWhile(c => c == ' ').Count();
                        if (curIndent < indent) break;
                        if (string.IsNullOrWhiteSpace(line) || line.Trim() == "}") { idx++; continue; }
                        if (line.TrimEnd().EndsWith("{"))
                        {
                            var numMatch = Regex.Match(line, @"^\s*(\d+)\s*\{");
                            if (!numMatch.Success) { idx++; continue; }
                            var key = numMatch.Groups[1].Value; idx++;
                            var child = ParseMessage(lines, ref idx, curIndent + 2);
                            if (obj.TryGetValue(key, out var exist))
                            {
                                if (exist is JArray arr) arr.Add(child);
                                else obj[key] = new JArray(exist, child);
                            }
                            else obj[key] = child;
                        }
                        else
                        {
                            var m = fieldRe.Match(line);
                            if (m.Success)
                            {
                                var key = m.Groups[1].Value;
                                var valText = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
                                JToken val = valText;
                                if (obj.TryGetValue(key, out var exist))
                                {
                                    if (exist is JArray ja) ja.Add(val);
                                    else obj[key] = new JArray(exist, val);
                                }
                                else obj[key] = val;
                            }
                            idx++;
                        }
                    }
                    return obj;
                }

                // 4. 下載 XAPK
                if (selectedUrl != null)
                {
                    Console.WriteLine($"準備從以下網址下載 XAPK: {selectedUrl}");
                    Console.WriteLine("Downloading XAPK ...");
                    var xapkBytes = await http.GetByteArrayAsync(selectedUrl);
                    string filePath = Path.Combine(downloadPath, "BlueArchive.XAPK"); // downloadPath 是您程式碼中定義的下載路徑
                    await File.WriteAllBytesAsync(filePath, xapkBytes);
                    Console.WriteLine($"XAPK 已儲存為: {filePath}");
                }
                else
                {
                     Console.Error.WriteLine("未能選擇有效的下載 URL。");
                }
                await UnXAPK.UnXAPKMain(args); 
            }    

            // 下載/更新 Chromium
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync(); // 不加任何參數

            // 啟動瀏覽器（Headless = false 方便除錯觀察；若要隱藏視窗可以設為 true）
            var launchOptions = new LaunchOptions
            {
                Headless = false,  // 顯示瀏覽器視窗
                DefaultViewport = null  // 不限制視窗大小
            };

            using var browser = await Puppeteer.LaunchAsync(launchOptions);
            var page = await browser.NewPageAsync();

            // 設定下載行為：將檔案下載到程式執行目錄 (或指定其他資料夾)
            var client = await page.Target.CreateCDPSessionAsync();
            await client.SendAsync("Page.setDownloadBehavior", new
            {
                behavior = "allow",
                downloadPath = Path.Combine(rootDirectory, "Downloads", "XAPK"),
            });

            // 導向至下載頁面，等待網頁載入完成
            // 由於 Cloudflare 可能有「五秒盾」或 JS Challenge，可以用 NetworkIdle2/NetworkIdle0 盡量等待網頁完成
            try
            {
                Console.WriteLine("嘗試載入頁面，等待 Cloudflare 驗證...");
                await page.GoToAsync(downloadUrl, WaitUntilNavigation.Networkidle2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"載入頁面時發生錯誤: {ex.Message}");
            }


            await Task.Delay(5000); // 等待 5 秒看是否需要額外 Cloudflare 檢查
            Console.WriteLine("開始等待檔案下載...");


            string downloadedFile = await WaitForDownloadedFileAsync(downloadPath, TimeSpan.FromSeconds(600));
            if (downloadedFile != null)
            {
                Console.WriteLine($"檔案下載完成: {downloadedFile}");
            }
            else
            {
                Console.WriteLine("下載逾時或沒有偵測到下載檔案。");
            }

            Console.WriteLine("下載程序結束，關閉瀏覽器。");
            await browser.CloseAsync();
            await UnXAPK.UnXAPKMain(args);
        }

        public static async Task<string> WaitForDownloadedFileAsync(string downloadDir, TimeSpan timeout)
        {
            var watch = Stopwatch.StartNew();
            var initialFiles = Directory.GetFiles(downloadDir).ToHashSet();

            while (watch.Elapsed < timeout)
            {
                var currentFiles = Directory.GetFiles(downloadDir).ToHashSet();
                var newFiles = currentFiles.Except(initialFiles).ToList();
                if (newFiles.Count > 0)
                {
                    var newFile = newFiles[0];
                    bool isFileReady = false;
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            using (var fileStream = File.Open(newFiles[0], FileMode.Open, FileAccess.Read, FileShare.None))
                            {
                                isFileReady = fileStream.Length > 0;
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            await Task.Delay(1000);
                        }
                        if (isFileReady) break;
                    }
                    return newFile;
                }
                await Task.Delay(500);

            }
            return null;
        }

    }
}
