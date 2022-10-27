using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace FunctionsApp;

public class GameHub : Hub
{
    public async Task NewCallReceived(string message)
    {
        await Clients.All.SendAsync("NewCallReceived", message);
    }

    public Task JoinGroup(string groupName)
    {
        Console.WriteLine($"Joining group {groupName}");
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}
