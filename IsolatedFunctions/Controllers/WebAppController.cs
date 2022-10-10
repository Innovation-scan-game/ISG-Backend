using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace IsolatedFunctions.Controllers;

public class WebAppController
{
    [Function(nameof(Prototype))]
    public async Task<HttpResponseData> Prototype([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(await File.ReadAllTextAsync("webapp/index.html"));
        response.Headers.Add("Content-Type", "text/html");
        return response;
    }

    [Function(nameof(Cms))]
    public async Task<HttpResponseData> Cms([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(await File.ReadAllTextAsync("webapp/cms.html"));
        response.Headers.Add("Content-Type", "text/html");
        return response;
    }
}
