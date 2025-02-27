using System.Collections.Concurrent;
using Fleck;
using WebSocketBoilerplate;

namespace Api.WebSockets;

public interface IConnectionManager
{
    Task<Dictionary<string, string>> GetAllSocketIdsWithConnectionId();
    public Task<Dictionary<string, string>> GetAllConnectionIdsWithSocketId();
    public Task<ConcurrentDictionary<string, HashSet<string>>> GetAllTopicsWithMembers();
    public Task<ConcurrentDictionary<string, HashSet<string>>> GetAllMembersWithTopics();
    Task AddToTopic(string topic, string memberId, TimeSpan? expiry = null);
    Task RemoveFromTopic(string topic, string memberId);
    Task<List<string>> GetMembersFromTopicId(string topic);
    Task<List<string>> GetTopicsFromMemberId(string memberId);
    Task<string> GetClientIdFromSocketId(string socketId);
    Task OnOpen(IWebSocketConnection socket, string clientId);
    Task OnClose(IWebSocketConnection socket, string clientId);
    Task BroadcastToTopic<T>(string topic, T message) where T : BaseDto;
}