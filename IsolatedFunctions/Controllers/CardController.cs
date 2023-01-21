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
using Newtonsoft.Json;
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
        if (card is null)
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

        if (req.Body == Stream.Null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.NoContent);
        }

        MultipartFormDataParser? body = await MultipartFormDataParser.ParseAsync(req.Body);
        CreateCardDto? cardDto = GetCardDtoFromRequest<CreateCardDto>(body);

        if (cardDto is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid card data!");
        }

        if (await CardService.CardExists(cardDto.Name))
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "A card with this name already exists!");
        }

        Card card = _mapper.Map<Card>(cardDto);

        FilePart? file = body.Files?.ToList().FirstOrDefault();


        if (file is not null)
        {
            if (!IsContentTypeAllowed(file.ContentType))
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid image file type!");
            if (!IsContentSizeAppropriate(file.Data))
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "File size must be between 1 KByte and 5 MB!");
            card.Picture = await ImageUploadService.UploadImage(file, Enums.BlobContainerName.CardImages);
        }

        await CardService.AddCard(card);
        return req.CreateResponse(HttpStatusCode.Created);
    }


    private static T? GetCardDtoFromRequest<T>(IMultipartFormDataParser formData)
    {
        try
        {
            string? json = formData.GetParameterValue("json");
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (ArgumentNullException)
        {
            return default;
        }
    }

    private bool IsContentSizeAppropriate(Stream data)
    {
        return data.Length is > 1024 and < 1024 * 1024 * 5;
    }


    private bool IsContentTypeAllowed(string contentType)
    {
        string[] allowedContent = {"image/png", "image/jpeg"};
        return allowedContent.Contains(contentType);
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

        if (req.Body.Length == 0)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "No input!");
        }

        MultipartFormDataParser? body = await MultipartFormDataParser.ParseAsync(req.Body);
        EditCardDto? editCardDto = GetCardDtoFromRequest<EditCardDto?>(body);


        if (editCardDto is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid input!");
        }

        Card? dbCard = await CardService.GetCardById(Guid.Parse(editCardDto.Id));

        if (dbCard is null)
        {
            return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "A card with this ID does not exist!");
        }

        dbCard.Name = editCardDto.Name;
        dbCard.Body = editCardDto.Body;
        dbCard.Type = (CardTypeEnum) editCardDto.Type;

        FilePart? file = body.Files?.ToList().FirstOrDefault();
        if (file is not null)
        {
            if (!IsContentTypeAllowed(file.ContentType))
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid image file type!");
            if (!IsContentSizeAppropriate(file.Data))
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "File size must be between 1 KByte and 5 MB!");
            dbCard.Picture = await ImageUploadService.UploadImage(file, Enums.BlobContainerName.CardImages);
        }

        await CardService.UpdateCard(dbCard);
        return await req.CreateSuccessResponse(HttpStatusCode.OK);
    }
}