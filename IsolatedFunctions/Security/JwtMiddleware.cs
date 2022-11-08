using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services;

namespace IsolatedFunctions.Security;

public class JwtMiddleware : IFunctionsWorkerMiddleware
{
    private ILogger<JwtMiddleware> Logger { get; }
    private ITokenService TokenService { get; }

    public JwtMiddleware(ITokenService tokenService, ILogger<JwtMiddleware> logger)
    {
        TokenService = tokenService;
        Logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {

        if (context.BindingContext.BindingData.TryGetValue("Headers", out object? headerData))
        {
            string headersString = headerData!.ToString()!;

            Dictionary<string, string> headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersString);

            if (headers.TryGetValue("Authorization", out string? authorizationHeader))
            {
                try
                {
                    AuthenticationHeaderValue bearerHeader = AuthenticationHeaderValue.Parse(authorizationHeader);

                    ClaimsPrincipal user = await TokenService.GetByValue(bearerHeader.Parameter!);

                    context.Items["User"] = user;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while parsing authorization header");
                    return;
                }
            }
        }
        await next(context);
    }
}
