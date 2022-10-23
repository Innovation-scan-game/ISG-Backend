using System.Net;
using AutoMapper;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace IsolatedFunctions.Controllers;

public class CardController
{
    private readonly InnovationGameDbContext _context;
    private readonly IMapper _mapper;

    public CardController(InnovationGameDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [Function(nameof(GetAllCards))]
    [OpenApiOperation(operationId: "GetCards", tags: new[] {"cards"}, Summary = "Gets a list of all the cards",
         Description = "Gets a list of all the cards that are currently in the game")]
     [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardListDto),
         Description = "The OK response")]
    public async Task<HttpResponseData> GetAllCards([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cards")] HttpRequestData req)
    {
        List<Card> cards = _context.Cards.ToList();

        List<CardDto> cardDtOs = _mapper.Map<List<CardDto>>(cards);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(cardDtOs);
        return response;
    }

    [Function(nameof(GetCardById))]
    [OpenApiOperation(operationId: "GetCardId", tags: new[] {"cards"}, Summary = "Gets a card by ID",
        Description = "Gets a single card that is currently in the game")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "The OK response")]
    public async Task<HttpResponseData> GetCardById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getCard/{id}")]
        HttpRequestData req, string id)
    {
        Card? card = _context.Cards.FirstOrDefault(c => c.Id == Guid.Parse(id));

        if (card == null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Card not found.");
        }

        return await req.CreateSuccessResponse(_mapper.Map<CardDto>(card));
    }

    [Function(nameof(DeleteCard))]
    [OpenApiOperation(operationId: "DeleteCard", tags: new[] {"cards"}, Summary = "Deletes a card by ID",
        Description = "Deletes a single card")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "The OK response")]
    public async Task<HttpResponseData> DeleteCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cards/{id}")]
        HttpRequestData req,
        FunctionContext executionContext, string id)
    {
        var response = req.CreateResponse();

        if (!executionContext.IsAdmin())
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return response;
        }

        if (!Guid.TryParse(id, out Guid cardId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteStringAsync("Invalid card id");
            return response;
        }

        Card? card = _context.Cards.FirstOrDefault(c => c.Id == cardId);
        if (card == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            await response.WriteStringAsync("Card not found");
            return response;
        }

        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
        response.StatusCode = HttpStatusCode.OK;
        await response.WriteStringAsync($"Card '{card.Name}' deleted");
        return response;
    }

    [Function(nameof(CreateCard))]
    [OpenApiOperation(operationId: "CreateCard", tags: new[] {"cards"}, Summary = "Creates a new card",
        Description = "Create a single card")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "The OK response")]
    public async Task<HttpResponseData> CreateCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cards/create")]
        HttpRequestData req, FunctionContext executionContext)

    {
        var response = req.CreateResponse();
        if (req.Body.Length == 0)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return response;
        }


        CreateCardDto? cardDto = await req.ReadFromJsonAsync<CreateCardDto>();
        if (cardDto == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteStringAsync("Invalid card data");
            return response;
        }

        if (_context.Cards.Any(c => c.Name == cardDto.Name))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteStringAsync("Card with this name already exists");
            return response;
        }

        Card card = _mapper.Map<Card>(cardDto);

        _context.Cards.Add(card);
        await _context.SaveChangesAsync();

        response.StatusCode = HttpStatusCode.Created;
        return response;
    }

    [Function(nameof(EditCard))]
    [OpenApiOperation(operationId: "EditCard", tags: new[] {"cards"}, Summary = "Edits a card by ID",
        Description = "Edits a single card")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CardDto),
        Description = "The OK response")]
    public async Task<HttpResponseData> EditCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cards/edit")]
        HttpRequestData req, FunctionContext executionContext)

    {
        HttpResponseData response = req.CreateResponse();

        if (req.Body.Length == 0)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return response;
        }

        EditCardDto? editCardDto = await req.ReadFromJsonAsync<EditCardDto>();

        Card? dbCard = _context.Cards.FirstOrDefault(c => c.Id == Guid.Parse(editCardDto!.Id));
        if (dbCard == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteStringAsync("Card with this id does not exist");
            return response;
        }

        dbCard.Name = editCardDto!.Name;
        dbCard.Body = editCardDto.Body;

        dbCard.Type = (CardTypeEnum) editCardDto.Type;
        _context.Cards.Update(dbCard);
        await _context.SaveChangesAsync();
        response.StatusCode = HttpStatusCode.OK;
        return response;
    }
}
