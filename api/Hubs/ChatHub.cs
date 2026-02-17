// Hubs/ChatHub.cs

using api.Config;
using Message;
using Microsoft.AspNetCore.SignalR;

namespace api.Hubs
{
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

        // ── OnConnected ───────────────────────────────────────────────────────

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        // ── Authenticate ──────────────────────────────────────────────────────
        // Client calls this immediately after connecting with their persistent userId

        public async Task Authenticate(string userId, string username)
        {
            var user = new ConnectedUser
            {
                UserId = userId,
                Username = username,
                ConnectionId = Context.ConnectionId
            };

            lock (_lock)
            {
                // Remove stale mapping if user reconnects
                if (_userConnections.TryGetValue(userId, out var oldConnId))
                    _connectionUsers.Remove(oldConnId);

                _userConnections[userId] = Context.ConnectionId;
                _connectionUsers[Context.ConnectionId] = user;
            }

            await Clients.Caller.SendAsync("Authenticated", new
            {
                userId = userId,
                username = username
            });

            await Clients.Others.SendAsync("UserOnline", new
            {
                userId = userId,
                username = username
            });

            var unreadCounts = await _messageDb.GetUnreadCountsAsync(userId);
            await Clients.Caller.SendAsync("UnreadCounts", unreadCounts);

            await UpdateUserCount();
        }

        // ── SendPrivateMessage ────────────────────────────────────────────────

        public async Task SendPrivateMessage(string toUserId, string message)
        {
            ConnectedUser? sender;
            lock (_lock) { _connectionUsers.TryGetValue(Context.ConnectionId, out sender); }
            if (sender == null) return;

            // 1. Save to MongoDB first — regardless of recipient online status
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

            // 2. Echo to sender so their bubble appears immediately
            await Clients.Caller.SendAsync("ReceiveMessage", payload);

            // 3. Deliver to recipient if online now
            string? recipientConnId;
            lock (_lock) { _userConnections.TryGetValue(toUserId, out recipientConnId); }

            if (recipientConnId != null)
                await Clients.Client(recipientConnId).SendAsync("ReceiveMessage", payload);
            // If offline → already in DB, loads via GetConversationHistory on next login
        }

        // ── SendGroupMessage ──────────────────────────────────────────────────

        public async Task SendGroupMessage(string groupId, string message)
        {
            ConnectedUser? sender;
            lock (_lock) { _connectionUsers.TryGetValue(Context.ConnectionId, out sender); }
            if (sender == null) return;

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
            ConnectedUser? caller;
            lock (_lock) { _connectionUsers.TryGetValue(Context.ConnectionId, out caller); }
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

        // ── Typing — targeted, not broadcast ─────────────────────────────────

        public async Task NotifyTyping(string toUserId)
        {
            ConnectedUser? sender;
            lock (_lock) { _connectionUsers.TryGetValue(Context.ConnectionId, out sender); }
            if (sender == null) return;

            string? recipientConnId;
            lock (_lock) { _userConnections.TryGetValue(toUserId, out recipientConnId); }

            if (recipientConnId != null)
                await Clients.Client(recipientConnId).SendAsync("UserTyping", sender.Username);
        }

        public async Task NotifyStopTyping(string toUserId)
        {
            ConnectedUser? sender;
            lock (_lock) { _connectionUsers.TryGetValue(Context.ConnectionId, out sender); }
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

        // ── OnDisconnected ────────────────────────────────────────────────────

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectedUser? user;
            lock (_lock)
            {
                _connectionUsers.TryGetValue(Context.ConnectionId, out user);
                if (user != null)
                {
                    _connectionUsers.Remove(Context.ConnectionId);
                    _userConnections.Remove(user.UserId);
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

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task UpdateUserCount()
        {
            int count;
            lock (_lock) { count = _userConnections.Count; }
            await Clients.All.SendAsync("UpdateUserCount", count);
        }
    }

    public class ConnectedUser
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string ConnectionId { get; set; } = "";
    }
}