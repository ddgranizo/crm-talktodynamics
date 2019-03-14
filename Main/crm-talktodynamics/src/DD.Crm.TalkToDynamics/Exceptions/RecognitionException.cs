using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Exceptions
{
    public class RecognitionException: Exception
    {
        public CancellationDetails Details { get; set; }

        public RecognitionException(CancellationDetails details)
        {
            this.Details = details;
        }
    }
}
