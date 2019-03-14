using DD.Crm.TalkToDynamics.Events;
using DD.Crm.TalkToDynamics.Utilities;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics
{
    class Program
    {
        private static SpeechManager speachManager;
        static void Main(string[] args)
        {
            var suscriptionId = SecretManager.GetSecret("suscriptionId", "YourSuscriptionIdHere");
            var region = SecretManager.GetSecret("regionId", "YourRegionHere");
            var luisApplicationId = SecretManager.GetSecret("luisApplicationId", "YourLuisApplicationIdHere");
            var luisSecretKey = SecretManager.GetSecret("luisKey", "YourSecretKeyHere");
            var crmPassword = SecretManager.GetSecret("crmPassword", "YourCrmPasswordHere");


            RecognitionManager recManager =  new RecognitionManager(suscriptionId, region, "es-es");
            speachManager = new SpeechManager();
            ConversationManager conversation = new ConversationManager(luisApplicationId, luisSecretKey, crmPassword);
            conversation.Context.OnMiddleConversationResponse += Context_OnMiddleConversationResponse;
            conversation.OnProcessedRequest += Conversation_OnProcessedRequest;


            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("Talk");
                string text = recManager.Recognice().Result;
                Console.WriteLine($"-{text}");
                var response = conversation.NewRequest(text);
                Console.WriteLine($"\t-{response.Text}");
                Console.WriteLine($"-{text}");
                speachManager.Speak(response.Text);
            }
       
        }

        private static void Conversation_OnProcessedRequest(object sender, ConversationIntentEventArgs args)
        {
            Console.WriteLine("Response:");
            Console.WriteLine(args.Response.RawJson);
        }

        private static void Context_OnMiddleConversationResponse(object sender, ConversationResponseEventArgs args)
        {
            speachManager.Speak(args.Response.Text);
        }
    }
}
