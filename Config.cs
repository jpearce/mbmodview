using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Forms;

namespace MBScriptEditor
{
    /// <summary>Static config class, don't care for the designer-based one.</summary>
    internal static class Config
    {
        #region private config settings

        
        private static Configuration config;
        private static Dictionary<String, String> settings; //will just save any time it's accessed
        #endregion        

        internal static String GetSetting(String KeyName)
        {
            String retVal;
            return settings.TryGetValue(KeyName, out retVal) ? retVal : String.Empty;
        }

        internal static void SetSetting(String KeyName, String KeyValue)
        {
            if (settings.ContainsKey(KeyName)) { settings[KeyName] = KeyValue; }
            else { settings.Add(KeyName, KeyValue); }
        }

        internal static void SaveAll()
        {
            //drop all, easier than checking for new 
            while (config.AppSettings.Settings.AllKeys.Length > 0)
            {
                config.AppSettings.Settings.Remove(config.AppSettings.Settings.AllKeys[0]);
            }
            foreach (KeyValuePair<String, String> kvp in settings)
            {
                config.AppSettings.Settings.Add(kvp.Key, kvp.Value);
            }
            config.Save(ConfigurationSaveMode.Minimal);
        }

        static Config()
        {
            config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);
            List<String> tempkeys = new List<String>(config.AppSettings.Settings.AllKeys);
            settings = new Dictionary<string, string>(tempkeys.Count);
            for (int i = 0; i < tempkeys.Count; ++i)
            {
                settings.Add(tempkeys[i], config.AppSettings.Settings[tempkeys[i]].Value);
            }                   
        }


    }
}
