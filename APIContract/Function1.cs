using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace APIContract {
    public class Function1 {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> log) {
            _logger = log;
        }

        [FunctionName("userInfo/{id}")]
        [OpenApiOperation(operationId: "UserInfo", tags: new[] { "user" }, Summary = "Gets user info", Description = "Gets information about user by user ID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> UserInfo([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("deleteUser")]
        [OpenApiOperation(operationId: "DeleteUser", tags: new[] { "user" }, Summary = "Delete user", Description = "Delete user by id")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DeleteUser([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("newUser")]
        [OpenApiOperation(operationId: "CreateUser", tags: new[] { "user" }, Summary = "Creates a new user", Description = "Creates a new user based on the data given")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> CreateUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("login/{id}")]
        [OpenApiOperation(operationId: "LoginUser", tags: new[] { "user" }, Summary = "A login for the user", Description = "A user can login based on their ID by using their token")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> LoginUser([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("getCards")]
        [OpenApiOperation(operationId: "GetCards", tags: new[] { "cards" }, Summary = "Gets a list of all the cards", Description = "Gets a list of all the cards that are currently in the game")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetCards([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("getCard/{id}")]
        [OpenApiOperation(operationId: "GetCard", tags: new[] { "cards" }, Summary = "Get a single card", Description = "Gets a single card based on the card ID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetCard([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("postCard")]
        [OpenApiOperation(operationId: "PostCard", tags: new[] { "cards" }, Summary = "Creates a card", Description = "Creates a card based on the data given")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> PostCard([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("putCard/{id}")]
        [OpenApiOperation(operationId: "PutCard", tags: new[] { "cards" }, Summary = "Updates a card", Description = "Updates a card based on the card ID with the given data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> PutCard([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("deleteCard/{id}")]
        [OpenApiOperation(operationId: "DeleteCard", tags: new[] { "cards" }, Summary = "Deletes a card", Description = "Deletes a card based on the card ID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DeleteCard([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("currentRoom")]
        [OpenApiOperation(operationId: "CurrentRoom", tags: new[] { "game" }, Summary = "Gets current users", Description = "Gets all users in current room")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> CurrentRoom([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("newRoom")]
        [OpenApiOperation(operationId: "NewRoom", tags: new[] { "game" }, Summary = "Creates new room", Description = "Creates a new room")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> NewRoom([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("deleteCurrentRoom/{id}")]
        [OpenApiOperation(operationId: "DeleteCurrentRoom", tags: new[] { "game" }, Summary = "Deletes current room", Description = "Deletes a room based on room ID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DeleteCurrentRoom([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("settings")]
        [OpenApiOperation(operationId: "Settings", tags: new[] { "game" }, Summary = "Update room settings", Description = "Update settings relevant to current game")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Settings([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("addUser")]
        [OpenApiOperation(operationId: "AddUser", tags: new[] { "game" }, Summary = "Invites user to room", Description = "Update settings relevant to current game")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> AddUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("kickUser")]
        [OpenApiOperation(operationId: "KickUser", tags: new[] { "game" }, Summary = "Removes user from room", Description = "Kicks specified user from current room")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> KickUser([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("nextRound")]
        [OpenApiOperation(operationId: "NextRound", tags: new[] { "game" }, Summary = "Progress to next round", Description = "Advance to round after current")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> NextRound([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("joinGame")]
        [OpenApiOperation(operationId: "JoinGame", tags: new[] { "game" }, Summary = "Get room data", Description = "Access game data by room code")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> JoinGame([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("addEmoji")]
        [OpenApiOperation(operationId: "AddEmoji", tags: new[] { "game" }, Summary = "Add emoji to response", Description = "React to a response with an emoji")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> AddEmoji([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("matchHistory/{id}")]
        [OpenApiOperation(operationId: "MatchHistory", tags: new[] { "user" }, Summary = "Get user history", Description = "Get the game history of a user by ID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> MatchHistory([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }

        [FunctionName("chatMessage")]
        [OpenApiOperation(operationId: "ChatMessage", tags: new[] { "game" }, Summary = "Send a message", Description = "Send a message to rest of the participants")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ChatMessage([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req) {
            return new OkObjectResult("a");
        }
    }
}

