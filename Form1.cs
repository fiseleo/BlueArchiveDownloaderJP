using System.Text.RegularExpressions;
using BlueArchiveGUIDownloader.Properties;
namespace BlueArchiveGUIDownloader
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Console.SetOut(new RichTextBoxWriter(logbox));
            Console.WriteLine("Blue Archive GUI Downloader");
            VersionText.ValidatingType = typeof(string);
            VersionText.TypeValidationCompleted += VersionText_TypeValidationCompleted;
        }

        private void button1_Click(object sender, EventArgs e)
        {

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

            ProgressBar.Visible = true;
            if (result != DialogResult.Yes)

                return;

            // …下面維持你原本的下載邏輯…

            try
            {
                await RunDownloadAsync();
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

        private async Task RunDownloadAsync()
        {
            // 這裡是下載邏輯
            // 你可以使用 HttpClient 或 WebClient 來下載檔案
            
            var versionArg = VersionText.Text.Trim();
            bool validFmt = Regex.IsMatch(versionArg, @"^\d\.\d{2}\.\d{6}$");
            string rootDirectory = Directory.GetCurrentDirectory();
            if (!Directory.Exists(Path.Combine(rootDirectory, "Downloads", "XAPK")))
            {
                Directory.CreateDirectory(Path.Combine(rootDirectory, "Downloads", "XAPK"));
            }
            var downloadPath = Path.Combine(rootDirectory, "Downloads", "XAPK");
            if (DirectDownload.Checked)
                await DoDirectDownload();
            else if (UseChromeBrowserDownload.Checked)
                await DoChromeDownload();
            else
                throw new Exception(Resources.ErrorChooseDownloadMethod);
        }
    }
}
