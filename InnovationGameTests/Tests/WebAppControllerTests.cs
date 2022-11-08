// using System.Net.Mime;
// using Azure;
// using Azure.Storage.Blobs;
// using Azure.Storage.Blobs.Models;
// using IsolatedFunctions.Controllers;
// using Moq;
// using NSubstitute;
//
// namespace InnovationGameTests.Tests;
//
// public class WebAppControllerTests
// {
//     private WebAppController _webAppController;
//
//     [OneTimeSetUp]
//     public void GlobalSetup()
//     {
//         var blobProperties = new BlobProperties();
//
//         // var blobProperties = new Mock<BlobProperties>();
//         // blobProperties.Setup(b => b.ContentType).Returns("application/json");
//
//         var responseMock = new Mock<Response>();
//
//         var blobClient = new Mock<BlobClient>();
//         blobClient.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(true, responseMock.Object));
//
//         blobClient.Setup(m => m.GetPropertiesAsync(null, CancellationToken.None))
//             .ReturnsAsync(Response.FromValue<BlobProperties>(new MockBlobProperties(), responseMock.Object));
//         // blobClient.Setup(b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
//         //     .ReturnsAsync(new Response(blobProperties));
//
//
//         var blobContainerClient = new Mock<BlobContainerClient>();
//         blobContainerClient.Setup(b => b.GetBlobClient(It.IsAny<string>())).Returns(blobClient.Object);
//
//         var blobServiceClient = new Mock<BlobServiceClient>();
//         blobServiceClient.Setup(b => b.GetBlobContainerClient(It.IsAny<string>())).Returns(blobContainerClient.Object);
//
//
//         BlobServiceClient blobC = new();
//
//         _webAppController = new WebAppController(blobServiceClient.Object);
//     }
//
//     [Test]
//     public async Task TestGetWebApp()
//     {
//         var req = MockHelpers.CreateHttpRequestData();
//         var response = await _webAppController.WebApp(req);
//         Console.WriteLine(response);
//     }
// }
