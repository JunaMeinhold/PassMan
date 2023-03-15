namespace PassMan.Model
{
    using Newtonsoft.Json;
    using System.IO;

    public class Config
    {
        private const string ConfigName = "config.json";

        static Config()
        {
            if (!File.Exists(ConfigName))
            {
                Default = new();
            }
            else
            {
                var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigName));
                config ??= new();
                Default = config;
            }
        }

        public static Config Default { get; }

        public DataSourceMode DataSource { get; set; } = DataSourceMode.Appdata;

        public void Save()
        {
            File.WriteAllText(ConfigName, JsonConvert.SerializeObject(this));
        }
    }
}