namespace DriveScanner;

public record FileItem(string Name, string Path)
{
    public string? ContainingFolder => System.IO.Path.GetDirectoryName(Path);
}