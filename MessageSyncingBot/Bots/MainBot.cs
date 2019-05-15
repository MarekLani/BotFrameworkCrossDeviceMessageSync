// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using MessageSyncingBot.Bots.Resources;
using MessageSyncingBot.Extensions;
using MessageSyncingBot.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Bot.Connector;
using MessageSyncingBot.Helpers;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;

namespace MessageSyncingBot.Bots
{
    public class MainBot<T> : ActivityHandler where T : Dialog
    {
        private const string WebChatChannelId = "webchat";
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private readonly ILogger _logger;
        private readonly Dialog _dialog;
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IConfiguration _configuration;

        private IStatePropertyAccessor<GlobalUserState> _globalStateAccessor;

   

        public MainBot(ConversationState conversationState, UserState userState, IBotFrameworkHttpAdapter adapter, T dialog, ILogger<MainBot<T>> logger, IConfiguration configuration)
        {
            if (conversationState == null)
            {
                throw new System.ArgumentNullException(nameof(conversationState));
            }

            if (logger == null)
            {
                throw new System.ArgumentNullException(nameof(logger));
            }

            _conversationState = conversationState;
            _userState = userState;
            _logger = logger;
            _dialog = dialog;
            _globalStateAccessor = _userState.CreateProperty<GlobalUserState>(nameof(GlobalUserState));
            _adapter = adapter;
            _configuration = configuration;
            _logger.LogTrace("Turn start.");
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn. This avoids the need to explicitly save changes to state each time
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
    
        // Process incoming message
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            turnContext.OnSendActivities(async (ctx, activities, nextSend) =>
            {
                // run full pipeline
                var responses = await nextSend().ConfigureAwait(false);
                foreach (var activity in activities)
                {
                    foreach (var cr in ConversationSynchronizer.GetConvReferences(activity.Conversation.Id))
                    {
                       if(ctx.Activity.Conversation.Id != cr.Key)
                            await (_adapter as BotFrameworkHttpAdapter).ContinueConversationAsync(_configuration["MicrosoftAppId"], cr.Value, CreateCallback(activity), CancellationToken.None);
                    }
                }
                return responses;
            });

            await ConversationSynchronizer.ResendMessage(turnContext, cancellationToken);

            var state = await _globalStateAccessor.GetAsync(turnContext, () => new GlobalUserState());

            // If the user has not yet been welcomed, welcome them. Make sure the welcome inlcudes a prompt to throw the message away and ask them to enter a new message
            if (state.DidBotWelcomeUser == false)
            {
                // Set state
                state.DidBotWelcomeUser = true;

                // Send welcome message
                var name = turnContext.Activity.From.Name ?? string.Empty;
                await turnContext.SendActivityAsync($"{String.Format(MainBotStrings.Welcome_name, name)}", cancellationToken: cancellationToken);

                // Run or contonue the initial dialog
                await _dialog.Run(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken: cancellationToken);
            }
            else
            {
                // Run the initial dialog
                await _dialog.Run(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken: cancellationToken);
            }
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
    
        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var reference = turnContext.Activity.GetConversationReference();
            ConversationSynchronizer.AddConvIdReference(turnContext.Activity.From.Name, turnContext.Activity.Conversation.Id, reference);
        }

        // Greet when users are added to the conversation.
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
           

            // Welcome each member that was added
            foreach (var member in membersAdded)
            {
                // The bot itself is a conversation member too ... this check makes sure this is not the bot joining
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // Look for web chat channel because it sends this event when a user messages so we want to only do this if not webchat. Webchat welcome is handled on receipt of first message
                    if (turnContext.Activity.ChannelId.ToLower() != WebChatChannelId)
                    {
                        // Set state
                        var state = await _globalStateAccessor.GetAsync(turnContext, () => new GlobalUserState());
                        state.DidBotWelcomeUser = true;

                        // Send welcome message
                        var name = member.Name ?? string.Empty;
                        await turnContext.SendActivityAsync($"{String.Format(MainBotStrings.WelcomeToTheConversation_name, name)}", cancellationToken: cancellationToken);

                        // Run the initial dialog
                        await _dialog.Run(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken: cancellationToken);
                    }
                }
            }
        }


    }
}
