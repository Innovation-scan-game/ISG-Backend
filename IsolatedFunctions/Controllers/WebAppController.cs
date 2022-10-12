using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace IsolatedFunctions.Controllers;

public class WebAppController
{
    // private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _blobContainerClient;

    public WebAppController(BlobServiceClient blobServiceClient)
    {
        _blobContainerClient = blobServiceClient.GetBlobContainerClient("$web");
    }


    // [Function(nameof(CmsProxy))]
    // public async Task<HttpResponseData> CmsProxy(
    //     [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/{*path}")]
    //     HttpRequestData req,
    //     FunctionContext executionContext,
    //     string path)
    // {
    //     var response = req.CreateResponse(HttpStatusCode.OK);
    //
    //     var blob = _blobContainerClient.GetBlobClient(path);
    //     if (await blob.ExistsAsync())
    //     {
    //         response.Headers.Add("Content-Type", blob.GetPropertiesAsync().Result.Value.ContentType);
    //         await response.WriteStringAsync((await blob.DownloadContentAsync()).Value.Content.ToString());
    //     }
    //
    //     return response;
    // }
    //
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cmsold/{*route}")] HttpRequestData req, string route)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(await File.ReadAllTextAsync("webapp/cms/index.html"));
        response.Headers.Add("Content-Type", "text/html");
        return response;
    }
}
