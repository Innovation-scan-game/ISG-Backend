using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.Models;
using IsolatedFunctions.DTO.SignalDTOs;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Extensions;
using IsolatedFunctions.Outputs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Services.Interfaces;

namespace IsolatedFunctions.Controllers;

public class SignalHubController
{
    private readonly IMapper _mapper;

    private IUserService UserService { get; }

    public SignalHubController(IMapper mapper, IUserService userService)
    {
        UserService = userService;
        _mapper = mapper;
    }

    [Function("negotiate")]
    public static async Task<HttpResponseData> Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData req,
        FunctionContext executionContext,
        [SignalRConnectionInfoInput(HubName = "Hub")]
        SignalRConnectionInfo connectionInfo, SignalRInvocationContext context)
    {
        ClaimsPrincipal? principal = executionContext.GetUser();
        // TODO: Validate user. Either here or in the middleware.

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(connectionInfo);

        return response;
    }

    [Function(nameof(JoinGroup))]
    public async Task<MessageAndGroupResponse> JoinGroup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "joinGrp")]
        HttpRequestData req,
        FunctionContext executionContext,
        [SignalRConnectionInfoInput(HubName = "Hub")]
        SignalRConnectionInfo connectionInfo)
    {
        ClaimsPrincipal? principal = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(principal?.Identity?.Name!);
        if (dbUser is null)
        {
            return new MessageAndGroupResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }


        if (dbUser.CurrentSession == null)
        {
            return new MessageAndGroupResponse
                {UserResponse = req.CreateResponse(HttpStatusCode.OK)};
        }

        JoinGroupDto? joinGroupDto = await req.ReadFromJsonAsync<JoinGroupDto>();

        LobbyPlayerDto userDto = _mapper.Map<LobbyPlayerDto>(dbUser);

        SignalRMessageAction msg = new("newPlayer")
        {
            GroupName = dbUser.CurrentSession!.SessionCode,
            Arguments = new object[] {userDto}
        };

        SignalRGroupAction grp = new(SignalRGroupActionType.Add)
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            ConnectionId = joinGroupDto!.ConnectionId
        };

        return new MessageAndGroupResponse {MessageAction = msg, GroupAction = grp, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }


    /// <summary>
    /// Remove disconnected players from the group and inform the other players.
    /// </summary>
    [Function(nameof(OnDisconnected))]
    public MessageAndGroupResponse OnDisconnected(
        [SignalRTrigger(hubName: "Hub", category: "connections", @event: "disconnected")]
        SignalRInvocationContext context)
    {
        var groupAction = new SignalRGroupAction(SignalRGroupActionType.RemoveAll)
        {
            ConnectionId = context.ConnectionId,
        };

        var messageAction = new SignalRMessageAction("playerLeft")
        {
            ConnectionId = context.ConnectionId,
            Arguments = new object[] {context.ConnectionId}
        };

        return new MessageAndGroupResponse {GroupAction = groupAction, MessageAction = messageAction};
    }


    /// <summary>
    /// Send the client connection id to the client when the connection is first established.
    /// </summary>
    [SignalROutput(HubName = "Hub")]
    [Function(nameof(OnConnected))]
    public SignalRMessageAction OnConnected(
        [SignalRTrigger(hubName: "Hub", category: "connections", @event: "connected")]
        SignalRInvocationContext context)
    {
        return new SignalRMessageAction("newConnection")
        {
            ConnectionId = context.ConnectionId,
            Arguments = new object[] {context.ConnectionId}
        };
    }
}
