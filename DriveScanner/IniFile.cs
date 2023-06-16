using System.Text;

namespace DriveScanner;

public class IniFile
{
    public IniFile(string iniFileText) => Parse(iniFileText);

    private readonly Dictionary<string, string?> _keys = new();

    public (string key, string? value) GetKey(string key) => !_keys.ContainsKey(key) ? (key, null) : (key, _keys[key]);

    public bool AddKeyValue(string key, string? value)
    {
        if (_keys.ContainsKey(key)) return false;
        _keys.Add(key, value);
        return true;
    }

    public bool AddOrOverwriteKeyValue(string key, string? value)
    {
        if (_keys.ContainsKey(key))
        {
            _keys[key] = value;
            return false;
        }

        _keys.Add(key, value);
        return true;
    }

    public bool RemoveKey(string key) => _keys.Remove(key);


    private void Parse(string iniFileText)
    {
        //This impl of an ini file there are no [sections], there are only comments "#comment" and key=value
        var lines = iniFileText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith('#')) continue;
            if (line.StartsWith('[')) continue;
            var keyValuePair = line.Split('=', 2);
            if (keyValuePair.Length != 2) continue;
            var key = keyValuePair[0];
            var value = keyValuePair[1];
            _keys.Add(key, value);
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in _keys) sb.AppendLine($"{key}={value}");
        return sb.ToString();
    }
}