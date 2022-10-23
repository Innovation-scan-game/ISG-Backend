using System.Net;
using System.Security.Claims;
using AutoMapper;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.DTO;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.DTO.GameSessionDTOs;
using IsolatedFunctions.DTO.SignalDTOs;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Extensions;
using IsolatedFunctions.Outputs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;

namespace IsolatedFunctions.Controllers;

public class SessionController
{
    private readonly IMapper _mapper;
    private readonly InnovationGameDbContext _context;

    public SessionController(IMapper mapper, InnovationGameDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    [Function("SubmitAnswer")]
    
    [OpenApiOperation(operationId: "PostAnswer", tags: new[] {"session"}, Summary = "Posts a answer",
        Description = "Post a answer in the current session")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionResponseDto),
        Description = "The OK response")]
    public async Task<MessageResponse> SubmitAnswer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/submit")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        if (user == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
            // return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        var dbUser = await _context.Users.Include(u => u.CurrentSession).FirstOrDefaultAsync(u => u.Name == user.Identity!.Name);
        SubmitAnswerDto? dto = await req.ReadFromJsonAsync<SubmitAnswerDto>();

        SessionResponse response = new()
        {
            User = dbUser!,
            Session = dbUser!.CurrentSession!,
            CreatedAt = DateTime.Now,
            CardNumber = dbUser.CurrentSession!.CurrentRound,
            Response = dto!.Answer
        };
        _context.SessionResponses.Add(response);

        await _context.SaveChangesAsync();


        UserDto userDto = _mapper.Map<UserDto>(dbUser);

        var message = new SignalRMessageAction("newAnswer")
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            Arguments = new object[] {userDto, dto.Answer}
        };

        var responseData = req.CreateResponse(HttpStatusCode.OK);
        await responseData.WriteAsJsonAsync(response.Response);

        return new MessageResponse {UserResponse = responseData, Message = message};
    }

    [Function("SendMessage")]
    
    [OpenApiOperation(operationId: "PostMessage", tags: new[] {"session"}, Summary = "Posts a message",
        Description = "Post a message in the current session")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionResponseDto),
        Description = "The OK response")]
    
    public async Task<MessageResponse> SendMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/message")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();

        if (user == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User dbUser = _context.Users.Include(u => u.CurrentSession).FirstOrDefault(u => u.Name == user.Identity!.Name)!;

        UserDto userDto = _mapper.Map<UserDto>(dbUser);

        ChatMessageDto? chatMessage = await req.ReadFromJsonAsync<ChatMessageDto>();

        var message = new SignalRMessageAction("newMessage")
        {
            GroupName = dbUser.CurrentSession!.SessionCode,
            Arguments = new object[] {userDto, dbUser.CurrentSession.CurrentRound, chatMessage!.Message}
        };

        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function("NextRound")]
    
    [OpenApiOperation(operationId: "PostRound", tags: new[] {"session"}, Summary = "Stars a new round",
        Description = "Stars a new round in the current session")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(GameStateDto),
        Description = "The OK response")]
    public async Task<MessageResponse> NextRound(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/next")]
        HttpRequestData req, FunctionContext executionContext)

    {
        ClaimsPrincipal? user = executionContext.GetUser();

        if (user == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User dbUser = _context.Users.Include(usr => usr.CurrentSession!.Cards).FirstOrDefault(u => u.Name == user.Identity!.Name)!;

        // Check if executing user is session host or not
        if (dbUser.CurrentSession!.HostId != dbUser.Id)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Forbidden)};
        }

        if (dbUser.CurrentSession.CurrentRound < dbUser.CurrentSession.Rounds)
        {
            dbUser.CurrentSession.CurrentRound += 1;
        }

        await _context.SaveChangesAsync();
        var message = new SignalRMessageAction("nextRound")
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            Arguments = new object[] {dbUser.CurrentSession.CurrentRound}
        };

        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function("EndSession")]
    [OpenApiOperation(operationId: "PostSession", tags: new[] {"session"}, Summary = "End Session",
        Description = "End the current session")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(GameStateDto),
        Description = "The OK response")]
    
    public async Task<MessageResponse> EndSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/end")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();

        if (user == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User dbUser = _context.Users.Include(usr => usr.CurrentSession!.Cards).FirstOrDefault(u => u.Name == user.Identity!.Name)!;

        // Check if executing user is session host or not
        if (dbUser.CurrentSession!.HostId != dbUser.Id)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Forbidden)};
        }

        dbUser.CurrentSession.Status = SessionStatus.Completed;
        await _context.SaveChangesAsync();


        SignalRMessageAction message = new("endSession")
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            Arguments = new object[] {"end"}
        };

        // Remove all users from session
        await _context.Users.Include(usr => usr.CurrentSession).Where(u => u.CurrentSession!.Id == dbUser.CurrentSession.Id)
            .ForEachAsync(usr =>
            {
                usr.CurrentSession = null;
                usr.Ready = false;
            });

        await _context.SaveChangesAsync();


        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function("JoinSession")]
    [OpenApiOperation(operationId: "GetSession", tags: new[] {"session"}, Summary = "Joins a game",
        Description = "Joins a session that has not started yet")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(JoinRequestDto),
        Description = "The OK response")]
    public async Task<HttpResponseData> JoinSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/join")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();

        var response = req.CreateResponse();

        if (user == null)
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(new ErrorDto {Message = "Unauthorized"});
            return response;
        }

        JoinRequestDto? joinRequestDto = await req.ReadFromJsonAsync<JoinRequestDto>();
        if (joinRequestDto == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(new ErrorDto {Message = "Bad request"});
            return response;
        }

        GameSession? session =
            await _context.GameSessions.Include(s => s.Players)
                .FirstOrDefaultAsync(session => session.SessionCode == joinRequestDto.SessionAuth && session.Status == SessionStatus.Lobby);

        if (session == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Session not found");
        }

        if (session.Status != SessionStatus.Lobby)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Session is no longer valid.");
        }

        User? dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Name == user.Identity!.Name);
        dbUser!.CurrentSession = session;
        dbUser.Ready = false;
        await _context.SaveChangesAsync();
        LobbyResponseDto sessionDto = _mapper.Map<LobbyResponseDto>(session);
        return await req.CreateSuccessResponse(sessionDto);
    }

    [Function("LeaveSession")]
    [OpenApiOperation(operationId: "PostSession", tags: new[] {"session"}, Summary = "Leaves a game",
        Description = "Leaves a session ")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(JoinRequestDto),
        Description = "The OK response")]
    
    public async Task<MessageResponse> LeaveSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/leave")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        if (user == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User? dbUser = await _context.Users.Include(usr => usr.CurrentSession).FirstOrDefaultAsync(u => u.Name == user.Identity!.Name);
        var message = new SignalRMessageAction("playerLeft")
        {
            GroupName = dbUser!.CurrentSession!.SessionCode,
            Arguments = new object[] {dbUser}
        };

        if (dbUser.CurrentSession.HostId == dbUser.Id)
        {
            _context.GameSessions.Remove(dbUser.CurrentSession);
            dbUser.CurrentSession = null;
        }

        dbUser.CurrentSession = null;
        dbUser.Ready = false;
        await _context.SaveChangesAsync();

        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function("CreateSession")]
    [OpenApiOperation(operationId: "GetSession", tags: new[] {"session"}, Summary = "Creates a session",
        Description = "Stars a session that has not created yet")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LobbyResponseDto),
        Description = "The OK response")]
    public async Task<HttpResponseData> CreateSession([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        if (user == null)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        User? dbUser = await _context.Users.Include(usr => usr.CurrentSession).FirstOrDefaultAsync(u => u.Name == user.Identity!.Name);

        GameSession session = GameSession.New();
        session.HostId = dbUser!.Id;
        _context.GameSessions.Add(session);

        dbUser.CurrentSession = session;
        dbUser.Ready = false;

        await _context.SaveChangesAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        LobbyResponseDto sessionDto = _mapper.Map<LobbyResponseDto>(session);
        await response.WriteAsJsonAsync(sessionDto);
        return response;
    }

    [Function("StartSession")]
    [OpenApiOperation(operationId: "PostSession", tags: new[] {"session"}, Summary = "Starts a game",
        Description = "Starts a session that has not started yet")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StartGameDto),
        Description = "The OK response")]
    public async Task<MessageResponse> StartSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/start")]
        HttpRequestData req, FunctionContext executionContext)
    {
        SessionOptionsDto? options = await req.ReadFromJsonAsync<SessionOptionsDto>();

        Random rnd = new Random();
        List<Card> dbCards = _context.Cards.ToList().OrderBy(_ => rnd.Next()).Take(options!.Rounds).OrderBy(c => c.Id).ToList();

        List<CardDto> cards = _mapper.Map<List<CardDto>>(dbCards);

        var principal = executionContext.GetUser();
        if (principal == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User? user = await _context.Users.Include(u => u.CurrentSession).FirstOrDefaultAsync(u => u.Name == principal.Identity!.Name);


        user!.CurrentSession!.Rounds = options.Rounds;
        user.CurrentSession.Status = SessionStatus.Active;
        user.CurrentSession.Cards = dbCards;
        user.CurrentSession.RoundDurationSeconds = options.RoundDuration;
        await _context.SaveChangesAsync();


        StartGameDto responseDto = new()
        {
            Cards = cards,
            RoundDuration = options.RoundDuration,
        };


        var message = new SignalRMessageAction("startGame")
        {
            GroupName = user.CurrentSession.SessionCode,
            Arguments = new object[] {responseDto}
        };


        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function("ChangeReadyState")]
    [OpenApiOperation(operationId: "PostState", tags: new[] {"session"}, Summary = "Changes Ready state",
        Description = "Allows users to change whether they are ready or not")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RequestGameStateDto),
        Description = "The OK response")]
    
    [SignalROutput(HubName = "Hub")]
    public async Task<SignalRMessageAction> ChangeReadyState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/ready")]
        HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "Hub")]
        SignalRConnectionInfo connectionInfo,
        FunctionContext executionContext)
    {
        ClaimsPrincipal? principal = executionContext.GetUser();
        User? user = await _context.Users.Include(usr => usr.CurrentSession)
            .FirstOrDefaultAsync(usr => usr.Name == principal!.Identity!.Name);

        ChangeReadinessDto? readiness = await req.ReadFromJsonAsync<ChangeReadinessDto>();

        user!.Ready = readiness!.Ready;
        LobbyPlayerDto playerDto = _mapper.Map<LobbyPlayerDto>(user);

        await _context.SaveChangesAsync();

        return new SignalRMessageAction("readyStateChanged")
        {
            GroupName = user.CurrentSession!.SessionCode,
            Arguments = new object[] {playerDto}
        };
    }

    [Function(nameof(MatchHistory))]
    [OpenApiOperation(operationId: "GetHistory", tags: new[] {"session"}, Summary = "Show Match History",
        Description = "Shows the match history of a session")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(HistoryDto),
        Description = "The OK response")]
    public async Task<HttpResponseData> MatchHistory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/history")] HttpRequestData req, FunctionContext executionContext)
    {
        var sessions = _context.GameSessions.Include(s => s.Players).Include(s => s.Cards).Include(s => s.Responses)
            .ThenInclude(r => r.User).ToList();

        SessionDto[]? dtos = _mapper.Map<SessionDto[]>(sessions);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dtos);
        return response;
    }
}
