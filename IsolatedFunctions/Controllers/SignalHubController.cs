using System.Net;
using System.Security.Claims;
using AutoMapper;
using DAL.Data;
using IsolatedFunctions.DTO.SignalDTOs;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Extensions;
using IsolatedFunctions.Outputs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;

namespace IsolatedFunctions.Controllers;

public class SignalHubController
{
    private readonly InnovationGameDbContext _context;
    private readonly IMapper _mapper;

    public SignalHubController(InnovationGameDbContext context, IMapper mapper)
    {
        _context = context;
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

    [Function(nameof(JoinSignalRGroup))]
    public async Task<MessageAndGroupResponse> JoinSignalRGroup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "joinGrp")]
        HttpRequestData req,
        FunctionContext executionContext,
        [SignalRConnectionInfoInput(HubName = "Hub")]
        SignalRConnectionInfo connectionInfo)
    {
        var principal = executionContext.GetUser();
        if (principal == null)
        {
            return new MessageAndGroupResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        var dbUser = _context.Users.Include(usr => usr.CurrentSession).FirstOrDefault(usr => usr.Name == principal.Identity!.Name);

        if (dbUser?.CurrentSession == null)
        {
            return new MessageAndGroupResponse
                {UserResponse = await req.CreateErrorResponse(HttpStatusCode.BadRequest, "User is not in a session")};
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
