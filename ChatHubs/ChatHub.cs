
using Microsoft.AspNetCore.SignalR;

namespace Twibbly.ChatHubs
{
    public class ChatHub : Hub
    {
        private static readonly List<string> AllConnectedUsers = new List<string>();

        private static readonly List<string> ConnectedUsers = new List<string>();
        private static readonly Dictionary<string, string> UserConnections = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> ChatPairs = new Dictionary<string, string>();
        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
            ConnectedUsers.Remove(Context.ConnectionId);
            AllConnectedUsers.Remove(Context.ConnectionId);
            await Clients.All.SendAsync("ConnectedUsers", AllConnectedUsers.Count - 1);
        }

        public async Task JoinChat()
        {
            string username = Context.ConnectionId;

            if (!ConnectedUsers.Contains(username))
            {
                ConnectedUsers.Add(username);
            }

            UserConnections[username] = Context.ConnectionId;

            if (ConnectedUsers.Count >= 2)
            {
                // Get a random pair of connected users
                var pairs = GetRandomPairs(ConnectedUsers);
                string user1 = pairs[0];
                string user2 = pairs[1];

                // Add the pair to the chat pairs dictionary
                ChatPairs[user1] = user2;
                ChatPairs[user2] = user1;

                await Clients.Client(UserConnections[user1]).SendAsync("ReceiveMessage", "You are now connected to a random user.");
                await Clients.Client(UserConnections[user2]).SendAsync("ReceiveMessage", "You are now connected to a random user.");

                await Clients.Client(UserConnections[user1]).SendAsync("IsConnectToUser", true);
                await Clients.Client(UserConnections[user2]).SendAsync("IsConnectToUser", true);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "Waiting for another user to connect...", false);
                await Clients.Caller.SendAsync("IsConnectToUser", false);

            }
            AllConnectedUsers.Add(username);
            await Clients.All.SendAsync("ConnectedUsers", AllConnectedUsers.Count - 1);
        }

        public async Task SendMessage(string message)
        {
            string username = Context.ConnectionId;

            if (ChatPairs.ContainsKey(username))
            {
                string recipient = ChatPairs[username];
                await Clients.Client(UserConnections[recipient]).SendAsync("ReceiveMessage", $"Stranger: {message}");
            }
        }

        public async Task LeaveChat()
        {
            string username = Context.ConnectionId;

            if (ChatPairs.ContainsKey(username))
            {
                string recipient = ChatPairs[username];
                ChatPairs.Remove(username);
                ChatPairs.Remove(recipient);

                await Clients.Client(UserConnections[recipient]).SendAsync("StopChating", "The other user has left the chat.");

                await Clients.Client(UserConnections[recipient]).SendAsync("IsConnectToUser", false);
            }

            ConnectedUsers.Remove(username);
            UserConnections.Remove(username);

            AllConnectedUsers.Remove(username);
            await Clients.All.SendAsync("ConnectedUsers", AllConnectedUsers.Count - 1);
        }

        private List<string> GetRandomPairs(List<string> users)
        {
            var pairs = new List<string>();

            while (users.Count >= 2)
            {
                int index1 = new Random().Next(users.Count);
                string user1 = users[index1];
                users.RemoveAt(index1);

                int index2 = new Random().Next(users.Count);
                string user2 = users[index2];
                users.RemoveAt(index2);

                pairs.Add(user1);
                pairs.Add(user2);
            }

            return pairs;
        }
        public async Task<int> GetWaitingUsersCount()
        {
            return ConnectedUsers.Count - 1; // exclude current user from count
        }
    }
}