using System.Net;
using AutoMapper;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.Controllers;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.Security;
using IsolatedFunctions.Services;
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

    [OneTimeSetUp]
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
        await _context.SaveChangesAsync();

        IMapper mapper = MockHelpers.CreateMapper();
        _cardController = new CardController(_context, mapper);
        _token = await MockHelpers.GetLoginToken("admin", "password");

        TokenService tokenService = new TokenService(null, new Mock<ILogger<TokenService>>().Object);
        _middleware = new JwtMiddleware(tokenService, new Mock<ILogger<JwtMiddleware>>().Object);
    }

    [OneTimeTearDown]
    public void GlobalTearDown()
    {
        _context.Users.RemoveRange(_context.Users);
    }

    [Test]
    public async Task TestGetAllCards()
    {
        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        var res = await _cardController.GetAllCards(req);

        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
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
            // Call the controller endpoint
            HttpResponseData res = await _cardController.DeleteCard(req, ctx, cardToDelete.Id.ToString());

            // Assert that the response is OK and that the user is deleted.
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_context.Cards.Count(), Is.EqualTo(0));
        });
    }
}
