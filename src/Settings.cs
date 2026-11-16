using System;
using System.IO;
using System.Xml.Serialization;

namespace YodaFy
{
    [Serializable]
    public class Settings
    {
        public bool AutomationEnabled;
        public bool BrowserEnabled;
        public int IdleMilliseconds = 1800;
        public string[] Processes = new string[] { "Discord", "WhatsApp", "Telegram", "slack", "ms-teams", "Teams", "Signal" };
        public string[] BrowserTitles = new string[] { "Discord", "WhatsApp", "Telegram", "Slack", "Microsoft Teams", "Signal" };

        public Settings Copy()
        {
            return new Settings { AutomationEnabled = AutomationEnabled, BrowserEnabled = BrowserEnabled,
                IdleMilliseconds = IdleMilliseconds, Processes = (string[])Processes.Clone(),
                BrowserTitles = (string[])BrowserTitles.Clone() };
        }

        public static string SettingsPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YodaFy", "settings.xml"); }
        }

        public static Settings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new Settings();
                using (FileStream stream = File.OpenRead(SettingsPath))
                {
                    Settings value = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
                    value.IdleMilliseconds = Math.Max(1000, Math.Min(10000, value.IdleMilliseconds));
                    if (value.Processes == null) value.Processes = new string[0];
                    if (value.BrowserTitles == null) value.BrowserTitles = new string[0];
                    return value;
                }
            }
            catch { return new Settings(); }
        }

        public void Save()
        {
            string directory = Path.GetDirectoryName(SettingsPath);
            Directory.CreateDirectory(directory);
            string temporary = SettingsPath + ".tmp";
            using (FileStream stream = File.Create(temporary)) new XmlSerializer(typeof(Settings)).Serialize(stream, this);
            if (File.Exists(SettingsPath)) File.Replace(temporary, SettingsPath, null);
            else File.Move(temporary, SettingsPath);
        }
    }
}
