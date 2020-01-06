using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Thierry
{
    public class Configuration
    {
        static Configuration()
        {
        }

        private Configuration()
        {
        }

        public static Configuration Config { get; private set; } = new Configuration();

        public List<Guild> Guilds { get; set; }


        public string Token { get; set; }

        public static void LoadConfig()
        {
            string json;

            var fileStream = new FileStream(@"config.json", FileMode.Open, FileAccess.Read);
            using (var reader = new StreamReader(fileStream))
            {
                json = reader.ReadToEnd();
            }

            Config = JsonConvert.DeserializeObject<Configuration>(json);
        }

        public static void SaveConfig()
        {
            var json = JsonConvert.SerializeObject(Config);

            using (var writer = new StreamWriter(@"config.json", false))
            {
                writer.Write(json);
            }
        }
    }
}