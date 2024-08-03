using System;
using System.Diagnostics;
using System.IO;

class APK
{
    public static void Main(string[] args)
    {
        
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
