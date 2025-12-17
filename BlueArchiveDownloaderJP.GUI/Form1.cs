using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using BlueArchiveGUIDownloader.Properties;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Crypto;
using ICSharpCode.SharpZipLib.Zip;
namespace BlueArchiveGUIDownloader
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Console.SetOut(new RichTextBoxWriter(logbox));
            Console.WriteLine("Blue Archive GUI Downloader");
            ProgressBar.Visible = false;
            VersionText.ValidatingType = typeof(string);
            VersionText.TypeValidationCompleted += VersionText_TypeValidationCompleted;
            ProgressBar.Style = ProgressBarStyle.Continuous;
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string rootDirectory = Directory.GetCurrentDirectory();
            var msg = Resources.ConfirmDelMessage;
            var title = Resources.ConfirmDelTitle;
            var result = MessageBox.Show(
                msg,
                title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (result != DialogResult.Yes)
                return;
            var downloadPath = Path.Combine(rootDirectory, "Downloads");
            if (Directory.Exists(downloadPath))
            {
                Directory.Delete(downloadPath, true);
                MessageBox.Show(
                    Resources.DelCompleteMessage,
                    Resources.DelCompleteTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                MessageBox.Show(
                    Resources.DelErrorMessage,
                    Resources.DelErrorTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (UseChromeBrowserDownload.Checked)
            {
                DirectDownload.Checked = false;
            }

        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (DirectDownload.Checked)
            {
                UseChromeBrowserDownload.Checked = false;
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void VersionText_TypeValidationCompleted(object sender, TypeValidationEventArgs e)
        {
            var tb = sender as MaskedTextBox;
            if (tb == null) return;


            tb.BackColor = e.IsValidInput
                ? Color.Green
                : Color.Red;
        }

        private void VersionText_KeyPress(object sender, KeyPressEventArgs e)
        {
            var mtb = sender as MaskedTextBox;
            if (mtb == null) return;

            // 允許 Backspace、Delete 正常運作
            if (e.KeyChar == '\b') return;

            // 按下任何 key 之後，等它處理完 mask，再把游標往右跳過 literals
            this.BeginInvoke((Action)(() =>
            {
                int pos = mtb.SelectionStart;
                // 當前游標如果卡在一個 literal（mask 中非 '0'、'9'、'L'…等 editable code），就跳過
                if (pos < mtb.Mask.Length && !"09L?&C".Contains(mtb.Mask[pos]))
                    mtb.SelectionStart = pos + 1;
            }));
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private async void DownloadData_Click(object sender, EventArgs e)
        {
            // 從資源檔抓 i18n 文字
            var msg = Resources.ConfirmDownloadMessage;
            var title = Resources.ConfirmDownloadTitle;

            var result = MessageBox.Show(
                msg,
                title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            ProgressBar.Value = 0;
            ProgressBar.Visible = true;
            if (result != DialogResult.Yes)
                return;

            // …下面維持你原本的下載邏輯…

            try
            {

                var progress = new Progress<double>(p =>
                {
                    // p = 0.0 ~ 100.0
                    ProgressBar.Value = (int)p;
                });
                await RunDownloadAsync(progress);
                MessageBox.Show(
                    Resources.DownloadCompleteMessage,
                    Resources.DownloadCompleteTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    Resources.DownloadErrorTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                ProgressBar.Visible = false;
            }
        }

        private void ProgressBar_Click(object sender, EventArgs e)
        {

        }

        private async Task RunDownloadAsync(IProgress<double> progress)
        {
            // 這裡是下載邏輯
            // 你可以使用 HttpClient 或 WebClient 來下載檔案
            string rootDirectory = Directory.GetCurrentDirectory();
            if (!Directory.Exists(Path.Combine(rootDirectory, "Downloads", "XAPK")))
            {
                Directory.CreateDirectory(Path.Combine(rootDirectory, "Downloads", "XAPK"));
            }
            var downloadPath = Path.Combine(rootDirectory, "Downloads", "XAPK");
            if (DirectDownload.Checked)
                await DoDirectDownload(progress);
            else if (UseChromeBrowserDownload.Checked)
                await DoChromeDownload();
            else
                throw new Exception(Resources.ErrorChooseDownloadMethod);
        }

        private async Task DoDirectDownload(IProgress<double> progress)
        {
            // 從輸入框獲取版本參數並去除前後空白
            var versionArg = VersionText.Text.Trim();
            // 驗證版本格式是否為 X.XX.XXXXXX
            bool validFmt = Regex.IsMatch(versionArg, @"^\d\.\d{2}\.\d{6}$");

            // PureAPK 的 API URL
            const string pbUrl = "https://api.pureapk.com/m/v3/cms/app_version?hl=en-US&package_name=com.YostarJP.BlueArchive";
            using var http = new HttpClient();
            http.Timeout = Timeout.InfiniteTimeSpan;
            // 設定請求標頭
            http.DefaultRequestHeaders.Add("x-sv", "29");
            http.DefaultRequestHeaders.Add("x-abis", "arm64-v8a,armeabi-v7a,armeabi");
            http.DefaultRequestHeaders.Add("x-gp", "1");
            Console.WriteLine("正在下載版本資訊 (response.pb)...");
            var pbBytes = await http.GetByteArrayAsync(pbUrl);

            // 使用 protoc.exe 解碼協定緩衝區
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

            // 解析解碼後的文字
            var lines = textProto.Split('\n');
            int idx = 0;
            JObject root = ParseMessage(lines, ref idx, 0);
            Console.WriteLine("已生成 response.json 的內部表示");

            // 從解析後的 JSON 中選取版本列表
            var array = root.SelectToken("$['1']['7']['2']") as JArray;
            if (array == null)
            {
                Console.Error.WriteLine("從回應中解析 version_list 失敗 (路徑 $['1']['7']['2'] 無效或不存在)。");
                return;
            }

            // 將版本資訊提取並結構化
            var entries = array
                    .Select(item =>
                    {
                        var itemObj = item as JObject;
                        if (itemObj == null) return null;

                        var threeToken = itemObj["3"] as JObject;
                        if (threeToken == null) return null;

                        var info = threeToken["2"] as JObject;
                        if (info == null) return null;

                        // 根據使用者要求，改用欄位 5 作為 versionCode
                        var versionCodeToken = info["5"];
                        // 欄位 6 作為 versionName (用於顯示)
                        var versionNameToken = info["6"];
                        // 修正路徑以正確獲取 XAPK URL
                        var urlToken = info?["24"]?["9"];

                        if (versionCodeToken == null || urlToken == null || urlToken.Type != JTokenType.String)
                        {
                            return null;
                        }

                        string versionCodeStr = versionCodeToken.Value<string>();
                        string versionNameStr = "N/A"; // 預設值
                        if (versionNameToken != null && versionNameToken.Type == JTokenType.String)
                        {
                            versionNameStr = versionNameToken.Value<string>();
                        }

                        string url = urlToken.Value<string>();

                        // 過濾掉非 XAPK 的連結
                        if (string.IsNullOrEmpty(versionCodeStr) || !url.StartsWith("https://download.pureapk.com/b/XAPK/"))
                        {
                            return null;
                        }

                        // 驗證 versionCode 必須是數字
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
            string selectedVersion = null; // 用於顯示的版本名稱

            if (validFmt)
            {
                // 如果使用者輸入了特定版本，根據 versionCode (XXXXXX) 進行匹配
                // 例如 "1.53.323417" 只取 "323417"
                var targetVersionCode = versionArg.Substring(5);
                var match = entries.FirstOrDefault(x => x.VersionCode.Equals(targetVersionCode, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    selectedUrl = match.Url;
                    selectedVersion = match.VersionName; // 使用完整的版本名稱進行顯示
                    Console.WriteLine($"已找到匹配的版本。將下載版本: {selectedVersion} (VersionCode: {match.VersionCode})");
                }
                else
                {
                    Console.WriteLine($"未找到指定的版本 (VersionCode: {targetVersionCode})。將改為下載最新版本。");
                }
            }

            // 如果沒有指定版本，或指定版本未找到，則選擇最新版本
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

            // 下載檔案
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
                        // 計算百分比並回報
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
                // 取小數點後的部分。例如 "1.53.323417" 只取 "323417"
                var versionCode = versionArg.Substring(5); // 從 index=5 開始擷取
                downloadUrl = $"https://d.apkpure.com/b/XAPK/com.YostarJP.BlueArchive?versionCode={versionCode}&nc=arm64-v8a&sv=24";
            }
            else
            {
                // 沒有指定版本，或格式不符合，改用 latest
                downloadUrl = "https://d.apkpure.com/b/XAPK/com.YostarJP.BlueArchive?version=latest";
            }
            Console.WriteLine($"準備下載 URL: {downloadUrl}");
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
            var launchOptions = new LaunchOptions
            {
                Headless = false,  // 顯示瀏覽器視窗
                DefaultViewport = null  // 不限制視窗大小
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
            await Task.Delay(5000); // 等待 5 秒看是否需要額外 Cloudflare 檢查
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

        private async void Audio_Click(object sender, EventArgs e)
        {
            string rootDirectory = Directory.GetCurrentDirectory();
            var AudioPath = Path.Combine(rootDirectory, "Downloads", "MediaResources", "GameData", "Audio", "VOC_JP");


            if (!Directory.Exists(AudioPath))
            {
                MessageBox.Show(
                    Resources.AudioErrorMessage,
                    Resources.AudioErrorTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            ProgressBar.Visible = true;
            ProgressBar.Value = 0;

            // 2) 建立 IProgress，報 p: 0.0 ~ 100.0
            var progress = new Progress<double>(p =>
            {
                // cast to int，確保落在 [0,100]
                ProgressBar.Value = Math.Min(100, Math.Max(0, (int)p));
            });

            // 3) 傳進 Extract_Audio
            await Extract_Audio(AudioPath, progress);


            ProgressBar.Visible = false;
            MessageBox.Show(
                Resources.AudioExtractCompleteMessage,
                Resources.AudioExtractCompleteTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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

                    Password = password  // 設定解密密碼
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

                    // 非同步複製
                    await using var outFs = File.Create(outFile);
                    await zip.CopyToAsync(outFs);
                }

                // 關閉 ZipInputStream
                File.Delete(zipFile);

                double pct = (i + 1) * 100.0 / total;
                progress.Report(pct);
            }


        }
    }
}