using Microsoft.AspNetCore.SignalR;
namespace api.Hubs
{

    /// <summary>
    /// SignalR Hub for real-time chat functionality
    /// </summary>
    public class ChatHub : Hub
    {
        // Store connected users (in production, use a database or distributed cache)
        private static readonly Dictionary<string, string> _connectedUsers = new();
        private static readonly object _lockObject = new();

        /// <summary>
        /// Send a chat message to all connected clients
        /// </summary>
        public async Task SendMessage(string username, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", new
            {
                username = username,
                message = message,
                timestamp = DateTime.UtcNow,
                connectionId = Context.ConnectionId
            });
        }

        /// <summary>
        /// Set the username for the current connection
        /// </summary>
        public async Task SetUsername(string username)
        {
            lock (_lockObject)
            {
                _connectedUsers[Context.ConnectionId] = username;
            }

            await Clients.Caller.SendAsync("UsernameSet", username);
            await Clients.Others.SendAsync("UserJoined", username);
            await UpdateUserCount();
        }

        /// <summary>
        /// Notify other users that this user is typing
        /// </summary>
        public async Task NotifyTyping(string username)
        {
            await Clients.Others.SendAsync("UserTyping", username);
        }

        /// <summary>
        /// Notify other users that this user stopped typing
        /// </summary>
        public async Task NotifyStopTyping(string username)
        {
            await Clients.Others.SendAsync("UserStopTyping", username);
        }

        /// <summary>
        /// Send a private message to a specific user
        /// </summary>
        public async Task SendPrivateMessage(string toConnectionId, string message)
        {
            string? fromUsername;
            lock (_lockObject)
            {
                _connectedUsers.TryGetValue(Context.ConnectionId, out fromUsername);
            }

            await Clients.Client(toConnectionId).SendAsync("ReceivePrivateMessage", new
            {
                from = fromUsername ?? "Anonymous",
                message = message,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get list of all connected users
        /// </summary>
        public async Task GetConnectedUsers()
        {
            List<string> users;
            lock (_lockObject)
            {
                users = _connectedUsers.Values.ToList();
            }

            await Clients.Caller.SendAsync("ConnectedUsers", users);
        }

        /// <summary>
        /// Called when a new connection is established
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
            await UpdateUserCount();
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a connection is terminated
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string? username = null;

            lock (_lockObject)
            {
                if (_connectedUsers.TryGetValue(Context.ConnectionId, out username))
                {
                    _connectedUsers.Remove(Context.ConnectionId);
                }
            }

            if (!string.IsNullOrEmpty(username))
            {
                await Clients.Others.SendAsync("UserLeft", username);
            }

            await UpdateUserCount();
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Broadcast the current number of connected users
        /// </summary>
        private async Task UpdateUserCount()
        {
            int count;
            lock (_lockObject)
            {
                count = _connectedUsers.Count;
            }

            await Clients.All.SendAsync("UpdateUserCount", count);
        }
    }
}
