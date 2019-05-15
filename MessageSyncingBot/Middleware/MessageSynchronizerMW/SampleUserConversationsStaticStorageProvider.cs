using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageSyncingBot.Middleware
{
    public class SampleUserConversationsStaticStorageProvider : IUserConversationsStorageProvider
    {
        private static List<SampleUserConversationsStorageStructure> userConversations = new List<SampleUserConversationsStorageStructure>();

        public void AddConvIdReference(string userId, string convId, ConversationReference cr)
        {
            if (userConversations.Where(u => u.UserId == userId).Any())
            {
                //Try add as we want to add only if ConvId + Conv Ref does not exist in the Mao
                userConversations.Where(u => u.UserId == userId).FirstOrDefault().ConversationIdReferenceMap.TryAdd(convId, cr);
            }
            else
            {
                userConversations.Add(new SampleUserConversationsStorageStructure(userId, convId, cr));
            }
        }

        public Dictionary<string, ConversationReference> GetOtherUserConversations(string convId)
        {
            return userConversations.Where(u => u.ConversationIdReferenceMap.ContainsKey(convId)).FirstOrDefault().ConversationIdReferenceMap;
        }
    }
}
