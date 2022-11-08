using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace IsolatedFunctions.Outputs;

public class MessageAndGroupResponse
{

    [SignalROutput(HubName = "Hub")] public SignalRGroupAction? GroupAction { get; set; }
    [SignalROutput(HubName = "Hub")] public SignalRMessageAction? MessageAction { get; set; }
    public HttpResponseData? UserResponse { get; set; }
}
