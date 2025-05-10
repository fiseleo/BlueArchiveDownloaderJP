using System.Text.RegularExpressions;

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
        
    }
}
