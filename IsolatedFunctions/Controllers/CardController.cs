using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Services.Interfaces;

namespace IsolatedFunctions.Controllers;

public class CardController
{
    private readonly IMapper _mapper;

    private ICardService CardService { get; }
    private IUserService UserService { get; }

    public CardController(IMapper mapper, ICardService cardService, IUserService userService)
    {
        UserService = userService;
        CardService = cardService;
        _mapper = mapper;
    }

    /// <summary>
    ///     Gets a list of all the cards that are currently in the game.
    /// </summary>
    [Function(nameof(GetAllCards))]
    [OpenApiOperation(operationId: "GetCards", tags: new[] {"cards"}, Summary = "Gets a list of all the cards",
        Description = "Gets a list of all the cards that are currently in the game")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardListDto),
        Description = "All Cards")]
    public async Task<HttpResponseData> GetAllCards([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cards")] HttpRequestData req)
    {
        List<Card> cards = await CardService.GetAllCards();
        return await req.CreateSuccessResponse(cards.Select(c => _mapper.Map<CardDto>(c)).ToList());
    }

    [Function(nameof(GetCardById))]
    [OpenApiOperation(operationId: "GetCardId", tags: new[] {"cards"}, Summary = "Gets a card by ID",
        Description = "Gets a single card that is currently in the game")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "The Requested Card")]
    public async Task<HttpResponseData> GetCardById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getCard/{id}")]
        HttpRequestData req, string id)
    {
        Card? card = await CardService.GetCardById(Guid.Parse(id));

        if (card == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Card not found!");
        }

        return await req.CreateSuccessResponse(_mapper.Map<CardDto>(card));
    }

    [Function(nameof(DeleteCard))]
    [OpenApiOperation(operationId: "DeleteCard", tags: new[] {"cards"}, Summary = "Deletes a card by ID",
        Description = "Deletes a single card")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "Deleted Card")]
    public async Task<HttpResponseData> DeleteCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cards/{id}")]
        HttpRequestData req,
        FunctionContext executionContext, string id)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);
        if (dbUser is null || dbUser.Role != UserRoleEnum.Admin)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized);
        }

        if (!Guid.TryParse(id, out Guid cardId))
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid card id!");
        }

        Card? card = await CardService.GetCardById(cardId);
        if (card == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Card not found!");
        }

        await CardService.RemoveCard(card);

        return await req.CreateSuccessResponse(HttpStatusCode.OK);
    }

    [Function(nameof(CreateCard))]
    [OpenApiOperation(operationId: "CreateCard", tags: new[] {"cards"}, Summary = "Creates a new card",
        Description = "Create a single card")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "The created card")]
    public async Task<HttpResponseData> CreateCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cards")]
        HttpRequestData req, FunctionContext executionContext)

    {
        ClaimsPrincipal? user = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);
        if (dbUser is null || dbUser.Role != UserRoleEnum.Admin)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized);
        }

        if (req.Body.Length == 0)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest);
        }

        CreateCardDto? cardDto = await req.ReadFromJsonAsync<CreateCardDto>();
        if (cardDto == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid card data!");
        }

        if (await CardService.CardExists(cardDto.Name))
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Card with this name already exists!");
        }

        return await req.CreateSuccessResponse(_mapper.Map<CardDto>(cardDto));
    }

    [Function(nameof(EditCard))]
    [OpenApiOperation(operationId: "EditCard", tags: new[] {"cards"}, Summary = "Edits a card by ID",
        Description = "Edits a single card")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "The edited card")]
    public async Task<HttpResponseData> EditCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cards")]
        HttpRequestData req, FunctionContext executionContext)

    {
        ClaimsPrincipal? principal = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(principal?.Identity?.Name!);
        if (dbUser is null || dbUser.Role != UserRoleEnum.Admin)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not authorized to edit cards!");
        }

        if (req.Body.Length == 0)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "No input!");
        }

        EditCardDto? editCardDto = await req.ReadFromJsonAsync<EditCardDto>();
        if (editCardDto is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid input!");
        }

        Card? dbCard = await CardService.GetCardById(Guid.Parse(editCardDto.Id));

        if (dbCard is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Card with this ID does not exist!");
        }

        dbCard.Name = editCardDto.Name;
        dbCard.Body = editCardDto.Body;
        dbCard.Type = (CardTypeEnum) editCardDto.Type;

        await CardService.UpdateCard(dbCard);
        return await req.CreateSuccessResponse(HttpStatusCode.OK);
    }
}
