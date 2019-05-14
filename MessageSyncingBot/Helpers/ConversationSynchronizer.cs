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
        private static List<UserConversations> userConversations = new List<UserConversations>();
        private static List<ConvIdReferenceMap> convIdReferenceMap = new List<ConvIdReferenceMap>();

        public static async Task ResendMessage(ITurnContext turnContext, CancellationToken cancellationToken)
        {

            //Map Conversations to UserId
            if (turnContext.Activity != null)
            {
                var userConversation = userConversations.Where(u => u.ConversationIdReferenceMap.ContainsKey(turnContext.Activity.Conversation.Id)).FirstOrDefault();
                if(userConversation != null)
                {
                    foreach (var c in userConversation.ConversationIdReferenceMap) {
                        //We do not want to resend message to same conversation
                        //if (turnContext.Activity.Conversation.Id == c.Key)
                        //    return;

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
                        a.Conversation.Id = c.Key;

                        var transcript = new Transcript(new List<Activity>() { a as Activity });

                        var connectorClient = turnContext.TurnState.Get<ConnectorClient>(typeof(IConnectorClient).FullName);
                        await connectorClient.Conversations.SendConversationHistoryAsync(c.Key, transcript, cancellationToken: cancellationToken);
                    }
                    
                }
            }
        }

        internal static void AddConversation(string userId, string conversationId)
        {
            if(userConversations.Where(u => u.UserId == userId).Any())
            {
                userConversations.Where(u => u.UserId == userId).FirstOrDefault().ConversationIdReferenceMap.Add(conversationId,null);
            }
            else
            {
                userConversations.Add(new UserConversations(userId, conversationId));
            }
        }

        internal static void AddConvIdReference (string convId, ConversationReference cr)
        {
            userConversations.Where(u => u.ConversationIdReferenceMap.ContainsKey(convId)).FirstOrDefault().ConversationIdReferenceMap[convId] = cr;
        }

        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

        internal static Dictionary<string,ConversationReference> GetConvReferences(string convId)
        {
            return userConversations.Where(uc => uc.ConversationIdReferenceMap.ContainsKey(convId)).FirstOrDefault().ConversationIdReferenceMap;
        }

        private static IActivity CloneActivity(IActivity activity)
        {
            activity = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(activity, _jsonSettings));
            return activity;

        }

    }
}
