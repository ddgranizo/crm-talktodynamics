using DD.Crm.TalkToDynamics.Exceptions;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Utilities
{
    public class RecognitionManager
    {
        public SpeechConfig Speacher { get; set; }
        public RecognitionManager(string suscriptionId, string region, string locale)
        {
            this.Speacher = SpeechConfig.FromSubscription(suscriptionId, region);
            this.Speacher.SpeechRecognitionLanguage = locale;
        }

        public async Task<string> Recognice()
        {
            using (var recognizer = new SpeechRecognizer(this.Speacher))
            {
                var result = await recognizer.RecognizeOnceAsync();
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    return result.Text;
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    return null;
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    throw new RecognitionException(cancellation);
                }
                return null;
            }
        }
    }
}
