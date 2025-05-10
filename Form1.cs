<<<<<<< HEAD
using System.Text.RegularExpressions;
=======
using System.Windows.Forms;
>>>>>>> 2397e618f8e1d2a12522097587a985ca71f41552

namespace BlueArchiveGUIDownloader
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Console.SetOut(new RichTextBoxWriter(logbox));
            Console.WriteLine("Blue Archive GUI Downloader");
<<<<<<< HEAD
            VersionText.ValidatingType = typeof(string);
            VersionText.TypeValidationCompleted += VersionText_TypeValidationCompleted;
=======
>>>>>>> 2397e618f8e1d2a12522097587a985ca71f41552
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
<<<<<<< HEAD
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
=======

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

>>>>>>> 2397e618f8e1d2a12522097587a985ca71f41552
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
<<<<<<< HEAD

        private void VersionText_TypeValidationCompleted(object sender, TypeValidationEventArgs e)
        {
            var tb = sender as MaskedTextBox;
            if (tb == null) return;

            // 驗證結果：失敗就紅底，成功就白底
            tb.BackColor = e.IsValidInput
                ? Color.Green
                : Color.Red;
        }
        
=======
>>>>>>> 2397e618f8e1d2a12522097587a985ca71f41552
    }
}
