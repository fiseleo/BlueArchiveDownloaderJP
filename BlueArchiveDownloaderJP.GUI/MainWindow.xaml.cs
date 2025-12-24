using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BlueArchiveGUIDownloader.Properties;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Crypto;
using ICSharpCode.SharpZipLib.Zip;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Dispatching;

namespace BlueArchiveGUIDownloader
{
    public sealed partial class MainWindow : Window
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly StringBuilder _logBuilder = new StringBuilder();

        public MainWindow()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            
            // Redirect console output to log
            Console.SetOut(new TextBlockWriter(this));
            Console.WriteLine("Blue Archive GUI Downloader");
            
            ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void VersionText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string text = textBox.Text.Trim();
            bool validFmt = Regex.IsMatch(text, @"^\d\.\d{2}\.\d{6}$");
            
            // Visual feedback using background color (WinUI 3 approach)
            if (string.IsNullOrEmpty(text))
            {
                textBox.Background = null;
            }
            else if (validFmt)
            {
                textBox.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                textBox.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.LightPink);
            }
        }

        private void DirectDownload_Checked(object sender, RoutedEventArgs e)
        {
            UseChromeBrowserDownload.IsChecked = false;
        }

        private void DirectDownload_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private void UseChromeBrowserDownload_Checked(object sender, RoutedEventArgs e)
        {
            DirectDownload.IsChecked = false;
        }

        private void UseChromeBrowserDownload_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private async void DelData_Click(object sender, RoutedEventArgs e)
        {
            string rootDirectory = Directory.GetCurrentDirectory();
            var msg = Resources.ConfirmDelMessage;
            var title = Resources.ConfirmDelTitle;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = msg,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            var downloadPath = Path.Combine(rootDirectory, "Downloads");
            if (Directory.Exists(downloadPath))
            {
                Directory.Delete(downloadPath, true);
                await ShowMessageAsync(
                    Resources.DelCompleteTitle,
                    Resources.DelCompleteMessage
                );
            }
            else
            {
                await ShowMessageAsync(
                    Resources.DelErrorTitle,
                    Resources.DelErrorMessage
                );
            }
        }

        private async void DownloadData_Click(object sender, RoutedEventArgs e)
        {
            var msg = Resources.ConfirmDownloadMessage;
            var title = Resources.ConfirmDownloadTitle;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = msg,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            
            ProgressBar.Value = 0;
            ProgressBar.Visibility = Visibility.Visible;
            
            if (result != ContentDialogResult.Primary)
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                var progress = new Progress<double>(p =>
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        ProgressBar.Value = p;
                    });
                });
                await RunDownloadAsync(progress);
                await ShowMessageAsync(
                    Resources.DownloadCompleteTitle,
                    Resources.DownloadCompleteMessage
                );
            }
            catch (Exception ex)
            {
                await ShowMessageAsync(
                    Resources.DownloadErrorTitle,
                    ex.Message
                );
            }
            finally
            {
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async void Audio_Click(object sender, RoutedEventArgs e)
        {
            string rootDirectory = Directory.GetCurrentDirectory();
            var AudioPath = Path.Combine(rootDirectory, "Downloads", "MediaResources", "GameData", "Audio", "VOC_JP");

            if (!Directory.Exists(AudioPath))
            {
                await ShowMessageAsync(
                    Resources.AudioErrorTitle,
                    Resources.AudioErrorMessage
                );
                return;
            }

            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;

            var progress = new Progress<double>(p =>
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    ProgressBar.Value = Math.Min(100, Math.Max(0, p));
                });
            });

            await Extract_Audio(AudioPath, progress);

            ProgressBar.Visibility = Visibility.Collapsed;
            await ShowMessageAsync(
                Resources.AudioExtractCompleteTitle,
                Resources.AudioExtractCompleteMessage
            );
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        public void AppendLog(string text)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                _logBuilder.AppendLine(text);
                LogBox.Text = _logBuilder.ToString();
            });
        }

        private async Task RunDownloadAsync(IProgress<double> progress)
        {
            string rootDirectory = Directory.GetCurrentDirectory();
            if (!Directory.Exists(Path.Combine(rootDirectory, "Downloads", "XAPK")))
            {
                Directory.CreateDirectory(Path.Combine(rootDirectory, "Downloads", "XAPK"));
            }
            
            if (DirectDownload.IsChecked == true)
                await DoDirectDownload(progress);
            else if (UseChromeBrowserDownload.IsChecked == true)
                await DoChromeDownload();
            else
                throw new Exception(Resources.ErrorChooseDownloadMethod);
        }

        private async Task DoDirectDownload(IProgress<double> progress)
        {
            var versionArg = VersionText.Text.Trim();
            bool validFmt = Regex.IsMatch(versionArg, @"^\d\.\d{2}\.\d{6}$");

            const string pbUrl = "https://api.pureapk.com/m/v3/cms/app_version?hl=en-US&package_name=com.YostarJP.BlueArchive";
            using var http = new HttpClient();
            http.Timeout = Timeout.InfiniteTimeSpan;
            http.DefaultRequestHeaders.Add("x-sv", "29");
            http.DefaultRequestHeaders.Add("x-abis", "arm64-v8a,armeabi-v7a,armeabi");
            http.DefaultRequestHeaders.Add("x-gp", "1");
            Console.WriteLine("正在下載版本資訊 (response.pb)...");
            var pbBytes = await http.GetByteArrayAsync(pbUrl);

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
            Console.WriteLine("正在使用 protoc --decode_raw 進行解碼...");
            using var proc = Process.Start(psi);
            await proc.StandardInput.BaseStream.WriteAsync(pbBytes, 0, pbBytes.Length);
            proc.StandardInput.Close();
            string textProto = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();

            var lines = textProto.Split('\n');
            int idx = 0;
            JObject root = ParseMessage(lines, ref idx, 0);
            Console.WriteLine("已生成 response.json 的內部表示");

            var array = root.SelectToken("$['1']['7']['2']") as JArray;
            if (array == null)
            {
                Console.Error.WriteLine("從回應中解析 version_list 失敗 (路徑 $['1']['7']['2'] 無效或不存在)。");
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

                        var versionCodeToken = info["5"];
                        var versionNameToken = info["6"];
                        var urlToken = info?["24"]?["9"];

                        if (versionCodeToken == null || urlToken == null || urlToken.Type != JTokenType.String)
                        {
                            return null;
                        }

                        string versionCodeStr = versionCodeToken.Value<string>();
                        string versionNameStr = "N/A";
                        if (versionNameToken != null && versionNameToken.Type == JTokenType.String)
                        {
                            versionNameStr = versionNameToken.Value<string>();
                        }

                        string url = urlToken.Value<string>();

                        if (string.IsNullOrEmpty(versionCodeStr) || !url.StartsWith("https://download.pureapk.com/b/XAPK/"))
                        {
                            return null;
                        }

                        if (!long.TryParse(versionCodeStr, out _))
                        {
                            return null;
                        }

                        return new { VersionCode = versionCodeStr, VersionName = versionNameStr, Url = url };
                    })
                    .Where(x => x != null)
                    .ToList();

            if (!entries.Any())
            {
                Console.Error.WriteLine("在回應中未找到有效的 XAPK 下載連結或版本資訊。");
                return;
            }

            string selectedUrl = null;
            string selectedVersion = null;

            if (validFmt)
            {
                var targetVersionCode = versionArg.Substring(5);
                var match = entries.FirstOrDefault(x => x.VersionCode.Equals(targetVersionCode, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    selectedUrl = match.Url;
                    selectedVersion = match.VersionName;
                    Console.WriteLine($"已找到匹配的版本。將下載版本: {selectedVersion} (VersionCode: {match.VersionCode})");
                }
                else
                {
                    Console.WriteLine($"未找到指定的版本 (VersionCode: {targetVersionCode})。將改為下載最新版本。");
                }
            }

            if (selectedUrl == null)
            {
                Console.WriteLine("正在確定最新版本...");
                var latest = entries
                    .OrderByDescending(e => long.Parse(e.VersionCode))
                    .First();

                selectedUrl = latest.Url;
                selectedVersion = latest.VersionName;
                Console.WriteLine($"將下載最新版本: {selectedVersion} (VersionCode: {latest.VersionCode})");
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

            if (selectedUrl != null)
            {
                Console.WriteLine($"選擇的 URL: {selectedUrl}");
                Console.WriteLine("正在下載 XAPK...");
                using var response = await new HttpClient { Timeout = Timeout.InfiniteTimeSpan }
                                            .GetAsync(selectedUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var downloadDir = Path.Combine(Directory.GetCurrentDirectory(), "Downloads", "XAPK");
                var outFile = Path.Combine(downloadDir, $"BlueArchive.xapk");
                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(outFile);
                var buffer = new byte[81920];
                long bytesReadTotal = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    bytesReadTotal += bytesRead;

                    if (totalBytes > 0)
                    {
                        double pct = bytesReadTotal * 100.0 / totalBytes;
                        progress.Report(pct);
                    }
                }

                Console.WriteLine("XAPK 下載完成。");
            }
            else
            {
                Console.Error.WriteLine("無法選擇有效的下載 URL。");
            }
            await UnXAPK.UnXAPKMain();
            return;
        }

        private async Task DoChromeDownload()
        {
            var versionArg = VersionText.Text.Trim();
            bool validFmt = Regex.IsMatch(versionArg, @"^\d\.\d{2}\.\d{6}$");
            string downloadUrl;
            if (!string.IsNullOrEmpty(versionArg) && validFmt)
            {
                var versionCode = versionArg.Substring(5);
                downloadUrl = $"https://d.apkpure.com/b/XAPK/com.YostarJP.BlueArchive?versionCode={versionCode}&nc=arm64-v8a&sv=24";
            }
            else
            {
                downloadUrl = "https://d.apkpure.com/b/XAPK/com.YostarJP.BlueArchive?version=latest";
            }
            Console.WriteLine($"準備下載 URL: {downloadUrl}");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            var launchOptions = new LaunchOptions
            {
                Headless = false,
                DefaultViewport = null
            };
            using var browser = await Puppeteer.LaunchAsync(launchOptions);
            var page = await browser.NewPageAsync();
            var client = await page.Target.CreateCDPSessionAsync();
            string rootDirectory = Directory.GetCurrentDirectory();
            var parameters = new Dictionary<string, object>
            {
                ["behavior"] = "allow",
                ["downloadPath"] = Path.Combine(rootDirectory, "Downloads", "XAPK")
            };

            await client.SendAsync("Page.setDownloadBehavior", parameters);
            try
            {
                Console.WriteLine("嘗試載入頁面並等待 Cloudflare 驗證...");
                await page.GoToAsync(downloadUrl, WaitUntilNavigation.Networkidle2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"載入頁面時發生錯誤: {ex.Message}");
            }
            await Task.Delay(5000);
            Console.WriteLine("等待檔案下載完成...");
            string downloadPath = Path.Combine(rootDirectory, "Downloads", "XAPK");
            string downloadedFile = await WaitForDownloadedFileAsync(downloadPath, TimeSpan.FromSeconds(600));
            if (downloadedFile != null)
            {
                Console.WriteLine($"下載完成: {downloadedFile}");
            }
            else
            {
                Console.WriteLine("下載逾時或未偵測到檔案。");
            }

            Console.WriteLine("下載流程完成；正在關閉瀏覽器。");
            await browser.CloseAsync();

            await UnXAPK.UnXAPKMain();
            return;
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

        private async Task Extract_Audio(string path, IProgress<double> progress)
        {
            var zipFiles = Directory.GetFiles(path, "*.zip");
            int total = zipFiles.Length;
            if (total == 0) return;
            for (int i = 0; i < total; i++)
            {
                var zipFile = zipFiles[i];
                string fileNameNoExt = Path.GetFileName(zipFile).ToLowerInvariant();
                byte[] pwdBytes = TableService.CreatePassword(fileNameNoExt, 20);
                string password = Convert.ToBase64String(pwdBytes);
                using var fs = File.OpenRead(zipFile);
                using var zip = new ZipInputStream(fs)
                {
                    Password = password
                };
                Console.WriteLine($"正在解壓縮 {zipFile} ...");
                Console.WriteLine($"密碼: {password}");
                var folderName = Path.GetFileNameWithoutExtension(zipFile);
                string extractRoot = Path.Combine(path, folderName);

                ZipEntry entry;
                while ((entry = zip.GetNextEntry()) != null)
                {
                    if (entry.IsDirectory)
                        continue;

                    string outFile = Path.Combine(extractRoot, entry.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);

                    await using var outFs = File.Create(outFile);
                    await zip.CopyToAsync(outFs);
                }

                File.Delete(zipFile);

                double pct = (i + 1) * 100.0 / total;
                progress.Report(pct);
            }
        }

        private class TextBlockWriter : TextWriter
        {
            private readonly MainWindow _window;

            public TextBlockWriter(MainWindow window)
            {
                _window = window;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void WriteLine(string value)
            {
                _window.AppendLog(value);
            }

            public override void Write(string value)
            {
                _window.AppendLog(value);
            }
        }
    }
}
