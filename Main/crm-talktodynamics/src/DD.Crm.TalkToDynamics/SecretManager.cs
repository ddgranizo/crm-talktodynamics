using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics
{
    public static class SecretManager
    {

        public static string GetSecret(string secretName, string defaultValue)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName =
                assembly.GetManifestResourceNames()
                .FirstOrDefault(k => k.EndsWith("secrets.json"));
            if (resourceName == null)
            {
                return defaultValue;
            }
            string result = string.Empty;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }
            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
            if (deserialized.ContainsKey(secretName))
            {
                return deserialized[secretName];
            }
            return defaultValue;
        }
    }
}
