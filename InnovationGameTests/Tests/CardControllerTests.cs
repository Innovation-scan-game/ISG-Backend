using System.Net;
using AutoMapper;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.Controllers;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Security;
using IsolatedFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace InnovationGameTests.Tests;

public class CardControllerTests
{
    private CardController _cardController;
    private InnovationGameDbContext _context;
    private JwtMiddleware _middleware;
    private string? _token;

    [SetUp]
    public async Task GlobalSetup()
    {
        _context = MockHelpers.CreateDbContext();

        User admin = new User
        {
            Name = "admin",
            Email = "admin@mail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = UserRoleEnum.Admin
        };
        _context.Users.Add(admin);
        Card card = new Card
        {
            Id = Guid.NewGuid(),
            Name = "CardName",
            Body = "CardBody",
            Type = CardTypeEnum.OpenAnswer,
        };
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();

        IMapper mapper = MockHelpers.CreateMapper();
        _cardController = new CardController(_context, mapper);
        _token = await MockHelpers.GetLoginToken("admin", "password");

        TokenService tokenService = new TokenService(null, new Mock<ILogger<TokenService>>().Object);
        _middleware = new JwtMiddleware(tokenService, new Mock<ILogger<JwtMiddleware>>().Object);
    }

    [TearDown]
    public async Task TearDown()
    {
        _context.Users.RemoveRange(_context.Users.ToList());
        _context.Cards.RemoveRange(_context.Cards.ToList());
        await _context.SaveChangesAsync();
    }

    [Test]
    public async Task TestCreateCard()
    {
         CreateCardDto createCardDto = new CreateCardDto
        {
            Name = "TestCard2",
            Body = "TestBody2",
            Type = 2,
        };
        
        string json = JsonConvert.SerializeObject(createCardDto);

        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);

        HttpResponseData response = await _cardController.CreateCard(req, req.FunctionContext);


        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(_context.Cards.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task TestCreateCardProvidingInvalidCardData()
    {
        CreateCardDto createCardDto = null;

        string json = JsonConvert.SerializeObject(createCardDto);

        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);

        HttpResponseData response = await _cardController.CreateCard(req, req.FunctionContext);


        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TestCreateCardUsingExistingName()
    {
        CreateCardDto createCardDto = new CreateCardDto
        {
            Name = "CardName",
            Body = "TestBody2",
            Type = 2,
        };

        string json = JsonConvert.SerializeObject(createCardDto);

        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);

        HttpResponseData response = await _cardController.CreateCard(req, req.FunctionContext);


        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TestGetAllCards()
    {
        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        var res = await _cardController.GetAllCards(req);

        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_context.Cards.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task TestGetCardById()
    {
        Card newCard = new Card
        {
            Id = Guid.NewGuid(),
            Name = "TestCard",
            Body = "TestBody",
            Type = CardTypeEnum.OpenAnswer,
        };

        _context.Cards.Add(newCard);
        await _context.SaveChangesAsync();


        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        var res = await _cardController.GetCardById(req, newCard.Id.ToString());

        res.Body.Position = 0;
        CardDto result = JsonConvert.DeserializeObject<CardDto>(await new StreamReader(res.Body).ReadToEndAsync());

        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Assert.That(result.Id, Is.EqualTo(newCard.Id));
        Assert.That(result.CardName, Is.EqualTo(newCard.Name));
        Assert.That(_context.Cards.Count(), Is.EqualTo(2));
    }
    
    [Test]
    public async Task TestGetUserByIDUsingWrongID()
    {
        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        var res = await _cardController.GetCardById(req, Guid.NewGuid().ToString());

        res.Body.Position = 0;
        UserDto result = JsonConvert.DeserializeObject<UserDto>(await new StreamReader(res.Body).ReadToEndAsync());

        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task TestEditCard()
    {
        Card cardToEdit = new Card
        {
            Id = Guid.NewGuid(),
            Name = "CardNameToEdit",
            Body = "CardBodyToEdit",
            Type = CardTypeEnum.OpenAnswer,
        };
        _context.Cards.Add(cardToEdit);
        await _context.SaveChangesAsync();

        EditCardDto editCardDto = new EditCardDto
        {
            Id = cardToEdit.Id.ToString(),
            Name = "CardNameChanged",
            Body = "CardBodyChanged",
            Type = 1,
        };

        string json = JsonConvert.SerializeObject(editCardDto);
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");

        async Task Next(FunctionContext context)
        {
            HttpResponseData response = await _cardController!.EditCard(req, req.FunctionContext);

            Card? editedCard = await _context.Cards.FindAsync(cardToEdit.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editedCard?.Name, Is.EqualTo("CardNameChanged"));
            Assert.That(editedCard?.Body, Is.EqualTo("CardBodyChanged"));
            Assert.That(editedCard?.Type, Is.EqualTo(CardTypeEnum.Scale));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestEditCardUsingUnknownID()
    {
        EditCardDto editCardDto = new EditCardDto
        {
            Id = Guid.NewGuid().ToString(),
            Name = "CardNameChanged",
            Body = "CardBodyChanged",
            Type = 1,
        };

        string json = JsonConvert.SerializeObject(editCardDto);
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");

        async Task Next(FunctionContext context)
        {
            HttpResponseData response = await _cardController!.EditCard(req, req.FunctionContext);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestDeleteCard()
    {
        // Setup for this specific test; Create a card that will then be deleted
        Card cardToDelete = new Card
        {
            Id = Guid.NewGuid(),
            Name = "TestCard",
            Body = "TestBody",
            Type = CardTypeEnum.OpenAnswer,
        };
        // card is added to our test db
        _context.Cards.Add(cardToDelete);
        await _context.SaveChangesAsync();

        // Forge a request containing an auth auth token
        HttpRequestData req = MockHelpers.CreateHttpRequestData(token: _token);

        // Invoke middleware to be able to validate token
        await _middleware.Invoke(req.FunctionContext, async (ctx) =>
        {
            // Assert that the cardToDelete has been added to our test db
            Assert.That(_context.Cards.Count(), Is.EqualTo(2));
            // Call the controller endpoint
            HttpResponseData res = await _cardController.DeleteCard(req, ctx, cardToDelete.Id.ToString());

            // Assert that the response is OK and that the card is deleted.
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_context.Cards.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task TestDeleteCardUsingUnknownCardID()
    {
        // Forge a request containing an auth auth token
        HttpRequestData req = MockHelpers.CreateHttpRequestData(token: _token);

        // Invoke middleware to be able to validate token
        await _middleware.Invoke(req.FunctionContext, async (ctx) =>
        {
            // Call the controller endpoint with new Guid ID
            HttpResponseData res = await _cardController.DeleteCard(req, ctx, Guid.NewGuid().ToString());

            // Assert that the response is NotFound.
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(_context.Cards.Count(), Is.EqualTo(1));
        });
    }

    // test delete card by user who is not admin
}
