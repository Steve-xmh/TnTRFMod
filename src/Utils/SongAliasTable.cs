using System.Text.Json.Nodes;

namespace TnTRFMod.Utils;

public static class SongAliasTable
{
    private static readonly Dictionary<string, string> AliasTable = new();

    public static async Task ReloadAliasTable()
    {
        AliasTable.Clear();
        try
        {
            var aliasTableFile = Path.Combine(TnTrfMod.Dir, "alias.json");
            Logger.Info($"Loading alias table from {aliasTableFile}");
            if (!File.Exists(aliasTableFile)) return;
            var aliasTableData = await File.ReadAllTextAsync(aliasTableFile);
            var aliasTable = JsonNode.Parse(aliasTableData);
            foreach (var kv in aliasTable.AsObject())
                if (kv.Value.AsValue().TryGetValue<string>(out var alias))
                    AliasTable[kv.Key.ToLower()] = alias;

            Logger.Info($"Loaded {AliasTable.Count} alias table");
        }
        catch (Exception e)
        {
            Logger.Warn("Can't load alias table:");
            Logger.Warn(e.Message);
        }
    }

    public static bool TryGetAlias(string key, out string alias)
    {
        return AliasTable.TryGetValue(key, out alias);
    }
}