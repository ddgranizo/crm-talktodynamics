using DD.Crm.TalkToDynamics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Events
{
    public class ConversationResponseEventArgs: EventArgs
    {
        public ConversationResponse Response { get; set; }
        public ConversationResponseEventArgs(ConversationResponse response)
        {
            this.Response = response;
        }
    }
}
