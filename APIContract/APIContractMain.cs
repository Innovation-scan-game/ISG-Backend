using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace APIContract;
public class APIContractMain {
    private readonly ILogger<APIContractMain> _logger;

    public APIContractMain(ILogger<APIContractMain> log) {
        _logger = log;
    }

    [FunctionName("userInfo/{id}")]
    [OpenApiOperation(operationId: "UserInfo", tags: new[] { "user" }, Summary = "Gets user info", Description = "Gets information about user by user ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDTO))]
    public async Task<IActionResult> UserInfo([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("deleteUser/{id}")]
    [OpenApiOperation(operationId: "DeleteUser", tags: new[] { "user" }, Summary = "Delete user", Description = "Delete user by id")]
    public async Task<IActionResult> DeleteUser([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("newUser")]
    [OpenApiRequestBody("application/json", typeof(CreateUserDTO), Required = true)]
    [OpenApiOperation(operationId: "CreateUser", tags: new[] { "user" }, Summary = "Creates a new user", Description = "Creates a new user based on the data given")]
    public async Task<IActionResult> CreateUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("login")]
    [OpenApiRequestBody("application/json", typeof(CreateUserDTO), Required = true)]
    [OpenApiOperation(operationId: "LoginUser", tags: new[] { "user" }, Summary = "A login for the user", Description = "A user can login based on their ID by using their token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponseDTO), Description = "User authorization token")]
    public async Task<IActionResult> LoginUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("getCards")]
    [OpenApiOperation(operationId: "GetCards", tags: new[] { "cards" }, Summary = "Gets a list of all the cards", Description = "Gets a list of all the cards that are currently in the game")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardListDTO), Description = "The OK response")]
    public async Task<IActionResult> GetCards([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("getCard/{id}")]
    [OpenApiOperation(operationId: "GetCard", tags: new[] { "cards" }, Summary = "Get a single card", Description = "Gets a single card based on the card ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDTO), Description = "CardDTO")]
    public async Task<IActionResult> GetCard([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("postCard")]
    [OpenApiOperation(operationId: "PostCard", tags: new[] { "cards" }, Summary = "Creates a card", Description = "Creates a card based on the data given")]
    [OpenApiRequestBody("application/json", typeof(CardDTO), Required = true)]
    public async Task<IActionResult> PostCard([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("putCard")]
    [OpenApiOperation(operationId: "PutCard", tags: new[] { "cards" }, Summary = "Updates a card", Description = "Updates a card based on the card ID with the given data")]
    [OpenApiRequestBody("application/json", typeof(CardDTO), Required = true)]
    public async Task<IActionResult> PutCard([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("deleteCard/{id}")]
    [OpenApiOperation(operationId: "DeleteCard", tags: new[] { "cards" }, Summary = "Deletes a card", Description = "Deletes a card based on the card ID")]
    public async Task<IActionResult> DeleteCard([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("newRoom")]
    [OpenApiOperation(operationId: "NewRoom", tags: new[] { "game" }, Summary = "Creates new room", Description = "Creates a new room")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LobbyResponseDTO), Description = "The OK response")]
    public async Task<IActionResult> NewRoom([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("deleteRoom/{id}")]
    [OpenApiOperation(operationId: "DeleteCurrentRoom", tags: new[] { "game" }, Summary = "Deletes current room", Description = "Deletes a room based on room ID")]
    public async Task<IActionResult> DeleteCurrentRoom([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("settings")]
    [OpenApiOperation(operationId: "Settings", tags: new[] { "game" }, Summary = "Update room settings", Description = "Update settings relevant to current game")]
    [OpenApiRequestBody("application/json", typeof(GameSettingsDTO), Required = true)]
    public async Task<IActionResult> Settings([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("addUser")]
    [OpenApiOperation(operationId: "AddUser", tags: new[] { "game" }, Summary = "Invites user to room", Description = "Invites user to room by ID")]
    [OpenApiRequestBody("application/json", typeof(UserDTO), Required = true)]
    public async Task<IActionResult> AddUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("kickUser/{id}")]
    [OpenApiOperation(operationId: "KickUser", tags: new[] { "game" }, Summary = "Removes user from room", Description = "Kicks specified user from current room")]
    public async Task<IActionResult> KickUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }
    
    [FunctionName("submitResponse")]
    [OpenApiOperation(operationId: "SubmitResponse", tags: new[] { "game" }, Summary = "Submit response to a card", Description = "Submit response to a card")]
    [OpenApiRequestBody("application/json", typeof(CardResponseDTO), Required = true)]
    public async Task<IActionResult> SubmitResponse([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("joinGame")]
    [OpenApiOperation(operationId: "JoinGame", tags: new[] { "game" }, Summary = "Get room data", Description = "Access game data by room code")]
    [OpenApiRequestBody("application/json", typeof(JoinRequestDTO), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LobbyResponseDTO), Description = "The OK response")]
    public async Task<IActionResult> JoinGame([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("addEmoji")]
    [OpenApiOperation(operationId: "AddEmoji", tags: new[] { "game" }, Summary = "Add emoji to response", Description = "React to a response with an emoji")]
    [OpenApiRequestBody("application/json", typeof(EmojiDTO), Required = true)]
    public async Task<IActionResult> AddEmoji([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("matchHistory/{id}")]
    [OpenApiOperation(operationId: "MatchHistory", tags: new[] { "game" }, Summary = "Get user history", Description = "Get the game history of a user by ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(HistoryDTO), Description = "The OK response")]
    public async Task<IActionResult> MatchHistory([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }

    [FunctionName("chatMessage")]
    [OpenApiOperation(operationId: "ChatMessage", tags: new[] { "game" }, Summary = "Send a message", Description = "Send a message to rest of the participants")]
    [OpenApiRequestBody("application/json", typeof(ChatMessageDTO), Required = true)]
    public async Task<IActionResult> ChatMessage([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req) {
        return new OkObjectResult("");
    }
}

