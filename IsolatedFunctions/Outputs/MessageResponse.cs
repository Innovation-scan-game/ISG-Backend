using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace IsolatedFunctions.Outputs;

/// <summary>
/// This class is used to trigger multiple output bindings at once;
/// A SignalR message as well as a http response to the client.
/// </summary>
public class MessageResponse
{
    [SignalROutput(HubName = "Hub")] public SignalRMessageAction? Message { get; set; }
    public HttpResponseData? UserResponse { get; set; }
}
