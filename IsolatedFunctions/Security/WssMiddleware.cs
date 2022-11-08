using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Services;

namespace IsolatedFunctions.Security;

public class WssMiddleware : IFunctionsWorkerMiddleware
{
    private ILogger<WssMiddleware> Logger { get; }
    private ITokenService TokenService { get; }

    public WssMiddleware(ITokenService tokenService, ILogger<WssMiddleware> logger)
    {
        TokenService = tokenService;
        Logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.BindingContext.BindingData.TryGetValue("token", out object? token))
        {
            try
            {
                ClaimsPrincipal user = await TokenService.GetByValue(token?.ToString()!);
                context.Items["User"] = user;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while validating websocket token");
                return;
            }
        }
        await next(context);
    }
}
