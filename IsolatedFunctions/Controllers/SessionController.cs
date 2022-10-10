using System.Net;
using System.Security.Claims;
using AutoMapper;
using DAL.Data;
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
    public async Task<HttpResponseData> JoinSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/join")]
        HttpRequestData req, FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        if (user == null)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        JoinRequestDto? joinRequestDto = await req.ReadFromJsonAsync<JoinRequestDto>();
        if (joinRequestDto == null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        GameSession? session =
            await _context.GameSessions.Include(s => s.Players)
                .FirstOrDefaultAsync(session => session.SessionCode == joinRequestDto.SessionAuth && session.Status == SessionStatus.Lobby);

        if (session == null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        if (session.Status != SessionStatus.Lobby)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteStringAsync("Invalid session status");
            return res;
        }

        User? dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Name == user.Identity!.Name);
        dbUser!.CurrentSession = session;
        dbUser.Ready = false;
        await _context.SaveChangesAsync();
        LobbyResponseDto sessionDto = _mapper.Map<LobbyResponseDto>(session);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(sessionDto);
        return response;
    }

    [Function("LeaveSession")]
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
            dbUser.CurrentSession.Status = SessionStatus.Cancelled;
        }

        dbUser.CurrentSession = null;
        dbUser.Ready = false;
        await _context.SaveChangesAsync();

        return new MessageResponse {Message = message, UserResponse = req.CreateResponse(HttpStatusCode.OK)};
    }

    [Function("CreateSession")]
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
    public async Task<MessageResponse> StartSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/start")]
        HttpRequestData req, FunctionContext executionContext)
    {
        SessionOptionsDto? options = await req.ReadFromJsonAsync<SessionOptionsDto>();

        Random rnd = new Random();
        IEnumerable<Card> dbCards = _context.Cards.ToList().OrderBy(_ => rnd.Next()).Take(options!.Rounds);

        CardDto[] cards = _mapper.Map<CardDto[]>(dbCards);

        var principal = executionContext.GetUser();
        if (principal == null)
        {
            return new MessageResponse {UserResponse = req.CreateResponse(HttpStatusCode.Unauthorized)};
        }

        User? user = await _context.Users.Include(u => u.CurrentSession).FirstOrDefaultAsync(u => u.Name == principal.Identity!.Name);


        user!.CurrentSession!.Rounds = options.Rounds;
        user.CurrentSession.Status = SessionStatus.Active;
        await _context.SaveChangesAsync();


        SessionResponseDto responseDto = new()
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
}
