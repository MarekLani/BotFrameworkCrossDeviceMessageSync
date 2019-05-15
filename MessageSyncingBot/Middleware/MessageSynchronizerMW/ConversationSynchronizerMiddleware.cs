using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessageSyncingBot.Middleware
{
    public class ConversationSynchronizationMiddleware: IMiddleware
    {
        private IUserConversationsStorageProvider _ucs;
        private BotAdapter _adapter;
        private IConfiguration _configuration;

        public ConversationSynchronizationMiddleware(IUserConversationsStorageProvider ucs, BotAdapter adapter, IConfiguration configuration)
        {
            _ucs = ucs;
            _adapter = adapter;
            _configuration = configuration;
        }

        public async Task ResendUserMessage(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity != null)
            {
                var conversationsWithRef = _ucs.GetOtherUserConversations(turnContext.Activity.Conversation.Id);

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

                    foreach (var c in conversationsWithRef)
                    {
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


        private static IActivity CloneActivity(IActivity activity)
        {
            activity = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(activity, _jsonSettings));
            return activity;

        }


        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {

            if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                
                var reference = turnContext.Activity.GetConversationReference();
                _ucs.AddConvIdReference(turnContext.Activity.From.Name, turnContext.Activity.Conversation.Id, reference);
            }

            if (turnContext.Activity.Type != ActivityTypes.Event)
            {
                try
                {

                    await ResendUserMessage(turnContext, cancellationToken);
                    //await Task.Delay(500);
                    // process bot logic
                    

                }
                catch (Exception) { }
                turnContext.OnSendActivities(async (ctx, activities, nextSend) =>
                {
                    // run full pipeline
                    var responses = await nextSend().ConfigureAwait(false);
                    foreach (var activity in activities)
                    {
                        foreach (var cr in _ucs.GetOtherUserConversations(activity.Conversation.Id))
                        {
                            if (ctx.Activity.Conversation.Id != cr.Key)
                                await _adapter.ContinueConversationAsync(_configuration["MicrosoftAppId"], cr.Value, CreateCallback(activity), CancellationToken.None);
                        }
                    }
                    return responses;
                });
            }



            

            //turnContext.OnUpdateActivity(async (ctx, activities, nextUpdate) =>
            //{
            //    //Save Conversation Reference
            //    var reference = ctx.Activity.GetConversationReference();
            //    _ucs.AddConvIdReference(ctx.Activity.From.Name, ctx.Activity.Conversation.Id, reference);

            //    var responses = await nextUpdate().ConfigureAwait(false);
            //    return responses;
            //});


        }

        private BotCallbackHandler CreateCallback(Activity activity)
        {
            return async (turnContext, token) =>
            {
                try
                {
                    //simulate delay
                    // await Task.Delay(5000);

                    // Send the user a proactive confirmation message.
                    await turnContext.SendActivityAsync(activity);
                }
                catch (Exception e)
                {
                    //TODO handle error logging
                    throw e;
                }
            };
        }
    }
}
