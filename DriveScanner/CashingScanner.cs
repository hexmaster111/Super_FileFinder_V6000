using System.Collections.Concurrent;
using System.Diagnostics;

namespace DriveScanner;

public class CashingScanner : IDisposable
{
    private static string ScannerWorkingDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DriveScanner");

    private string DriveLetter { get; }
    private char DriveLetterChar => DriveLetter[0];
    private string CashPath => Path.Combine(ScannerWorkingDir, DriveLetterChar + ".txt");
    private string CashInfoPath => Path.Combine(ScannerWorkingDir, DriveLetterChar + ".ini");

    private const string INI_LAST_SCAN_KEY = "lastscan";

    public bool IsReadyToSearch => IsReadyToSearchInternal();
    public Task ScanAsync() => Task.Run(() => Scan(DriveLetter));
    public FileItem[] Search(string searchPattern) => SearchInternal(searchPattern);
    public Task<FileItem[]> SearchAsync(string searchPattern) => Task.Run(() => Search(searchPattern));


    public CashingScanner(string driveLetter)
    {
        if (string.IsNullOrEmpty(driveLetter))
            throw new ArgumentException("Drive letter is null or empty!", nameof(driveLetter));

        if (!Directory.GetLogicalDrives().Contains(driveLetter))
            throw new InvalidOperationException("Drive not found!");

        DriveLetter = driveLetter;
        if (!Directory.Exists(ScannerWorkingDir)) Directory.CreateDirectory(ScannerWorkingDir);
        if (!File.Exists(CashPath)) File.Create(CashPath);
        if (!File.Exists(CashInfoPath)) File.Create(CashInfoPath);
    }


    private bool IsReadyToSearchInternal()
    {
        var iniFileText = File.ReadAllText(CashInfoPath);
        var iniFile = new IniFile(iniFileText);
        var (_, value) = iniFile.GetKey(INI_LAST_SCAN_KEY);
        if (value is null) return false;
        var pok = DateTime.TryParse(value, out var lastScan);
        if (!pok) return false;
        var now = DateTime.Now;
        var diff = now - lastScan;
        return !(diff.TotalDays > 14);
    }


    private FileItem[] SearchInternal(string searchPattern)
    {
        if (!IsReadyToSearch) throw new InvalidOperationException("Scanner is not ready to search!");

        searchPattern = searchPattern.ToLower();

        var cashFile = File.ReadAllText(CashPath);
        var lines = cashFile.Split(Environment.NewLine);
        List<string> foundLines =
            (from line in lines
                let l = line.ToLower()
                where l.Contains(searchPattern)
                select line)
            .ToList();

        return (from line in foundLines
                let name = Path.GetFileName(line)
                select new FileItem(name, line))
            .ToArray();
    }

    private void Scan(string path)
    {
        ConcurrentQueue<string> dirs = new();
        ConcurrentBag<string> files = new();


        //First we will get all the files in the root directory
        var rootDirs = Directory.GetFiles(path);
        foreach (var dir in rootDirs)
        {
            dirs.Enqueue(dir);
        }


        //The first check will be how we create the first task
        dirs.Enqueue(path);
        var tasks = new List<Task>();

        while (!dirs.IsEmpty)
        {
            while (dirs.Count > 0)
            {
                var gotDir = dirs.TryDequeue(out var currentDir);
                if (!gotDir) continue;

                var task = Task.Run(() =>
                {
                    try
                    {
                        var foundFiles = Directory.GetFiles(currentDir);
                        foreach (var file in foundFiles)
                        {
                            files.Add(file);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }

                    try
                    {
                        var subDirs = Directory.GetDirectories(currentDir);
                        foreach (var subDir in subDirs)
                        {
                            dirs.Enqueue(subDir);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }

        var sortedFiles = files.OrderBy(x => x.Count(c => c == '\\'));
        File.WriteAllText(CashPath, string.Join(Environment.NewLine, sortedFiles));
        var iniFileText = File.ReadAllText(CashInfoPath);
        var iniFile = new IniFile(iniFileText);
        iniFile.AddOrOverwriteKeyValue(INI_LAST_SCAN_KEY, DateTime.Now.ToString());
        File.WriteAllText(CashInfoPath, iniFile.ToString());
    }

    public void Dispose()
    {
    }
}