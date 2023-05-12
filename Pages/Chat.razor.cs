using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;

namespace Twibbly.Pages
{
    public partial class Chat : IAsyncDisposable
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ISnackbar snackbar { get; set; }
        [Inject] IJSRuntime jSRuntime { get; set; }
        private bool isConnected = false;
        private bool isConnecting = false;
        private string message = "";
        private int TotalOnlineUsers = 0;
        private HubConnection hubConnection;

        MudTextField<string> MudTextField;
        private async Task StartChat()
        {
            isConnecting = true;
            StateHasChanged();

            hubConnection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/chatHub"))
                .Build();

            hubConnection.On<string>("ReceiveMessage", (receivedMessage) =>
            {
                messageHistory.Add(receivedMessage);
                Task.Delay(TimeSpan.FromSeconds(5));
                InvokeAsync(StateHasChanged);
                jSRuntime.InvokeAsync<object>("scrollToBottom");
            });

            hubConnection.On<int>("ConnectedUsers", (totalOnlineUsers) =>
            {
                TotalOnlineUsers = totalOnlineUsers;
                InvokeAsync(StateHasChanged);
            });


            hubConnection.On<bool>("IsConnectToUser", (isConnected) =>
            {
                this.isConnected = isConnected;
                isConnecting = !this.isConnected;
            });

            hubConnection.On<string>("StopChating", (message) =>
            {
                snackbar.Add(message, Severity.Info);
            });

            await hubConnection.StartAsync();

            await hubConnection.SendAsync("JoinChat");

            StateHasChanged();
        }

        private async Task SendMessage()
        {
            if (!string.IsNullOrEmpty(message))
            {
                await hubConnection.SendAsync("SendMessage", message);
                messageHistory.Add("Me: " + message);
                message = "";
                await InvokeAsync(StateHasChanged);
                await MudTextField.Clear();
                await Task.Delay(TimeSpan.FromSeconds(1));
                await jSRuntime.InvokeAsync<object>("scrollToBottom");

            }
        }
        private async Task KeyPressEvent(KeyboardEventArgs keyboardEventArgs)
        {
            if (keyboardEventArgs.Code == "Enter")
            {
                await SendMessage();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task EndChat()
        {
            await hubConnection.SendAsync("LeaveChat");
            messageHistory.Clear();
            isConnected = false;
            StateHasChanged();
        }


        public async ValueTask DisposeAsync()
        {
            if (hubConnection is not null)
            {
                await hubConnection.SendAsync("LeaveChat");
                await hubConnection.DisposeAsync();
            }
        }

        private List<string> messageHistory = new List<string>();
    }
}
