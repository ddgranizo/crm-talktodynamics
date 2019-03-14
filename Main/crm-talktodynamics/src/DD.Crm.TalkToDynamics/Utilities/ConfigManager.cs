using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Utilities
{
    public class ConfigManager
    {
        static public string GetAppConfig(string configName)
        {
            string config = string.Empty;
            try
            {
                config = ConfigurationManager.AppSettings[configName];
            }
            catch (Exception)
            {
                return null;
            }
            return config;
        }
    }
}
