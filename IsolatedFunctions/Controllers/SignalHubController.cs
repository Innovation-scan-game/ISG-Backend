using System.Net;
using AutoMapper;
using DAL.Data;
using IsolatedFunctions.DTO.SignalDTOs;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Extensions;
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
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(connectionInfo);

        return response;
    }

    [Function("joinGrp")]
    public async Task<MessageAndGroup> JoinGrp([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        FunctionContext executionContext,
        [SignalRConnectionInfoInput(HubName = "Hub")]
        SignalRConnectionInfo connectionInfo)
    {
        var principal = executionContext.GetUser();
        var dbUser = _context.Users.Include(usr => usr.CurrentSession).FirstOrDefault(usr => usr.Name == principal!.Identity!.Name);

        JoinGroupDto? test = await req.ReadFromJsonAsync<JoinGroupDto>();
        // var body = new StreamReader(req.Body).ReadToEnd();
        // Console.WriteLine("JOIN GRP");

        LobbyPlayerDto userDto = _mapper.Map<LobbyPlayerDto>(dbUser);

        SignalRMessageAction msg = new("newPlayer")
        {
            GroupName = dbUser!.CurrentSession!.SessionCode,
            Arguments = new object[] {userDto}
        };

        SignalRGroupAction grp = new(SignalRGroupActionType.Add)
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            ConnectionId = test!.ConnectionId
        };

        return new MessageAndGroup {MessageAction = msg, GroupAction = grp, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }


    // update readiness

    // answer submitted?

    [Function("onconnected")]
    public MessageAndGroup OnConnected(
        [SignalRTrigger(hubName: "Hub", category: "connections", @event: "connected")]
        SignalRInvocationContext context)
    {
        // var t = context.Headers;
        // string token = context.Query["access_token"].ToString();

        // var headers = context.Headers;

        // Console.WriteLine("ONCON");
        // ClaimsPrincipal user = await TokenService.GetByValue(token);


        // var test = _context.Users.ToList();
        var message = new SignalRMessageAction("newConnection")
        {
            ConnectionId = context.ConnectionId,
            Arguments = new object[] {context.ConnectionId}
        };
        // var grp = new SignalRGroupAction(SignalRGroupActionType.Add)
        // {
        //     GroupName = "test",
        //     ConnectionId = context.ConnectionId
        // };
        return new MessageAndGroup {MessageAction = message};
    }


    public class MessageAndGroup
    {
        [SignalROutput(HubName = "Hub")] public SignalRGroupAction? GroupAction { get; set; }
        [SignalROutput(HubName = "Hub")] public SignalRMessageAction? MessageAction { get; set; }
        public HttpResponseData? UserResponse { get; set; }
    }


    [Function("SendToGroup")]
    [SignalROutput(HubName = "Hub")]
    public SignalRMessageAction SendToGroup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        return new SignalRMessageAction("newMessage")
        {
            GroupName = "test",
            Arguments = new object[] {"message to grp"}
        };
    }
}
