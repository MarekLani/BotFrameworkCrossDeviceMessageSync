using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Schema;

namespace MessageSyncingBot.Helpers
{
    /// <summary>
    /// This interface should be implemented when you create your own User Conversation storage 
    /// It should be defined in a way you are capable to map conversations and conversations references to specific user.
    /// </summary>
    public interface IUserConversationsStorage
    {
        void AddConvIdReference(string userId, string convId, ConversationReference cr);
        Dictionary<string, ConversationReference> GetOtherUserConversations(string convId);
        
    }

    public class UserConversationsStaticStorage: IUserConversationsStorage
    {
        private static List<UserConversations> userConversations = new List<UserConversations>();

        public void AddConvIdReference(string userId, string convId, ConversationReference cr)
        {
            if (userConversations.Where(u => u.UserId == userId).Any())
            {
                //Try add as we want to add only if ConvId + Conv Ref does not exist in the Mao
                userConversations.Where(u => u.UserId == userId).FirstOrDefault().ConversationIdReferenceMap.TryAdd(convId, cr);
            }
            else
            {
                userConversations.Add(new UserConversations(userId, convId, cr));
            }
        }

        public Dictionary<string, ConversationReference> GetOtherUserConversations(string convId)
        {
            return userConversations.Where(u => u.ConversationIdReferenceMap.ContainsKey(convId)).FirstOrDefault().ConversationIdReferenceMap;
        }
    }

    public class UserConversations
    { 
        public UserConversations(string userId, string conversationId, ConversationReference cr)
        {
            this.ConversationIdReferenceMap.Add(conversationId,cr);
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
