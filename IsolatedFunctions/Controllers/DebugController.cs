using System.Net;
using System.Security.Claims;
using DAL.Data;
using IsolatedFunctions.Extensions;
using IsolatedFunctions.Outputs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace IsolatedFunctions.Controllers;

public class DebugController
{
    private readonly InnovationGameDbContext _context;

    public DebugController(InnovationGameDbContext context)
    {
        _context = context;
    }

    [Function("SignalTest")]
    public MessageResponse SignalTest([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        // ClaimsPrincipal? u = executionContext.GetUser();
        Console.WriteLine("hub");
        // await _hub.SendMessage("lello");

        // var dbUser = await _context.Users.Include(usr => usr.CurrentSession).FirstAsync(usr => usr.Name == u.Identity.Name);

        var msg = new SignalRMessageAction("newMessage")
        {
            GroupName = "test",
            Arguments = new object[] {"hihi"},
        };

        return new MessageResponse {Message = msg, UserResponse = req.CreateResponse(HttpStatusCode.OK)};

        // return req.CreateResponse(HttpStatusCode.OK);
    }

    [Function("ClearDatabase")]
    public async Task<HttpResponseData> ClearDatabase([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();
        return req.CreateResponse(HttpStatusCode.OK);
    }

    [Function(nameof(RequireAdmin))]
    public HttpResponseData RequireAdmin([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        if (!executionContext.IsAdmin())
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        // Do admin stuff
        return req.CreateResponse(HttpStatusCode.OK);
    }

    [Function("AuthOnly")]
    public HttpResponseData AuthOnly([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        if (user == null)
        {
            return req.CreateResponse(HttpStatusCode.Unauthorized);
        }

        // var identities = user.Identities;
        // var id = user.Identity;
        // var isAdmin = user.IsInRole(UserRoleEnum.Admin.ToString());

        return req.CreateResponse(HttpStatusCode.OK);
    }


}
