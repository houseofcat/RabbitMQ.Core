using System.IO;
using Utf8Json;

namespace CookedRabbit.Core.Utils
{
    /// <summary>
    /// A static class to store methods that aid in reading Configs.
    /// </summary>
    public static class ConfigReader
    {
        public static Config ConfigFileRead(string fileNamePath)
        {
            using var stream = new FileStream(fileNamePath, FileMode.Open);

            return JsonSerializer.Deserialize<Config>(stream);
        }

        public static TopologyConfig TopologyConfigFileRead(string fileNamePath)
        {
            using var stream = new FileStream(fileNamePath, FileMode.Open);

            return JsonSerializer.Deserialize<TopologyConfig>(stream);
        }
    }
}
