using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace FunctionsApp.Functions;

/// <summary>
/// This is sufficient for a simple chat demo.
/// What is missing is user auth and a way to send messages only to users who are in the same session.
/// SignalR Groups seem to be a good fit for this.
/// </summary>
public class SignalRTest
{
    [FunctionName("negotiate")]
    public static async Task<SignalRConnectionInfo> Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
        HttpRequest req, ILogger log,
        [SignalRConnectionInfo(HubName = "GameSignal", ConnectionStringSetting = "AzureSignalRConnectionString")]
        SignalRConnectionInfo connectionInfo)
    {
        return connectionInfo;
    }

    [FunctionName("sendMessage")]
    public static Task SendMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
        object message, [SignalR(HubName = "GameSignal")] IAsyncCollector<SignalRMessage> signalRMessages)
    {
        return signalRMessages.AddAsync(
            new SignalRMessage
            {
                // GroupName = "grp",
                Target = "NewMessage",
                Arguments = new[] {message}
            });
    }

    [FunctionName("changeReadyState")]
    public static Task ChangeReadyState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
        object message, [SignalR(HubName = "GameSignal")] IAsyncCollector<SignalRMessage> signalRMessages)
    {
        return signalRMessages.AddAsync(
            new SignalRMessage
            {
                Target = "ReadyStateChange",
                Arguments = new[] {message}
            });
    }


    [FunctionName("joinGroup")]
    public static Task JoinGroup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
        object message, [SignalR(HubName = "GameSignal")] IAsyncCollector<SignalRGroupAction> signalRGroupActions)
    {
        return signalRGroupActions.AddAsync(
            new SignalRGroupAction
            {
                UserId = "user1",
                GroupName = "grp",
                Action = GroupAction.Add
            }
        );
    }
}
