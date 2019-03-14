using DD.Crm.TalkToDynamics.Events;
using DD.Crm.TalkToDynamics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Utilities
{

    public delegate void ConversationIntentHandler(object sender, ConversationIntentEventArgs args);

    public delegate void ConversationResponseHandler(object sender, ConversationResponseEventArgs args);

    public class ConversationManager
    {
        private string baseUrl = 
            "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{0}?staging=true&verbose=true&timezoneOffset=-360&subscription-key={1}&q={2}";
        public List<string> Requests { get; set; }
        public bool IsConnected { get; set; }

        public event ConversationIntentHandler OnProcessedRequest;

        public ContextManager Context { get; set; }

        public string LuisApplicationId { get; set; }
        public string LuisSecretKey { get; set; }

        public ConversationManager(string luisApplicationId, string luisSecretKey, string crmPassword)
        {
            
            this.LuisApplicationId = luisApplicationId;
            this.LuisSecretKey = luisSecretKey;

            this.Context = new ContextManager(crmPassword);

        }


        public ConversationResponse NewRequest(string request)
        {
            string composedUrl = string.Format(baseUrl, this.LuisApplicationId, this.LuisSecretKey, request);
            var response = RequestManager.Get<LuisResponse>(composedUrl);
            OnProcessedRequest?.Invoke(this, new ConversationIntentEventArgs(request, response));
            return this.Context.ProcessInContext(response);
        }
        
    }
}
