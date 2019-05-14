using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace MessageSyncingBot.Helpers
{
    public class UserConversations
    { 
        public UserConversations(string userId, string conversationId)
        {
            this.ConversationIdReferenceMap.Add(conversationId,null);
            this.UserId = userId;
        }

        public string UserId { get; set; }
        public Dictionary<string,ConversationReference> ConversationIdReferenceMap { get; set; } = new Dictionary<string, ConversationReference>();

    }

    public class ConvIdReferenceMap
    {
        public ConvIdReferenceMap(string convId, ConversationReference cr)
        {
            this.ConversationId = convId;
            this.ConversationReference = cr;
        }

        public ConvIdReferenceMap(string convId)
        {
            this.ConversationId = convId;
        }

        public string ConversationId { get; set; }
        public ConversationReference ConversationReference { get; set; }

    }
}
