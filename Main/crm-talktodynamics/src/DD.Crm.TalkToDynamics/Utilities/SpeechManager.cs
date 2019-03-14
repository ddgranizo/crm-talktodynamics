using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Utilities
{
    public class SpeechManager
    {
        public SpeechSynthesizer Speaker { get; set; }
        public SpeechManager()
        {
            this.Speaker = new SpeechSynthesizer();
        }

        public void Speak(string text)
        {
            this.Speaker.Speak(text);
        }
    }
}
