using System.IO;
using System.Threading.Tasks;
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

        public static async Task<Config> ConfigFileReadAsync(string fileNamePath)
        {
            using var stream = new FileStream(fileNamePath, FileMode.Open);

            return await JsonSerializer.DeserializeAsync<Config>(stream).ConfigureAwait(false);
        }

        public static TopologyConfig TopologyConfigFileRead(string fileNamePath)
        {
            using var stream = new FileStream(fileNamePath, FileMode.Open);

            return JsonSerializer.Deserialize<TopologyConfig>(stream);
        }

        public static async Task<TopologyConfig> TopologyConfigFileReadAsync(string fileNamePath)
        {
            using var stream = new FileStream(fileNamePath, FileMode.Open);

            return await JsonSerializer.DeserializeAsync<TopologyConfig>(stream).ConfigureAwait(false);
        }
    }
}
