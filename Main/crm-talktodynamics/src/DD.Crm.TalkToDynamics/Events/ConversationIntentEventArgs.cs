using DD.Crm.TalkToDynamics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Events
{
    public class ConversationIntentEventArgs : EventArgs
    {
        public LuisResponse Response { get; set; }
        public string Input { get; set; }
        public ConversationIntentEventArgs(string input, LuisResponse response)
        {
            this.Input = input;
            this.Response = response;
        }
    }
}
