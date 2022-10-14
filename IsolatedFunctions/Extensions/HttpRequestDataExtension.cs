using System.Net;
using IsolatedFunctions.DTO;
using Microsoft.Azure.Functions.Worker.Http;

namespace IsolatedFunctions.Extensions;

public static class HttpRequestDataExtension
{
    public static async Task<HttpResponseData> CreateErrorResponse(this HttpRequestData request, HttpStatusCode statusCode,
        string? message = null)
    {
        HttpResponseData response = request.CreateResponse();
        if (message == null)
        {
            message = statusCode.ToString();
        }

        await response.WriteAsJsonAsync(new ErrorDto {Message = message, Code = (int) statusCode}, statusCode);
        return response;
    }

    public static async Task<HttpResponseData> CreateSuccessResponse<T>(this HttpRequestData request, T result)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        if (result != null)
        {
            await response.WriteAsJsonAsync<T>(result);
        }

        return response;
    }
}
