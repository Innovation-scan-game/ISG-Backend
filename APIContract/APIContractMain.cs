using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using APIContract.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace APIContract;

public class APIContractMain
{
    private readonly ILogger<APIContractMain> _logger;

    public APIContractMain(ILogger<APIContractMain> log)
    {
        _logger = log;
    }

    [FunctionName("userInfo")]
    [OpenApiOperation(operationId: "UserInfo", tags: new[] {"user"}, Summary = "Gets user info",
        Description = "Gets information about user by user ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDTO))]
    public async Task<IActionResult> UserInfo([HttpTrigger(AuthorizationLevel.Function, "get", Route = "userInfo/{id}")] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("userList")]
    [OpenApiOperation(operationId: "UserList", tags: new[] {"user"}, Summary = "Gets user list", Description = "Gets list of users")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDTO[]))]
    public async Task<IActionResult> UserList([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
    {
        UserDTO user1 = new() {Username = "jurek", Email = "jurek.baumann@gmail.com", Role = UserRoleEnum.Admin};
        UserDTO user2 = new() {Username = "John Doe", Email = "john@doe.com", Role = UserRoleEnum.User};
        UserDTO user3 = new() {Username = "Hans", Email = "hans@gmail.com", Role = UserRoleEnum.User};
        UserDTO user4 = new() {Username = "Random Guy", Email = "rnd@gmail.com", Role = UserRoleEnum.User};

        return new OkObjectResult(new[] {user1, user2, user3, user4});
    }

    [FunctionName("deleteUser")]
    [OpenApiOperation(operationId: "DeleteUser", tags: new[] {"user"}, Summary = "Delete user", Description = "Delete user by id")]
    public async Task<IActionResult> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "deleteUser/{id}")] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("newUser")]
    [OpenApiRequestBody("application/json", typeof(CreateUserDTO), Required = true)]
    [OpenApiOperation(operationId: "CreateUser", tags: new[] {"user"}, Summary = "Creates a new user",
        Description = "Creates a new user based on the data given")]
    public async Task<IActionResult> CreateUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("login")]
    [OpenApiRequestBody("application/json", typeof(CreateUserDTO), Required = true)]
    [OpenApiOperation(operationId: "LoginUser", tags: new[] {"user"}, Summary = "A login for the user",
        Description = "A user can login based on their ID by using their token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponseDTO),
        Description = "User authorization token")]
    public async Task<IActionResult> LoginUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("getCards")]
    [OpenApiOperation(operationId: "GetCards", tags: new[] {"cards"}, Summary = "Gets a list of all the cards",
        Description = "Gets a list of all the cards that are currently in the game")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardListDTO),
        Description = "The OK response")]
    public async Task<IActionResult> GetCards([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
    {
        CardListDTO cardList = new()
        {
            Cards = new CardDTO[4]
        };

        cardList.Cards[0] = new CardDTO {CardName = "card1", CardBody = "card1 body", CardNumber = 0, CardType = CardTypeEnum.OpenAnswer};
        cardList.Cards[1] = new CardDTO
            {CardName = "card2", CardBody = "card2 body", CardNumber = 1, CardType = CardTypeEnum.MultipleChoice};
        cardList.Cards[2] = new CardDTO {CardName = "card2", CardBody = "card2 body", CardNumber = 2, CardType = CardTypeEnum.OpenAnswer};
        cardList.Cards[3] = new CardDTO {CardName = "card3", CardBody = "card3 body", CardNumber = 3, CardType = CardTypeEnum.OpenAnswer};


        return new OkObjectResult(cardList);
    }

    [FunctionName("getCard")]
    [OpenApiOperation(operationId: "GetCard", tags: new[] {"cards"}, Summary = "Get a single card",
        Description = "Gets a single card based on the card ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDTO),
        Description = "CardDTO")]
    public async Task<IActionResult> GetCard([HttpTrigger(AuthorizationLevel.Function, "get", Route = "getCard/{id}")] HttpRequest req,
        string id)
    {
        int.TryParse(id, out int cardId);
        CardDTO card = new()
        {
            ID = Guid.NewGuid(), CardName = $"card {id}", CardBody = $"card {id} body", CardNumber = cardId,
            CardType = CardTypeEnum.OpenAnswer
        };
        return new OkObjectResult(card);
    }

    [FunctionName("postCard")]
    [OpenApiOperation(operationId: "PostCard", tags: new[] {"cards"}, Summary = "Creates a card",
        Description = "Creates a card based on the data given")]
    [OpenApiRequestBody("application/json", typeof(CardDTO), Required = true)]
    public async Task<IActionResult> PostCard([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("putCard")]
    [OpenApiOperation(operationId: "PutCard", tags: new[] {"cards"}, Summary = "Updates a card",
        Description = "Updates a card based on the card ID with the given data")]
    [OpenApiRequestBody("application/json", typeof(CardDTO), Required = true)]
    public async Task<IActionResult> PutCard([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var card = JsonConvert.DeserializeObject<CardDTO>(requestBody);
        Console.WriteLine(card.CardName);
        return new OkObjectResult("");
    }

    [FunctionName("deleteCard")]
    [OpenApiOperation(operationId: "DeleteCard", tags: new[] {"cards"}, Summary = "Deletes a card",
        Description = "Deletes a card based on the card ID")]
    public async Task<IActionResult> DeleteCard(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "deleteCard/{id}")] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("newRoom")]
    [OpenApiOperation(operationId: "NewRoom", tags: new[] {"game"}, Summary = "Creates new room", Description = "Creates a new room")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LobbyResponseDTO),
        Description = "The OK response")]
    public async Task<IActionResult> NewRoom([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("deleteRoom")]
    [OpenApiOperation(operationId: "DeleteCurrentRoom", tags: new[] {"game"}, Summary = "Deletes current room",
        Description = "Deletes a room based on room ID")]
    public async Task<IActionResult> DeleteCurrentRoom(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "deleteRoom/{id}")] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("settings")]
    [OpenApiOperation(operationId: "Settings", tags: new[] {"game"}, Summary = "Update room settings",
        Description = "Update settings relevant to current game")]
    [OpenApiRequestBody("application/json", typeof(GameSettingsDTO), Required = true)]
    public async Task<IActionResult> Settings([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("addUser")]
    [OpenApiOperation(operationId: "AddUser", tags: new[] {"game"}, Summary = "Invites user to room",
        Description = "Invites user to room by ID")]
    [OpenApiRequestBody("application/json", typeof(UserDTO), Required = true)]
    public async Task<IActionResult> AddUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("kickUser")]
    [OpenApiOperation(operationId: "KickUser", tags: new[] {"game"}, Summary = "Removes user from room",
        Description = "Kicks specified user from current room")]
    public async Task<IActionResult> KickUser([HttpTrigger(AuthorizationLevel.Function, "post", Route = "kickUser/{id}")] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("submitResponse")]
    [OpenApiOperation(operationId: "SubmitResponse", tags: new[] {"game"}, Summary = "Submit response to a card",
        Description = "Submit response to a card")]
    [OpenApiRequestBody("application/json", typeof(CardResponseDTO), Required = true)]
    public async Task<IActionResult> SubmitResponse([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("joinGame")]
    [OpenApiOperation(operationId: "JoinGame", tags: new[] {"game"}, Summary = "Get room data",
        Description = "Access game data by room code")]
    [OpenApiRequestBody("application/json", typeof(JoinRequestDTO), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LobbyResponseDTO),
        Description = "The OK response")]
    public async Task<IActionResult> JoinGame([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("addEmoji")]
    [OpenApiOperation(operationId: "AddEmoji", tags: new[] {"game"}, Summary = "Add emoji to response",
        Description = "React to a response with an emoji")]
    [OpenApiRequestBody("application/json", typeof(EmojiDTO), Required = true)]
    public async Task<IActionResult> AddEmoji([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("matchHistory")]
    [OpenApiOperation(operationId: "MatchHistory", tags: new[] {"game"}, Summary = "Get user history",
        Description = "Get the game history of a user by ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(HistoryDTO),
        Description = "The OK response")]
    public async Task<IActionResult> MatchHistory(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "matchHistory/{id}")] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("chatMessage")]
    [OpenApiOperation(operationId: "ChatMessage", tags: new[] {"game"}, Summary = "Send a message",
        Description = "Send a message to rest of the participants")]
    [OpenApiRequestBody("application/json", typeof(ChatMessageDTO), Required = true)]
    public async Task<IActionResult> ChatMessage([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }
}
