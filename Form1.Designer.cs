namespace BlueArchiveGUIDownloader
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            DelData = new Button();
            DownloadData = new Button();
            UseChromeBrowserDownload = new CheckBox();
            DirectDownload = new CheckBox();
            logbox = new RichTextBox();
<<<<<<< HEAD
            VersionText = new MaskedTextBox();
=======
>>>>>>> 2397e618f8e1d2a12522097587a985ca71f41552
            SuspendLayout();
            // 
            // DelData
            // 
            DelData.BackColor = SystemColors.ControlLightLight;
            resources.ApplyResources(DelData, "DelData");
            DelData.Name = "DelData";
            DelData.UseVisualStyleBackColor = false;
            DelData.Click += button1_Click;
            // 
            // DownloadData
            // 
            resources.ApplyResources(DownloadData, "DownloadData");
            DownloadData.Name = "DownloadData";
            DownloadData.UseVisualStyleBackColor = true;
            // 
            // UseChromeBrowserDownload
            // 
            resources.ApplyResources(UseChromeBrowserDownload, "UseChromeBrowserDownload");
            UseChromeBrowserDownload.Name = "UseChromeBrowserDownload";
            UseChromeBrowserDownload.UseVisualStyleBackColor = true;
            UseChromeBrowserDownload.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // DirectDownload
            // 
            resources.ApplyResources(DirectDownload, "DirectDownload");
            DirectDownload.Name = "DirectDownload";
            DirectDownload.UseVisualStyleBackColor = true;
            DirectDownload.CheckedChanged += checkBox2_CheckedChanged;
            // 
            // logbox
            // 
            resources.ApplyResources(logbox, "logbox");
            logbox.Name = "logbox";
            logbox.ReadOnly = true;
            logbox.TextChanged += richTextBox1_TextChanged;
            // 
<<<<<<< HEAD
            // VersionText
            // 
            VersionText.Culture = new System.Globalization.CultureInfo("");
            resources.ApplyResources(VersionText, "VersionText");
            VersionText.Name = "VersionText";
            VersionText.ValidatingType = typeof(string);

            // 
=======
>>>>>>> 2397e618f8e1d2a12522097587a985ca71f41552
            // MainForm
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SystemColors.Window;
<<<<<<< HEAD
            Controls.Add(VersionText);
=======
>>>>>>> 2397e618f8e1d2a12522097587a985ca71f41552
            Controls.Add(logbox);
            Controls.Add(DirectDownload);
            Controls.Add(UseChromeBrowserDownload);
            Controls.Add(DownloadData);
            Controls.Add(DelData);
            Name = "MainForm";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button DelData;
        private Button DownloadData;
        private CheckBox UseChromeBrowserDownload;
        private CheckBox DirectDownload;
        private RichTextBox logbox;
<<<<<<< HEAD
        private MaskedTextBox VersionText;
=======
>>>>>>> 2397e618f8e1d2a12522097587a985ca71f41552
    }
}
