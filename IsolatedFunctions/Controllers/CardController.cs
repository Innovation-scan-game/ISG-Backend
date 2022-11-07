using System.Net;
using AutoMapper;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.Extensions;
using IsolatedFunctions.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace IsolatedFunctions.Controllers;

public class CardController
{
    private readonly IMapper _mapper;

    private ICardService CardService { get; }

    public CardController(IMapper mapper, ICardService cardService)
    {
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
        return await req.CreateSuccessResponse(_mapper.Map<CardDto>(cards));
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
        

        if (!executionContext.IsAdmin())
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized);
        }

        if (!Guid.TryParse(id, out Guid cardId))
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest,"Invalid card id!");
        }

        Card? card = await CardService.GetCardById(cardId);
        if (card == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.NotFound,"Card not found!");
        }

        await CardService.RemoveCard(card);
        
        return await req.CreateSuccessResponse(HttpStatusCode.OK);
        //await response.WriteStringAsync($"Card '{card.Name}' deleted");
        
    }

    [Function(nameof(CreateCard))]
    [OpenApiOperation(operationId: "CreateCard", tags: new[] {"cards"}, Summary = "Creates a new card",
        Description = "Create a single card")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "The created card")]
    public async Task<HttpResponseData> CreateCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cards/create")]
        HttpRequestData req, FunctionContext executionContext)

    {
        if (!executionContext.IsAdmin())
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cards/edit")]
        HttpRequestData req, FunctionContext executionContext)

    {
        if (!executionContext.IsAdmin())
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized);
        }

        if (req.Body.Length == 0)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest,"No input!");
        }

        EditCardDto? editCardDto = await req.ReadFromJsonAsync<EditCardDto>();
        if (editCardDto is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest,"Invalid input!");
        }

        Card? dbCard = await CardService.GetCardById(Guid.Parse(editCardDto!.Id));

        if (dbCard is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest,"Card with this ID does not exist!");
        }

        dbCard.Name = editCardDto.Name;
        dbCard.Body = editCardDto.Body;
        dbCard.Type = (CardTypeEnum) editCardDto.Type;
        
        await CardService.UpdateCard(dbCard);
        return await req.CreateSuccessResponse(HttpStatusCode.OK);
        
    }
}
