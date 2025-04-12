using System.Diagnostics;
using PuppeteerSharp;
using System.Net.Http;
using System.Collections.Specialized;

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
            if (Directory.Exists(downloadPath)){
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
                //reserved function,because cloudflare is blocking httpclient
                Console.WriteLine("使用直接下載模式...");
                try
                {
                    using (var httpClient = new HttpClient())
                    {

                        var content = new StringContent(string.Empty);
                        var response = await httpClient.PostAsync(downloadUrl, content);
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.Found)
                        {
                            string redirectUrl = response.Headers.Location.ToString();
                            Console.WriteLine($"取得重定向連結: {redirectUrl}");
                            
                            var downloadResponse = await httpClient.GetAsync(redirectUrl);
                            if (downloadResponse.IsSuccessStatusCode)
                            {
                                var fileName = Path.Combine(downloadPath, "BlueArchive.xapk");
                                using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    await downloadResponse.Content.CopyToAsync(fileStream);
                                }
                                Console.WriteLine($"檔案下載完成: {fileName}");
                                await UnXAPK.UnXAPKMain(args);
                                return;
                            }
                            else
                            {
                                Console.WriteLine($"重定向下載失敗，狀態碼: {downloadResponse.StatusCode}");
                            }
                        }
                        else if (response.IsSuccessStatusCode)
                        {
                            var fileName = Path.Combine(downloadPath, "BlueArchive.xapk");
                            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await response.Content.CopyToAsync(fileStream);
                            }
                            Console.WriteLine($"檔案下載完成: {fileName}");
                            await UnXAPK.UnXAPKMain(args);
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"直接下載失敗，狀態碼: {response.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"直接下載失敗: {ex.Message}");
                }
                Console.WriteLine("是否要使用無頭瀏覽器下載? (y/n)");
                string input = Console.ReadLine();
                if (input.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("切換回瀏覽器下載模式...");
                }
                else if (input.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Terminating Application");
                    return;
                }
                else
                {
                    Console.WriteLine("Invalid input, Terminating Application");
                    return;
                }
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

        public static async Task<string> WaitForDownloadedFileAsync(string downloadDir, TimeSpan timeout){
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
