// <author>Sourav Mondal/author>
// <summary>Chat application</summary>

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SimpleChat.Data;
using System.Linq;

namespace SimpleChat.Hubs;

/// <summary>
/// Real-time 1-to-1 chat hub. No channels/groups — messages are routed
/// directly to a specific target user's active connections.
/// </summary>
public class ChatHub : Hub
{
    private readonly ChatRepository _repo;

    // Maps UserId -> set of active SignalR connection ids (a user can have
    // multiple tabs/devices open). Static so it's shared across the app.
    private static readonly ConcurrentDictionary<int, HashSet<string>> OnlineUsers = new();
    private static readonly ConcurrentDictionary<string, int> ConnectionToUser = new();

    public ChatHub(ChatRepository repo)
    {
        _repo = repo;
    }

    // Called by the client right after connecting, to register who they are.
   
    public async Task Register(int userId)
    {
        var connections = OnlineUsers.GetOrAdd(userId, _ => new HashSet<string>());
        bool firstConnection;
        lock (connections)
        {
            firstConnection = connections.Count == 0;
            connections.Add(Context.ConnectionId);
        }

        // Remember which user this connection belongs to (for disconnect)
        ConnectionToUser[Context.ConnectionId] = userId;

        // Only announce "online" when it's their first active connection
        if (firstConnection)
            await Clients.All.SendAsync("PresenceChanged", userId, true);
    }

    // Send a direct message from the sender to a specific receiver.
    public async Task SendMessage(int senderId, int receiverId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        content = content.Trim();

        // 1) ALWAYS persist first — even if receiver is offline 👈 critical
        var messageId = await _repo.SaveMessageAsync(senderId, receiverId, content);
        var sender = await _repo.GetUserByIdAsync(senderId);

        var payload = new
        {
            messageId,
            senderId,
            receiverId,
            senderName = sender?.DisplayName ?? "Unknown",
            content,
            sentAt = DateTime.UtcNow
        };

        // 2) Push to receiver only IF online (offline is fine — it's in the DB)
        if (OnlineUsers.TryGetValue(receiverId, out var receiverConns))
        {
            string[] targets;
            lock (receiverConns) { targets = receiverConns.ToArray(); }
            await Clients.Clients(targets).SendAsync("ReceiveMessage", payload);
        }

        // 3) Echo to sender's tabs
        if (OnlineUsers.TryGetValue(senderId, out var senderConns))
        {
            string[] targets;
            lock (senderConns) { targets = senderConns.ToArray(); }
            await Clients.Clients(targets).SendAsync("ReceiveMessage", payload);
        }
    }


    // Clean up when a connection drops.
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionToUser.TryRemove(Context.ConnectionId, out var userId)
            && OnlineUsers.TryGetValue(userId, out var conns))
        {
            bool nowOffline;
            lock (conns)
            {
                conns.Remove(Context.ConnectionId);
                nowOffline = conns.Count == 0;
            }

            // Only announce "offline" when their LAST connection drops
            if (nowOffline)
                await Clients.All.SendAsync("PresenceChanged", userId, false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public Task<int[]> GetOnlineUsers()
    {
        return Task.FromResult(OnlineUsers
            .Where(kvp => { lock (kvp.Value) return kvp.Value.Count > 0; })
            .Select(kvp => kvp.Key)
            .ToArray());
    }


}
