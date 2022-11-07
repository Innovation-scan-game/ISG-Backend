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
using IsolatedFunctions.Services;
using IsolatedFunctions.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;

namespace IsolatedFunctions.Controllers;

public class SessionController
{
    private readonly IMapper _mapper;
    // private readonly InnovationGameDbContext _context;

    private IUserService UserService { get; }
    private ISessionService SessionService { get; }
    private ICardService CardService { get; }

    private ISessionResponseService SessionResponseService { get; }


    public SessionController(IMapper mapper, IUserService userService, ISessionService sessionService, ICardService cardService,
        ISessionResponseService sessionResponseService)
    {
        UserService = userService;
        SessionService = sessionService;
        CardService = cardService;
        SessionResponseService = sessionResponseService;

        _mapper = mapper;
        // _context = context;
    }

    [Function("SubmitAnswer")]
    [OpenApiOperation(operationId: "SubmitAnswer", tags: new[] {"session"}, Summary = "Submit an answer",
        Description = "Submit an answer in the current session")]
    [OpenApiRequestBody("application/json", typeof(SubmitAnswerDto), Description = "Answer to submit")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionResponseDto),
        Description = "The submitted answer")]
    public async Task<MessageResponse> SubmitAnswer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/submit")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        if (user == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User? dbUser = await UserService.GetUserByName(user.Identity!.Name!);
        // var dbUser = await _context.Users.Include(u => u.CurrentSession!.Responses).FirstOrDefaultAsync(u => u.Name == user.Identity!.Name);
        SubmitAnswerDto? dto = await req.ReadFromJsonAsync<SubmitAnswerDto>();

        // TODO: Check if user already submitted an answer for this round


        if (dto == null)
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.BadRequest)};
        }

        SessionResponse response = new()
        {
            User = dbUser!,
            Session = dbUser!.CurrentSession!,
            CreatedAt = DateTime.Now,
            CardNumber = dbUser.CurrentSession!.CurrentRound,
            Response = dto.Answer,
            ResponseType = SessionResponseType.Answer
        };

        await SessionResponseService.AddSessionResponse(response);
        // _context.SessionResponses.Add(response);
        //
        // await _context.SaveChangesAsync();


        UserDto userDto = _mapper.Map<UserDto>(dbUser);

        var message = new SignalRMessageAction("newAnswer")
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            Arguments = new object[] {userDto, dto.Answer}
        };

        var responseData = req.CreateResponse(HttpStatusCode.OK);
        // await responseData.WriteAsJsonAsync(response.Response);

        return new MessageResponse {UserResponse = responseData, Message = message};
    }


    [Function(nameof(SendMessage))]
    [OpenApiOperation(operationId: "SendMessage", tags: new[] {"session"}, Summary = "Sends a chat message",
        Description = "Send a message to all users in the current session")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "The message was sent.")]
    public async Task<MessageResponse> SendMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/message")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();

        if (user == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User? dbUser = await UserService.GetUserByName(user.Identity!.Name!);

        UserDto userDto = _mapper.Map<UserDto>(dbUser);

        ChatMessageDto? chatMessage = await req.ReadFromJsonAsync<ChatMessageDto>();

        var message = new SignalRMessageAction("newMessage")
        {
            GroupName = dbUser.CurrentSession!.SessionCode,
            Arguments = new object[] {userDto, dbUser.CurrentSession.CurrentRound, chatMessage!.Message}
        };

        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function(nameof(NextRound))]
    [OpenApiOperation(operationId: "NextRound", tags: new[] {"session"}, Summary = "Stars a new round",
        Description = "Advances the game to the next round. Optionally takes a conclusion for the current round.")]
    [OpenApiRequestBody("application/json", typeof(ConclusionDto), Required = false)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Next round started")]
    public async Task<MessageResponse> NextRound(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/next")]
        HttpRequestData req, FunctionContext executionContext)

    {
        ClaimsPrincipal? user = executionContext.GetUser();

        if (user == null)
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.Unauthorized)};
        }

        User? dbUser = await UserService.GetUserByName(user.Identity!.Name!);


        // Check if executing user is session host or not
        if (dbUser.CurrentSession!.HostId != dbUser.Id)
        {
            return new MessageResponse
                {UserResponse = await req.CreateErrorResponse(HttpStatusCode.Forbidden, "You are not the lobby host.")};
        }

        ConclusionDto? dto = await req.ReadFromJsonAsync<ConclusionDto>();
        if (dto != null && !string.IsNullOrEmpty(dto.Conclusion))
        {
            await SessionResponseService.AddSessionResponse(new SessionResponse
            {
                User = dbUser,
                Session = dbUser.CurrentSession,
                CreatedAt = DateTime.Now,
                CardNumber = dbUser.CurrentSession.CurrentRound,
                Response = dto.Conclusion,
                ResponseType = SessionResponseType.Conclusion
            });
        }


        if (dbUser.CurrentSession.CurrentRound < dbUser.CurrentSession.Rounds)
        {
            dbUser.CurrentSession.CurrentRound += 1;
        }
        else
        {
            return new MessageResponse
                {UserResponse = await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Game is already finished.")};
        }

        await UserService.UpdateUser(dbUser);
        // _context.Users.Update(dbUser);
        // await _context.SaveChangesAsync();


        var message = new SignalRMessageAction("nextRound")
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            Arguments = new object[] {dbUser.CurrentSession.CurrentRound}
        };

        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function(nameof(EndSession))]
    [OpenApiOperation(operationId: "EndSession", tags: new[] {"session"}, Summary = "End Session",
        Description = "End the session currently in progress")]
    [OpenApiRequestBody("application/json", typeof(ConclusionDto), Required = false)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Session ended")]
    public async Task<MessageResponse> EndSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/end")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();

        if (user == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User? dbUser = await UserService.GetUserByName(user.Identity!.Name!);

        // Check if executing user is session host or not
        bool isSessionHost = dbUser.CurrentSession!.HostId == dbUser.Id;
        if (!isSessionHost)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Forbidden)};
        }

        dbUser.CurrentSession.Status = SessionStatus.Completed;
        await UserService.UpdateUser(dbUser);

        ConclusionDto? dto = await req.ReadFromJsonAsync<ConclusionDto>();
        if (dto != null && !string.IsNullOrEmpty(dto.Conclusion))
        {
            Console.WriteLine("concl", dto.Conclusion);
            await SessionResponseService.AddSessionResponse(new SessionResponse
            {
                User = dbUser,
                Session = dbUser.CurrentSession,
                CreatedAt = DateTime.Now,
                CardNumber = dbUser.CurrentSession.CurrentRound,
                Response = dto.Conclusion,
                ResponseType = SessionResponseType.Conclusion
            });
        }

        SignalRMessageAction message = new("endSession")
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            Arguments = new object[] {"end"}
        };

        // Remove all users from session
        await UserService.GetUsersInSession(dbUser.CurrentSession.Id)
            // await _context.Users.Include(usr => usr.CurrentSession).Where(u => u.CurrentSession!.Id == dbUser.CurrentSession.Id)
            .ForEachAsync(usr =>
            {
                usr.CurrentSession = null;
                usr.Ready = false;

                UserService.UpdateUser(usr);
            });

        // await _context.SaveChangesAsync();


        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function(nameof(JoinSession))]
    [OpenApiOperation(operationId: "GetSession", tags: new[] {"session"}, Summary = "Joins a game",
        Description = "Joins a session that has not started yet")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(JoinRequestDto),
        Description = "joins the requested session")]
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

        GameSession? session = await SessionService.GetSessionByJoinCode(joinRequestDto.SessionAuth);

        // await _context.GameSessions.Include(s => s.Players)
        //     .FirstOrDefaultAsync(session => session.SessionCode == joinRequestDto.SessionAuth && session.Status == SessionStatus.Lobby);

        if (session == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Session not found");
        }

        if (session.Status != SessionStatus.Lobby)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Session is no longer valid.");
        }

        User? dbUser = await UserService.GetUserByName(user.Identity!.Name!);
        dbUser!.CurrentSession = session;
        dbUser.Ready = false;
        await UserService.UpdateUser(dbUser);
        // await _context.SaveChangesAsync();
        LobbyResponseDto sessionDto = _mapper.Map<LobbyResponseDto>(session);
        return await req.CreateSuccessResponse(sessionDto);
    }

    [Function(nameof(LeaveSession))]
    [OpenApiOperation(operationId: "PostSession", tags: new[] {"session"}, Summary = "Leaves a game",
        Description = "Leaves a session ")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(JoinRequestDto),
        Description = "Leaves the joined session")]
    public async Task<MessageResponse> LeaveSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/leave")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        if (user == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User? dbUser = await UserService.GetUserByName(user.Identity!.Name!);
        var message = new SignalRMessageAction("playerLeft")
        {
            GroupName = dbUser!.CurrentSession!.SessionCode,
            Arguments = new object[] {dbUser}
        };

        // Host left; Remove all players from session
        if (dbUser.CurrentSession.HostId == dbUser.Id)
        {
            var session = dbUser.CurrentSession;
            foreach (User player in session.Players)
            {
                player.CurrentSession = null;
                player.Ready = false;
            }

            session.Status = SessionStatus.Cancelled;
        }

        dbUser.Ready = false;
        await UserService.UpdateUser(dbUser);
        // await _context.SaveChangesAsync();

        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function(nameof(CreateSession))]
    [OpenApiOperation(operationId: "GetSession", tags: new[] {"session"}, Summary = "Creates a session",
        Description = "Stars a session that has not created yet")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LobbyResponseDto),
        Description = "Creates a new session")]
    public async Task<HttpResponseData> CreateSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/create")]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        if (user == null)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        User? dbUser = await UserService.GetUserByName(user.Identity!.Name!);

        GameSession session = GameSession.New();
        session.HostId = dbUser!.Id;
        // _context.GameSessions.Add(session);

        await SessionService.AddSession(session);


        dbUser.CurrentSession = session;
        dbUser.Ready = false;

        await UserService.UpdateUser(dbUser);

        // await _context.SaveChangesAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        LobbyResponseDto sessionDto = _mapper.Map<LobbyResponseDto>(session);
        await response.WriteAsJsonAsync(sessionDto);
        return response;
    }

    [Function(nameof(StartSession))]
    [OpenApiOperation(operationId: "PostSession", tags: new[] {"session"}, Summary = "Starts a game",
        Description = "Starts a session that has not started yet")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StartGameDto),
        Description = "Session started")]
    public async Task<MessageResponse> StartSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/start")]
        HttpRequestData req, FunctionContext executionContext)
    {
        SessionOptionsDto? options = await req.ReadFromJsonAsync<SessionOptionsDto>();

        Random rnd = new Random();
        List<Card> dbCards = (await CardService.GetAllCards()).OrderBy(_ => rnd.Next()).Take(options!.Rounds).OrderBy(c => c.Id).ToList();

        List<CardDto> cards = _mapper.Map<List<CardDto>>(dbCards);

        var principal = executionContext.GetUser();
        if (principal == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User? user = await UserService.GetUserByName(principal.Identity!.Name!);


        user!.CurrentSession!.Rounds = options.Rounds;
        user.CurrentSession.Status = SessionStatus.Active;
        user.CurrentSession.Cards = dbCards;
        user.CurrentSession.RoundDurationSeconds = options.RoundDuration;

        await UserService.UpdateUser(user);


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

    [Function(nameof(ChangeReadyState))]
    [OpenApiOperation(operationId: "ChangeReadyState", tags: new[] {"session"}, Summary = "Change Ready state",
        Description = "Allows users in a lobby to change whether they are ready or not.")]
    [OpenApiRequestBody("application/json", typeof(ChangeReadinessDto), Description = "The ready state to change to", Required = true)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Ready state was changed")]
    public async Task<MessageResponse> ChangeReadyState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/ready")]
        HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "Hub")]
        SignalRConnectionInfo connectionInfo,
        FunctionContext executionContext)
    {
        ClaimsPrincipal? principal = executionContext.GetUser();
        User? user = await UserService.GetUserByName(principal.Identity!.Name!);

        ChangeReadinessDto? readiness = await req.ReadFromJsonAsync<ChangeReadinessDto>();

        user!.Ready = readiness!.Ready;
        LobbyPlayerDto playerDto = _mapper.Map<LobbyPlayerDto>(user);

        await UserService.UpdateUser(user);

        return new MessageResponse
        {
            Message = new SignalRMessageAction("readyStateChanged")
            {
                GroupName = user.CurrentSession!.SessionCode,
                Arguments = new object[] {playerDto}
            },
            UserResponse = req.CreateResponse(HttpStatusCode.OK)
        };
    }

    [Function(nameof(MatchHistory))]
    [OpenApiOperation(operationId: "MatchHistory", tags: new[] {"session"}, Summary = "Show Match History",
        Description = "Shows the match history of a session")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDto[]),
        Description = "the requested match history")]
    public async Task<HttpResponseData> MatchHistory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/history")]
        HttpRequestData req, FunctionContext executionContext)
    {
        var sessions = SessionService.GetSessions();

        SessionDto[]? dtos = _mapper.Map<SessionDto[]>(sessions);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(dtos);
        return response;
    }
}
