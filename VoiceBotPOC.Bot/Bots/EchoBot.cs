// Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.15.2

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VoiceBotPOC.Bot.Models;

namespace VoiceBotPOC.Bot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private const string _wakeKeyWord = "hey emma";

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string userUtterance = turnContext.Activity.Text?.ToLower().Split(_wakeKeyWord, StringSplitOptions.RemoveEmptyEntries)[0];

            //string replyText = await TranslateText(userUtterance).ConfigureAwait(false);

            var replyText = $"{userUtterance}";
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }

        private async Task<string> TranslateText(string textToTranslate)
        {
            string key = "[Translator Key]";
            string endpoint = "[Translator Endpoint]";
            string location = "[Translator location]";

            //string route = "/translate?api-version=3.0&from=en&to=es";
            //Auto detect source language, by removing from qs
            string route = $"/translate?api-version=3.0&to={Environment.GetEnvironmentVariable("TranslateToLanguage")}";

            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    var jsonString = Regex.Unescape(result);
                    TextTranslationResult jResult = JsonConvert.DeserializeObject<TextTranslationResult>(jsonString.Remove(jsonString.Length - 1, 1).Remove(0, 1));

                    return jResult.Translations.FirstOrDefault().Text;
                }
                catch (Exception ex)
                {
                    return $"error: {ex.ToString()}";
                }
            }
        }
    }
}