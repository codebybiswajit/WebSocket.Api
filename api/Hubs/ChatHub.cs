// Hubs/ChatHub.cs

using api.Config;
using Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly MessageDb _messageDb;

        // userId (permanent) → connectionId (current session only)
        private static readonly Dictionary<string, string> _userConnections = new();
        // connectionId → user info (for disconnect lookup)
        private static readonly Dictionary<string, ConnectedUser> _connectionUsers = new();
        private static readonly object _lock = new();

        public ChatHub(DbManager messageDb)
        {
            _messageDb = messageDb.MessageDb;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private ConnectedUser? GetSender()
        {
            // Try in-memory lookup first, fall back to JWT claims
            lock (_lock)
            {
                if (_connectionUsers.TryGetValue(Context.ConnectionId, out var user))
                    return user;
            }

            var userId = Context.User?.FindFirst("UserId")?.Value;
            var username = Context.User?.FindFirst("Username")?.Value;

            if (string.IsNullOrEmpty(userId)) return null;

            return new ConnectedUser
            {
                UserId = userId,
                Username = username ?? "",
                ConnectionId = Context.ConnectionId
            };
        }

        private void RegisterUser(string userId, string username)
        {
            var user = new ConnectedUser
            {
                UserId = userId,
                Username = username,
                ConnectionId = Context.ConnectionId
            };

            lock (_lock)
            {
                if (_userConnections.TryGetValue(userId, out var oldConnId))
                    _connectionUsers.Remove(oldConnId);

                _userConnections[userId] = Context.ConnectionId;
                _connectionUsers[Context.ConnectionId] = user;
            }
        }

        private async Task UpdateUserCount()
        {
            int count;
            lock (_lock) { count = _userConnections.Count; }
            await Clients.All.SendAsync("UpdateUserCount", count);
        }

        // ── OnConnectedAsync ──────────────────────────────────────────────────
        // Reads identity from JWT claims — no separate Authenticate call needed

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("UserId")?.Value;
            var username = Context.User?.FindFirst("Username")?.Value;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(username))
            {
                RegisterUser(userId, username);

                var unreadCounts = await _messageDb.GetUnreadCountsAsync(userId);
                await Clients.Caller.SendAsync("UnreadCounts", unreadCounts);
                await Clients.Others.SendAsync("UserOnline", new { userId, username });
                await UpdateUserCount();
            }

            // Tell the client who they are
            await Clients.Caller.SendAsync("ConnectedAs", new { userId, username });

            await base.OnConnectedAsync();
        }

        // ── OnDisconnectedAsync ───────────────────────────────────────────────

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectedUser? user;
            lock (_lock)
            {
                _connectionUsers.TryGetValue(Context.ConnectionId, out user);
                if (user != null)
                {
                    _connectionUsers.Remove(Context.ConnectionId);
                    
                    // Only remove from _userConnections if this disconnected connection is still the active one
                    if (_userConnections.TryGetValue(user.UserId, out var activeConnId) && activeConnId == Context.ConnectionId)
                    {
                        _userConnections.Remove(user.UserId);
                    }
                }
            }

            if (user != null)
            {
                await Clients.Others.SendAsync("UserOffline", new
                {
                    userId = user.UserId,
                    username = user.Username
                });
            }

            await UpdateUserCount();
            await base.OnDisconnectedAsync(exception);
        }

        // ── SendPrivateMessage ────────────────────────────────────────────────

        public async Task SendPrivateMessage(string toUserId, string message)
        {
            var sender = GetSender();
            if (sender == null || string.IsNullOrEmpty(sender.UserId)) return;

            var saved = await _messageDb.SaveMessageAsync(
                fromUserId: sender.UserId,
                fromUsername: sender.Username,
                toId: toUserId,
                content: message,
                chatType: ChatType.Pair
            );

            if (saved == null) return;

            var payload = new
            {
                id = saved.Id,
                fromUserId = sender.UserId,
                fromUsername = sender.Username,
                toUserId = toUserId,
                message = message,
                timestamp = DateTime.UtcNow,
                chatType = "Pair"
            };

            // Echo to sender
            await Clients.Caller.SendAsync("ReceiveMessage", payload);

            // Deliver to recipient if online
            string? recipientConnId;
            lock (_lock) { _userConnections.TryGetValue(toUserId, out recipientConnId); }

            if (recipientConnId != null)
                await Clients.Client(recipientConnId).SendAsync("ReceiveMessage", payload);
        }

        // ── SendGroupMessage ──────────────────────────────────────────────────

        public async Task SendGroupMessage(string groupId, string message)
        {
            var sender = GetSender();
            if (sender == null || string.IsNullOrEmpty(sender.UserId)) return;

            var saved = await _messageDb.SaveMessageAsync(
                fromUserId: sender.UserId,
                fromUsername: sender.Username,
                toId: groupId,
                content: message,
                chatType: ChatType.Group
            );

            if (saved == null) return;

            var payload = new
            {
                id = saved.Id,
                fromUserId = sender.UserId,
                fromUsername = sender.Username,
                groupId = groupId,
                message = message,
                timestamp = DateTime.UtcNow,
                chatType = "Group"
            };

            await Clients.All.SendAsync("ReceiveGroupMessage", payload);
        }

        // ── GetConversationHistory ────────────────────────────────────────────

        public async Task GetConversationHistory(string withUserId, int page = 0)
        {
            var caller = GetSender();
            if (caller == null) return;

            var history = await _messageDb.GetConversationAsync(caller.UserId, withUserId, page);

            await Clients.Caller.SendAsync("ConversationHistory", new
            {
                withUserId = withUserId,
                messages = history.Select(m => new
                {
                    id = m.Id,
                    fromUserId = m.MessageFrom,
                    fromUsername = m.CreatedBy.Name,
                    message = m.Message,
                    timestamp = m.CreatedBy.Date,
                }),
                page = page
            });
        }

        // ── GetGroupHistory ───────────────────────────────────────────────────

        public async Task GetGroupHistory(string groupId, int page = 0)
        {
            var history = await _messageDb.GetGroupMessagesAsync(groupId, page);

            await Clients.Caller.SendAsync("GroupHistory", new
            {
                groupId = groupId,
                messages = history.Select(m => new
                {
                    id = m.Id,
                    fromUserId = m.MessageFrom,
                    fromUsername = m.CreatedBy.Name,
                    message = m.Message,
                    timestamp = m.CreatedBy.Date,
                }),
                page = page
            });
        }

        // ── Typing ────────────────────────────────────────────────────────────

        public async Task NotifyTyping(string toUserId)
        {
            var sender = GetSender();
            if (sender == null) return;

            string? recipientConnId;
            lock (_lock) { _userConnections.TryGetValue(toUserId, out recipientConnId); }

            if (recipientConnId != null)
                await Clients.Client(recipientConnId).SendAsync("UserTyping", sender.Username);
        }

        public async Task NotifyStopTyping(string toUserId)
        {
            var sender = GetSender();
            if (sender == null) return;

            string? recipientConnId;
            lock (_lock) { _userConnections.TryGetValue(toUserId, out recipientConnId); }

            if (recipientConnId != null)
                await Clients.Client(recipientConnId).SendAsync("UserStopTyping", sender.Username);
        }

        // ── GetOnlineUsers ────────────────────────────────────────────────────

        public async Task GetOnlineUsers()
        {
            List<object> users;
            lock (_lock)
            {
                users = _connectionUsers.Values
                    .Select(u => (object)new { userId = u.UserId, username = u.Username })
                    .ToList();
            }
            await Clients.Caller.SendAsync("OnlineUsers", users);
        }
    }

    public class ConnectedUser
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string ConnectionId { get; set; } = "";
    }
}