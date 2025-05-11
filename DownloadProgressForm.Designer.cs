namespace BlueArchiveGUIDownloader
{
    partial class DownloadProgressForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DownloadProgressForm));
            pbMedia = new ProgressBar();
            pbBundle = new ProgressBar();
            pbTable = new ProgressBar();
            lblBundle = new Label();
            lblMedia = new Label();
            lblTable = new Label();
            SuspendLayout();
            // 
            // pbMedia
            // 
            resources.ApplyResources(pbMedia, "pbMedia");
            pbMedia.Name = "pbMedia";
            pbMedia.Click += progressBar1_Click;
            // 
            // pbBundle
            // 
            resources.ApplyResources(pbBundle, "pbBundle");
            pbBundle.Name = "pbBundle";
            pbBundle.Click += pbBundle_Click;
            // 
            // pbTable
            // 
            resources.ApplyResources(pbTable, "pbTable");
            pbTable.Name = "pbTable";
            // 
            // lblBundle
            // 
            resources.ApplyResources(lblBundle, "lblBundle");
            lblBundle.Name = "lblBundle";
            // 
            // lblMedia
            // 
            resources.ApplyResources(lblMedia, "lblMedia");
            lblMedia.Name = "lblMedia";
            // 
            // lblTable
            // 
            resources.ApplyResources(lblTable, "lblTable");
            lblTable.Name = "lblTable";
            // 
            // DownloadProgressForm
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            ControlBox = false;
            Controls.Add(lblTable);
            Controls.Add(lblMedia);
            Controls.Add(lblBundle);
            Controls.Add(pbTable);
            Controls.Add(pbBundle);
            Controls.Add(pbMedia);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "DownloadProgressForm";
            Load += DownloadProgressForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ProgressBar pbMedia;
        private ProgressBar pbBundle;
        private ProgressBar pbTable;
        private Label lblBundle;
        private Label lblMedia;
        private Label lblTable;
    }
}