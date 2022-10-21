using System.Net;
using Azure.Storage.Blobs;
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
        var blob = _blobContainerClient.GetBlobClient("app/index.html");
        var response = req.CreateResponse(HttpStatusCode.OK);
        if (await blob.ExistsAsync())
        {
            var props = blob.GetPropertiesAsync().Result.Value;
            var ct = blob.GetPropertiesAsync().Result.Value.ContentType;

            response.Headers.Add("Content-Type", blob.GetPropertiesAsync().Result.Value.ContentType);
            await response.WriteStringAsync((await blob.DownloadContentAsync()).Value.Content.ToString());
        }

        return response;
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
