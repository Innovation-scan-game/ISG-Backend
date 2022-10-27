using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Domain.Enums;
using IsolatedFunctions.DTO;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.DTO.GameSessionDTOs;
using IsolatedFunctions.DTO.SignalDTOs;
using IsolatedFunctions.DTO.UserDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionsApp;

public class APIContractMain
{
    private readonly ILogger<APIContractMain> _logger;

    public APIContractMain(ILogger<APIContractMain> log)
    {
        _logger = log;
    }





    [FunctionName("deleteUser")]
    [OpenApiOperation(operationId: "DeleteUser", tags: new[] {"user"}, Summary = "Delete user", Description = "Delete user by id")]
    public async Task<IActionResult> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "deleteUser/{id}")] HttpRequest req)
    {
        return new OkObjectResult("");
    }





    [FunctionName("getCards")]
    [OpenApiOperation(operationId: "GetCards", tags: new[] {"cards"}, Summary = "Gets a list of all the cards",
        Description = "Gets a list of all the cards that are currently in the game")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardListDto),
        Description = "The OK response")]
    public async Task<IActionResult> GetCards([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
    {
        CardListDto cardList = new()
        {
            Cards = new CardDto[4]
        };

        cardList.Cards[0] = new CardDto {CardName = "card1", CardBody = "card1 body", CardNumber = 0, Type = CardTypeEnum.OpenAnswer};
        cardList.Cards[1] = new CardDto
            {CardName = "card2", CardBody = "card2 body", CardNumber = 1, Type = CardTypeEnum.MultipleChoice};
        cardList.Cards[2] = new CardDto {CardName = "card2", CardBody = "card2 body", CardNumber = 2, Type = CardTypeEnum.OpenAnswer};
        cardList.Cards[3] = new CardDto {CardName = "card3", CardBody = "card3 body", CardNumber = 3, Type = CardTypeEnum.OpenAnswer};


        return new OkObjectResult(cardList);
    }

    [FunctionName("getCard")]
    [OpenApiOperation(operationId: "GetCard", tags: new[] {"cards"}, Summary = "Get a single card",
        Description = "Gets a single card based on the card ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "CardDTO")]
    public async Task<IActionResult> GetCard([HttpTrigger(AuthorizationLevel.Function, "get", Route = "getCard/{id}")] HttpRequest req,
        string id)
    {
        int.TryParse(id, out int cardId);
        CardDto card = new()
        {
            Id = Guid.NewGuid(), CardName = $"card {id}", CardBody = $"card {id} body", CardNumber = cardId,
            Type = CardTypeEnum.OpenAnswer
        };
        return new OkObjectResult(card);
    }

    [FunctionName("postCard")]
    [OpenApiOperation(operationId: "PostCard", tags: new[] {"cards"}, Summary = "Creates a card",
        Description = "Creates a card based on the data given")]
    [OpenApiRequestBody("application/json", typeof(CardDto), Required = true)]
    public async Task<IActionResult> PostCard([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("putCard")]
    [OpenApiOperation(operationId: "PutCard", tags: new[] {"cards"}, Summary = "Updates a card",
        Description = "Updates a card based on the card ID with the given data")]
    [OpenApiRequestBody("application/json", typeof(CardDto), Required = true)]
    public async Task<IActionResult> PutCard([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var card = JsonConvert.DeserializeObject<CardDto>(requestBody);
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
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LobbyResponseDto),
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
    [OpenApiRequestBody("application/json", typeof(GameSettingsDto), Required = true)]
    public async Task<IActionResult> Settings([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("addUser")]
    [OpenApiOperation(operationId: "AddUser", tags: new[] {"game"}, Summary = "Invites user to room",
        Description = "Invites user to room by ID")]
    [OpenApiRequestBody("application/json", typeof(UserDto), Required = true)]
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
    [OpenApiRequestBody("application/json", typeof(CardResponseDto), Required = true)]
    public async Task<IActionResult> SubmitResponse([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("joinGame")]
    [OpenApiOperation(operationId: "JoinGame", tags: new[] {"game"}, Summary = "Get room data",
        Description = "Access game data by room code")]
    [OpenApiRequestBody("application/json", typeof(JoinRequestDto), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LobbyResponseDto),
        Description = "The OK response")]
    public async Task<IActionResult> JoinGame([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("addEmoji")]
    [OpenApiOperation(operationId: "AddEmoji", tags: new[] {"game"}, Summary = "Add emoji to response",
        Description = "React to a response with an emoji")]
    [OpenApiRequestBody("application/json", typeof(EmojiDto), Required = true)]
    public async Task<IActionResult> AddEmoji([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("matchHistory")]
    [OpenApiOperation(operationId: "MatchHistory", tags: new[] {"game"}, Summary = "Get user history",
        Description = "Get the game history of a user by ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(HistoryDto),
        Description = "The OK response")]
    public async Task<IActionResult> MatchHistory(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "matchHistory/{id}")] HttpRequest req)
    {
        return new OkObjectResult("");
    }

    [FunctionName("chatMessage")]
    [OpenApiOperation(operationId: "ChatMessage", tags: new[] {"game"}, Summary = "Send a message",
        Description = "Send a message to rest of the participants")]
    [OpenApiRequestBody("application/json", typeof(ChatMessageDto), Required = true)]
    public async Task<IActionResult> ChatMessage([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req)
    {
        return new OkObjectResult("");
    }
}
