using System.Windows.Forms;

namespace BlueArchiveGUIDownloader
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Console.SetOut(new RichTextBoxWriter(logbox));
            Console.WriteLine("Blue Archive GUI Downloader");
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
