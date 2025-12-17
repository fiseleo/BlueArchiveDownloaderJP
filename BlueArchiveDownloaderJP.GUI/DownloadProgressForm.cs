using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueArchiveGUIDownloader
{
    public partial class DownloadProgressForm : Form
    {
        public DownloadProgressForm()
        {
            InitializeComponent();
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void DownloadProgressForm_Load(object sender, EventArgs e)
        {

        }

        public void InitMax(int bundleCount, int mediaCount, int tableCount)
        {
            pbBundle.Maximum = bundleCount;
            pbMedia.Maximum = mediaCount;
            pbTable.Maximum = tableCount;
        }
        public void ReportBundle(int value, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ReportBundle(value, message)));
                return;
            }
            pbBundle.Value = value;
            lblBundle.Text = message;
        }
        public void ReportMedia(int value, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ReportMedia(value, message)));
                return;
            }
            pbMedia.Value = value;
            lblMedia.Text = message;
        }
        public void ReportTable(int value, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ReportTable(value, message)));
                return;
            }
            pbTable.Value = value;
            lblTable.Text = message;
        }

        private void pbBundle_Click(object sender, EventArgs e)
        {

        }
    }
}
