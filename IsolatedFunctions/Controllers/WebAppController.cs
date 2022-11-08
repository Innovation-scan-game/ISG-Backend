using System.Net;
using Azure.Storage.Blobs;
using IsolatedFunctions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace IsolatedFunctions.Controllers;

public class WebAppController
{
    private readonly BlobContainerClient _blobContainerClient;

    public WebAppController(BlobServiceClient blobServiceClient)
    {
        _blobContainerClient = blobServiceClient.GetBlobContainerClient("$web");
    }

    [Function(nameof(WebApp))]
    public async Task<HttpResponseData> WebApp([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "web/app")] HttpRequestData req)
    {
        BlobClient? blob = _blobContainerClient.GetBlobClient("app/index.html1");
        if (await blob.ExistsAsync())
        {
            return await req.CreateFileResponse(blob);
        }
        return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Not found");
    }


    [Function(nameof(Prototype))]
    public async Task<HttpResponseData> Prototype([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(await File.ReadAllTextAsync("webapp/index.html"));
        response.Headers.Add("Content-Type", "text/html");
        return response;
    }

    [Function(nameof(Cms))]
    public async Task<HttpResponseData> Cms(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cmsold/{*route}")]
        HttpRequestData req, string route)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(await File.ReadAllTextAsync("webapp/cms/index.html"));
        response.Headers.Add("Content-Type", "text/html");
        return response;
    }
}
