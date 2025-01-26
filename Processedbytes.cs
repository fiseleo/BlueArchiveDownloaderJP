using DownloadGameData;
using MemoryPack;
using Newtonsoft.Json;

class Processedbytes
{
    public static async Task ProcessedbytesMain(string[] args)
    {
        string rootDirectory = Directory.GetCurrentDirectory();
        string inputPath = Path.Combine(rootDirectory, "Downloads", "bytes");
        string outputPath = Path.Combine(rootDirectory, "Downloads", "json");
        string[] inputPathFile = Directory.GetFiles(inputPath);
        foreach (string filePath in inputPathFile)
        {
            byte[] bin = File.ReadAllBytes(filePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string outputFilePath = Path.Combine(outputPath, $"{fileNameWithoutExtension}.json");
            string jsonString;
            if (filePath.EndsWith("TableCatalog.bytes"))
                jsonString = JsonConvert.SerializeObject(
                    MemoryPackSerializer.Deserialize<TableCatalog>(bin),
                    Formatting.Indented
                );
            else
                jsonString = JsonConvert.SerializeObject(
                    MemoryPackSerializer.Deserialize<Media.Service.MediaCatalog>(bin),
                    Formatting.Indented
                );
            using (StreamWriter writer = File.CreateText(outputFilePath))
            {
                writer.Write(jsonString);
            }

            Console.WriteLine($"{fileNameWithoutExtension}.bytes has been converted to {fileNameWithoutExtension}.json");
        }
        await DownloadFiles.DownloadMain(args);
    }
}

[MemoryPackable]
public partial class TableBundle
{
    public required string Name { get; set; }
    public long Size { get; set; }
    public long Crc { get; set; }
    public bool IsInBuild { get; set; }
    public bool IsChanged { get; set; }
    public bool IsPrologue { get; set; }
    public bool IsSplitDownload { get; set; }
    public List<string>? Includes { get; set; }
}

[MemoryPackable]
public partial class TableCatalog
{
    public required Dictionary<string, TableBundle> Table { get; set; }
}

namespace Media.Service
{
    public enum MediaType
    {
        None = 0,
        Audio = 1,
        Video = 2,
        Texture = 3,
    }

    [MemoryPackable]
    public partial class Media
    {
        [JsonProperty("path")]
        public required string Path { get; set; }
        public required string FileName { get; set; }
        public long Bytes { get; set; }
        public long Crc { get; set; }
        public bool IsPrologue { get; set; }
        public bool IsSplitDownload { get; set; }
        public MediaType MediaType { get; set; }
    }

    [MemoryPackable]
    public partial class MediaCatalog
    {
        public required Dictionary<string, Media> Table { get; set; }
    }
}