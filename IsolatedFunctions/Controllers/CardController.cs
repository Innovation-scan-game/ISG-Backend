using System.Net;
using System.Security.Claims;
using AutoMapper;
using Domain.Enums;
using Domain.Models;
using HttpMultipartParser;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Services;
using Services.Interfaces;

namespace IsolatedFunctions.Controllers;

public class CardController
{
    private readonly IMapper _mapper;

    private ICardService CardService { get; }
    private IUserService UserService { get; }
    public IImageUploadService ImageUploadService { get; }

    public CardController(IMapper mapper, ICardService cardService, IUserService userService, IImageUploadService imageUploadService)
    {
        _mapper = mapper;
        UserService = userService;
        CardService = cardService;
        ImageUploadService = imageUploadService;
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cards/{id}")]
        HttpRequestData req, Guid id)
    {
        Card? card = await CardService.GetCardById(id);

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
        FunctionContext executionContext, Guid id)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);
        if (dbUser is null || dbUser.Role != UserRoleEnum.Admin)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized);
        }


        Card? card = await CardService.GetCardById(id);
        if (card is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.NotFound, "Card not found!");
        }

        await CardService.RemoveCard(card);

        return await req.CreateSuccessResponse(HttpStatusCode.OK);
    }


    [Function(nameof(AddCardImage))]
    public async Task<HttpResponseData> AddCardImage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cards/{id}/image")]
        HttpRequestData req, FunctionContext executionContext, Guid id)
    {
        ClaimsPrincipal? user = executionContext.GetUser();
        User? dbUser = await UserService.GetUserByName(user?.Identity?.Name!);
        if (dbUser is null || dbUser.Role != UserRoleEnum.Admin)
        {
            return await req.CreateErrorResponse(HttpStatusCode.Unauthorized);
        }

        if (req.Body == Stream.Null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.NoContent);
        }

        Card card = await CardService.GetCardById(id);

        MultipartFormDataParser? body = await MultipartFormDataParser.ParseAsync(req.Body);
        FilePart? file = body.Files?.ToList().FirstOrDefault();

        if (file is not null)
        {
            card.Picture = await ImageUploadService.UploadImage(file, Enums.BlobContainerName.CardImages);
        }

        try
        {
            await CardService.UpdateCard(card);
            return req.CreateResponse(HttpStatusCode.Created);
        }
        catch (Exception e)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, e.Message);
        }
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

        CreateCardDto? cardDto = await req.ReadFromJsonAsync<CreateCardDto>();

        if (cardDto is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid card data!");
        }


        Card card = _mapper.Map<Card>(cardDto);

        try
        {
            await CardService.AddCard(card);
            return req.CreateResponse(HttpStatusCode.Created);
        }
        catch (Exception e)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, e.Message);
        }
    }


    [Function(nameof(EditCard))]
    [OpenApiOperation(operationId: "EditCard", tags: new[] {"cards"}, Summary = "Edit an existing card",
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

        EditCardDto? editCardDto = await req.ReadFromJsonAsync<EditCardDto>();


        if (editCardDto is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid input!");
        }

        Card updatedCard = _mapper.Map<Card>(editCardDto);

        try
        {
            await CardService.UpdateCard(updatedCard);
            return await req.CreateSuccessResponse(HttpStatusCode.OK);
        }
        catch (Exception e)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, e.Message);
        }
    }
}
