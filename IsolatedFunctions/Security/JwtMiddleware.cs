using System.Net.Http.Headers;
using System.Security.Claims;
using IsolatedFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
        // The OnConnected function is invoked by the signalR client potentially via websocket.
        // Websockets do not support custom headers (see https://github.com/dotnet/aspnetcore/issues/40659) so we skip validation here.
        // Imo this is no security issue since the websocket connection can only be established by providing a valid token in the first place.
        bool isOnConnected = context.FunctionDefinition.Name == "onconnected";

        if (!isOnConnected && context.BindingContext.BindingData.TryGetValue("Headers", out object? headerData))
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
                    Logger.LogError(e.Message);
                }
            }
        }

        await next(context);
    }
}
