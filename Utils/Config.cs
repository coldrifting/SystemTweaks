using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SystemTweaks.Utils;

public sealed class CaseInsensitiveDictionary<TValue>() : Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Config))]
internal partial class ConfigContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Config.FileTypeAction))]
internal partial class FileTypeActionContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Config.FileType))]
internal partial class FileTypeContext : JsonSerializerContext
{
}

[UsedImplicitly]
public class Config
{
    public required string ProgramsToInstallDirectory { get; set; }
    public required List<string> WinGetPackagesToInstall { get; set; }
    public required List<string> AppxPackagesToRemove { get; set; }
    public required List<string> ShellNewEntriesToDelete { get; set; }
    public required CaseInsensitiveDictionary<string> Resources { get; set; }
    public required CaseInsensitiveDictionary<FileTypeAction> FileTypeActions { get; set; }
    public required List<FileType> FileTypes { get; set; }

    public class FileTypeAction
    {
        public required string Name { get; set; }
        public required string Label { get; set; }
        public required string Command { get; set; }
        public bool HasShieldIcon { get; set; }
    }

    public class FileType
    {
        public required string Extension { get; set; }
        public required string Type { get; set; }
        public string? ProgId { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? DefaultAction { get; set; }
        public string[] Actions { get; set; } = [];
        public string[] ActionsExtended { get; set; } = [];
        public string[] ActionsHidden { get; set; } = [];
        public bool TakeOwnership { get; set; }
    }

    public static Config? ReadConfigFile(string configFilePath)
    {
        string jsonText = File.ReadAllText(configFilePath);
        return System.Text.Json.JsonSerializer.Deserialize<Config>(jsonText, ConfigContext.Default.Config);
    }
}
