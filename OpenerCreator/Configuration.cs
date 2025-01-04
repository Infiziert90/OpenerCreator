using System.IO;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace OpenerCreator;

public class Configuration : IPluginConfiguration
{
    public bool AbilityAnts = true;
    public int CountdownTime = 7;
    public bool IgnoreTrueNorth = true;
    public bool IsCountdownEnabled = false;
    public bool StopAtFirstMistake = false;
    public int Version { get; set; } = 1;

    public static Configuration Load()
    {
        return Plugin.PluginInterface.ConfigFile.Exists
                   ? JsonConvert.DeserializeObject<Configuration>(
                         File.ReadAllText(Plugin.PluginInterface.ConfigFile.FullName)) ?? new Configuration()
                   : new Configuration();
    }

    public void Save()
    {
        File.WriteAllText(Plugin.PluginInterface.ConfigFile.FullName, JsonConvert.SerializeObject(this));
    }
}
