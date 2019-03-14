using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Utilities
{
    public static class RequestManager
    {
        public static T Get<T>(string url) where T : IRaw
        {
            string html = string.Empty;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }
            T deserialized = JsonConvert.DeserializeObject<T>(html);
            if (deserialized!=null)
            {
                deserialized.RawJson = html;
            }
            return deserialized;
        }
    }
}
