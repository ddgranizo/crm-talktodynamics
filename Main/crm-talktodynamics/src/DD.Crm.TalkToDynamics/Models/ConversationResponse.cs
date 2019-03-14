using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Models
{
    public class ConversationResponse
    {
        public string Text { get; set; }
        public ConversationResponse(string text)
        {
            this.Text = text;
        }
    }
}
