using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageSyncingBot.Middleware
{

    /// <summary>
    /// This interface should be implemented when you create your own User Conversation storage 
    /// It should be defined in a way you are capable to map conversations and conversations references to specific user.
    /// </summary>
    public interface IUserConversationsStorageProvider
    {
        void AddConvIdReference(string userId, string convId, ConversationReference cr);
        Dictionary<string, ConversationReference> GetOtherUserConversations(string convId);

    }
    
}
