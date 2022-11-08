using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.Enums;
using Domain.Models;
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
using Services.Interfaces;

namespace IsolatedFunctions.Controllers;

public class SessionController
{
    private readonly IMapper _mapper;

    private IUserService UserService { get; }
    private ISessionService SessionService { get; }
    private ICardService CardService { get; }

    private ISessionResponseService SessionResponseService { get; }


    public SessionController(IMapper mapper, IUserService userService, ISessionService sessionService,
        ICardService cardService,
        ISessionResponseService sessionResponseService)
    {
        UserService = userService;
        SessionService = sessionService;
        CardService = cardService;
        SessionResponseService = sessionResponseService;

        _mapper = mapper;
    }

    [Function("SubmitAnswer")]
    [OpenApiOperation(operationId: "SubmitAnswer", tags: new[] {"session"}, Summary = "Submit an answer",
        Description = "Submit an answer in the current session")]
    [OpenApiRequestBody("application/json", typeof(SubmitAnswerDto), Description = "Answer to submit")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(SessionResponseDto),
        Description = "The submitted answer")]
    public async Task<MessageResponse> SubmitAnswer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/submit")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);
        if (dbUser is null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        SubmitAnswerDto? dto = await req.ReadFromJsonAsync<SubmitAnswerDto>();

        // TODO: Verify functionality

        if (await SessionResponseService.UserCompletedQuestion(dbUser.Id, dbUser.CurrentSession!.CurrentRound))
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.BadRequest, "You already answered this question!")};
        }


        if (dto is null)
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.BadRequest)};
        }

        SessionResponse response = new()
        {
            User = dbUser,
            Session = dbUser.CurrentSession!,
            CreatedAt = DateTime.Now,
            CardNumber = dbUser.CurrentSession!.CurrentRound,
            Response = dto.Answer,
            ResponseType = SessionResponseType.Answer
        };

        await SessionResponseService.AddSessionResponse(response);
        UserDto userDto = _mapper.Map<UserDto>(dbUser);

        var message = new SignalRMessageAction("newAnswer")
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            Arguments = new object[] {userDto, dto.Answer}
        };

        HttpResponseData responseData = req.CreateResponse(HttpStatusCode.OK);
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
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);
        if (dbUser is null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        UserDto userDto = _mapper.Map<UserDto>(dbUser);

        ChatMessageDto? chatMessage = await req.ReadFromJsonAsync<ChatMessageDto>();

        var message = new SignalRMessageAction("newMessage")
        {
            //TODO: Check for NULL
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
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);

        if (dbUser is null)
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.Unauthorized)};
        }

        if (dbUser.CurrentSession is null)
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.BadRequest, "You are not in a session!")};
        }

        // Check if executing user is session host or not
        if (dbUser.CurrentSession!.HostId != dbUser.Id)
        {
            return new MessageResponse
            {
                UserResponse = await req.CreateErrorResponse(HttpStatusCode.Forbidden, "You are not the lobby host.")
            };
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
            {
                UserResponse = await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Game is already finished.")
            };
        }

        await UserService.UpdateUser(dbUser);

        SignalRMessageAction message = new("nextRound")
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
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);

        if (dbUser is null)
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.Unauthorized)};
        }


        // Check if executing user is session host or not
        bool isSessionHost = dbUser.CurrentSession!.HostId == dbUser.Id;
        if (!isSessionHost)
        {
            return new MessageResponse
                {UserResponse = await req.CreateErrorResponse(HttpStatusCode.Forbidden, "You are not the lobby host.")};
        }

        dbUser.CurrentSession.Status = SessionStatus.Completed;
        await UserService.UpdateUser(dbUser);

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

        SignalRMessageAction message = new("endSession")
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            Arguments = new object[] {"end"}
        };

        // Remove all users from session
        await UserService.GetUsersInSession(dbUser.CurrentSession.Id)
            .ForEachAsync(usr =>
            {
                usr.CurrentSession = null;
                usr.Ready = false;
                UserService.UpdateUser(usr);
            });


        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function(nameof(JoinSession))]
    [OpenApiOperation(operationId: "GetSession", tags: new[] {"session"}, Summary = "Joins a game",
        Description = "Joins a session that has not started yet")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(JoinRequestDto),
        Description = "joins the requested session")]
    public async Task<HttpResponseData> JoinSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/join")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);

        if (dbUser is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized);
        }

        JoinRequestDto? joinRequestDto = await req.ReadFromJsonAsync<JoinRequestDto>();
        if (joinRequestDto is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request");
        }

        GameSession? session = await SessionService.GetSessionByJoinCode(joinRequestDto.SessionAuth);

        if (session is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Session not found");
        }

        if (session.Status != SessionStatus.Lobby)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Session is no longer valid.");
        }

        dbUser.CurrentSession = session;
        dbUser.Ready = false;
        await UserService.UpdateUser(dbUser);

        LobbyResponseDto sessionDto = _mapper.Map<LobbyResponseDto>(session);
        return await req.CreateSuccessResponse(sessionDto);
    }

    [Function(nameof(LeaveSession))]
    [OpenApiOperation(operationId: "PostSession", tags: new[] {"session"}, Summary = "Leaves a game",
        Description = "Leaves a session ")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(JoinRequestDto),
        Description = "Leaves the joined session")]
    public async Task<MessageResponse> LeaveSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/leave")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);

        if (dbUser is null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        SignalRMessageAction message = new SignalRMessageAction("playerLeft")
        {
            GroupName = dbUser.CurrentSession!.SessionCode,
            Arguments = new object[] {dbUser}
        };

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

        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function(nameof(CreateSession))]
    [OpenApiOperation(operationId: "GetSession", tags: new[] {"session"}, Summary = "Creates a session",
        Description = "Stars a session that has not created yet")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(LobbyResponseDto),
        Description = "Creates a new session")]
    public async Task<HttpResponseData> CreateSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/create")]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);
        if (dbUser is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized);
        }


        GameSession session = GameSession.New();
        session.HostId = dbUser.Id;
        await SessionService.AddSession(session);

        dbUser.CurrentSession = session;
        dbUser.Ready = false;
        await UserService.UpdateUser(dbUser);

        LobbyResponseDto sessionDto = _mapper.Map<LobbyResponseDto>(session);
        return await req.CreateSuccessResponse(sessionDto);
    }

    [Function(nameof(StartSession))]
    [OpenApiOperation(operationId: "PostSession", tags: new[] {"session"}, Summary = "Starts a game",
        Description = "Starts a session that has not started yet")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(StartGameDto),
        Description = "Session started")]
    public async Task<MessageResponse> StartSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/start")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? principal = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(principal?.Identity?.Name!);

        if (dbUser is null)
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.Unauthorized)};
        }

        SessionOptionsDto? options = await req.ReadFromJsonAsync<SessionOptionsDto>();

        if (options is null)
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid options")};
        }

        if (dbUser.CurrentSession is null)
        {
            return new MessageResponse
                {UserResponse = await req.CreateErrorResponse(HttpStatusCode.BadRequest, "You are not in a session")};
        }

        Random rnd = new Random();
        List<Card> randomizedCards = (await CardService.GetAllCards()).OrderBy(_ => rnd.Next()).Take(options.Rounds)
            .OrderBy(c => c.Id).ToList();

        List<CardDto> cards = _mapper.Map<List<CardDto>>(randomizedCards);


        dbUser.CurrentSession!.Rounds = options.Rounds;
        dbUser.CurrentSession.Status = SessionStatus.Active;
        dbUser.CurrentSession.Cards = randomizedCards;
        dbUser.CurrentSession.RoundDurationSeconds = options.RoundDuration;

        await UserService.UpdateUser(dbUser);

        StartGameDto responseDto = new()
        {
            Cards = cards,
            RoundDuration = options.RoundDuration
        };


        SignalRMessageAction message = new SignalRMessageAction("startGame")
        {
            GroupName = dbUser.CurrentSession.SessionCode,
            Arguments = new object[] {responseDto}
        };

        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function(nameof(ChangeReadyState))]
    [OpenApiOperation(operationId: "ChangeReadyState", tags: new[] {"session"}, Summary = "Change Ready state",
        Description = "Allows users in a lobby to change whether they are ready or not.")]
    [OpenApiRequestBody("application/json", typeof(ChangeReadinessDto), Description = "The ready state to change to",
        Required = true)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Ready state was changed")]
    public async Task<MessageResponse> ChangeReadyState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/ready")]
        HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "Hub")]
        SignalRConnectionInfo connectionInfo,
        FunctionContext executionContext)
    {
        ClaimsPrincipal? principal = executionContext.GetUser();
        User? user = await UserService.GetUserByName(principal?.Identity?.Name!);

        if (user is null)
        {
            return new MessageResponse {UserResponse = await req.CreateErrorResponse(HttpStatusCode.Unauthorized)};
        }

        ChangeReadinessDto? readiness = await req.ReadFromJsonAsync<ChangeReadinessDto>();

        user.Ready = readiness!.Ready;
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
        Description = "Shows the match history")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(SessionDto[]),
        Description = "The match history")]
    public async Task<HttpResponseData> MatchHistory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "session/history")]
        HttpRequestData req, FunctionContext executionContext)
    {
        List<GameSession> sessions = await SessionService.GetSessions();

        SessionDto[]? matches = _mapper.Map<SessionDto[]>(sessions);
        return await req.CreateSuccessResponse(matches);
    }
}
