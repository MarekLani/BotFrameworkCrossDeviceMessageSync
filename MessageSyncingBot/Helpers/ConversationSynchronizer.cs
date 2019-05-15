using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessageSyncingBot.Helpers
{
    public class ConversationSynchronizer
    {
        public static UserConversationsStaticStorage ucs = new UserConversationsStaticStorage();


        public static async Task ResendMessage(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity != null)
            {
                var conversationsWithRef = ucs.GetOtherUserConversations(turnContext.Activity.Conversation.Id);

                //We check if there are more conversations with the same user opened
                if (conversationsWithRef.Count > 1)
                {
                    if (turnContext.Activity.From == null)
                    {
                        turnContext.Activity.From = new ChannelAccount();
                    }

                    if (string.IsNullOrEmpty((string)turnContext.Activity.From.Properties["role"]))
                    {
                        turnContext.Activity.From.Properties["role"] = "user";
                    }
                    var a = CloneActivity(turnContext.Activity);

                    //Activity ID needs to be unique
                    a.Id = string.Concat(a.Conversation.Id + "_" + Guid.NewGuid().ToString());
                    a.Timestamp = DateTimeOffset.UtcNow;
                    a.ChannelData = string.Empty; // WebChat uses ChannelData for id comparisons, so we clear it here

                    var connectorClient = turnContext.TurnState.Get<ConnectorClient>(typeof(IConnectorClient).FullName);

                    foreach (var c in conversationsWithRef) {
                        //We do not want to resend message to same conversation
                        if (turnContext.Activity.Conversation.Id == c.Key)
                            continue;

                        a.Conversation.Id = c.Key;
                        var transcript = new Transcript(new List<Activity>() { a as Activity });
                       
                        await connectorClient.Conversations.SendConversationHistoryAsync(c.Key, transcript, cancellationToken: cancellationToken);
                    }
                    
                }
            }
        }

        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

        internal static Dictionary<string,ConversationReference> GetConvReferences(string convId)
        {
            return ucs.GetOtherUserConversations(convId);
        }

        private static IActivity CloneActivity(IActivity activity)
        {
            activity = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(activity, _jsonSettings));
            return activity;

        }

        internal static void AddConvIdReference(string fromId, string convId, ConversationReference reference)
        {
            ucs.AddConvIdReference(fromId, convId, reference);
        }
    }
}
