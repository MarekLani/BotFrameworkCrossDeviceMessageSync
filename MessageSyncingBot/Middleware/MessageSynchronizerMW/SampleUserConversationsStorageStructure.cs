using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageSyncingBot.Middleware
{
    public class SampleUserConversationsStorageStructure
    {
        public SampleUserConversationsStorageStructure(string userId, string conversationId, ConversationReference cr)
        {
            this.ConversationIdReferenceMap.Add(conversationId, cr);
            this.UserId = userId;
        }

        public string UserId { get; set; }
        public Dictionary<string, ConversationReference> ConversationIdReferenceMap { get; set; } = new Dictionary<string, ConversationReference>();

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
}
