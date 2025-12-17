

class Hex
{
    public static async Task HexMain(string[] args)
    {
        // 根目錄與資料夾路徑設定
        string rootDirectory = Directory.GetCurrentDirectory();
        string downloadPath = Path.Combine(rootDirectory, "Downloads", "XAPK");
        string unZipPath = Path.Combine(downloadPath, "Unzip");
        // 假設所有目標檔案都在 "assets/bin/Data" 資料夾
        string dataPath = Path.Combine(unZipPath, "assets", "bin", "Data");

        if (!Directory.Exists(dataPath))
        {
            Console.WriteLine("Data directory does not exist; cannot proceed.");
            return;
        }

        // 列出 Data 資料夾下的所有檔案
        var dataFiles = Directory.GetFiles(dataPath, "*", SearchOption.TopDirectoryOnly);
        if (dataFiles.Length == 0)
        {
            Console.WriteLine("No files found in the Data directory.");
            return;
        }

        // 要搜尋的十六進位序列（字串形式）
        string sequence = "47616D654D61696E436F6E666967000092030000";

        // 輸出資料夾 (避免覆蓋原檔)
        string outputDir = Path.Combine(downloadPath, "Processed");
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        Console.WriteLine($"Starting to process {dataFiles.Length} files...");
        int processedCount = 0;

        foreach (var filePath in dataFiles)
        {
            string fileName = Path.GetFileName(filePath);
            Console.WriteLine($"\nProcessing file: {fileName}");

            // 1) 讀取檔案 bytes
            byte[] fileData;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fileData = new byte[fs.Length];
                fs.Read(fileData, 0, fileData.Length);
            }

            // 2) 搜尋指定序列
            int index = FindSequence(fileData, sequence);
            if (index == -1)
            {
                Console.WriteLine("  => Sequence not found; skipping.");
                continue;
            }

            // 3) 計算裁切後剩餘的 byte[]
            int bytesToRemove = index + (sequence.Length / 2);
            if (bytesToRemove >= fileData.Length)
            {
                Console.WriteLine("  => Invalid sequence position or length; unable to trim.");
                continue;
            }

            byte[] newData = new byte[fileData.Length - bytesToRemove];
            Array.Copy(fileData, bytesToRemove, newData, 0, newData.Length);

            // 4) 可選擇移除最後 2 個 0x00（若存在）
            byte[] finalData = RemoveTrailingDoubleZero(newData);

            // 5) 寫出新的檔案
            string newFilePath = Path.Combine(outputDir, "GameMainConfig");
            File.WriteAllBytes(newFilePath, finalData);

            processedCount++;
            Console.WriteLine("  => Successfully created new file: " + newFilePath);
        }
        await GetDownloadLink.GetDownloadLinkMain(args);
    }

    /// <summary>
    /// 在 byte[] 中找到指定的 16 進位序列，回傳起始位置；若找不到則 -1。
    /// </summary>
    static int FindSequence(byte[] array, string hexSequence)
    {
        int sequenceByteLength = hexSequence.Length / 2;  // 每 2 字元 = 1 byte

        for (int i = 0; i <= array.Length - sequenceByteLength; i++)
        {
            bool found = true;
            for (int j = 0; j < hexSequence.Length; j += 2)
            {
                byte byteValue = byte.Parse(
                    hexSequence.Substring(j, 2),
                    System.Globalization.NumberStyles.HexNumber);

                if (array[i + (j / 2)] != byteValue)
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }

        return -1;
    }

    /// <summary>
    /// 若最後 2 bytes 為 0x00 0x00，就移除之；否則維持原樣。
    /// </summary>
    static byte[] RemoveTrailingDoubleZero(byte[] data)
    {
        if (data.Length >= 2 &&
            data[data.Length - 2] == 0x00 &&
            data[data.Length - 1] == 0x00)
        {
            // 重新建立一個少 2 bytes 的陣列
            byte[] trimmed = new byte[data.Length - 2];
            Buffer.BlockCopy(data, 0, trimmed, 0, trimmed.Length);
            Console.WriteLine("  => Removed trailing 0x00 0x00.");
            return trimmed;
        }
        return data;
    }
}
