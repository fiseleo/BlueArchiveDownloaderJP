using BAdownload;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

class APK
{
    public static void Main(string[] args)
    {
        Console.WriteLine("To specify a specific version of the game, use the \"-f\" argument.");
        Console.WriteLine("For example:");
        Console.WriteLine("BAdownload.exe -f 1.987654321");
        if (args.Length >= 2)
        {
            if (args[0] == "-f")
            {
                GlobalData.IsForcedVersion = true;
                GlobalData.ForcedVersion = args[1];
            }
        }
        string pythonInterpreterRelativePath = @"python\python.exe";
        string pythonScriptRelativePath = @"python\download_apk.py";
        string currentDirectory = Environment.CurrentDirectory;
        string fullPathToPythonInterpreter = Path.GetFullPath(Path.Combine(currentDirectory, pythonInterpreterRelativePath));
        string fullPathToPythonScript = Path.GetFullPath(Path.Combine(currentDirectory, pythonScriptRelativePath));
        Console.WriteLine($"Python Interpreter Path: {fullPathToPythonInterpreter}");
        Console.WriteLine($"Python Script Path: {fullPathToPythonScript}");
        if (!File.Exists(fullPathToPythonInterpreter))
        {
            Console.WriteLine($"Error: Python interpreter not found at {fullPathToPythonInterpreter}");
            return;
        }

        if (!File.Exists(fullPathToPythonScript))
        {
            Console.WriteLine($"Error: Python script not found at {fullPathToPythonScript}");
            return;
        }

        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = fullPathToPythonInterpreter;
        start.Arguments = fullPathToPythonScript;
        if (GlobalData.IsForcedVersion)
            start.Arguments += $" -f {GlobalData.ForcedVersion}";
        start.WorkingDirectory = Path.GetDirectoryName(fullPathToPythonInterpreter); 
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        
        try
        {
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Regex r = new Regex("APK file: (.*).+?\r?$", RegexOptions.Compiled);
                    Match match = r.Match(result);
                    if (match.Success)
                    {
                        GlobalData.XapkFile = Path.Combine(currentDirectory, "python", match.Groups[1].Value);
                    }
                    Console.WriteLine(result);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        APKzip.zipMain(args);
    }
}
