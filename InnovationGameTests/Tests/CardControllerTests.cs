﻿using System.Net;
using AutoMapper;
using Azure.Storage.Blobs;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.Controllers;
using IsolatedFunctions.DTO.CardDTOs;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.DTO.Validators;
using IsolatedFunctions.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Services;

namespace InnovationGameTests.Tests;

public class CardControllerTests
{
    private CardController _cardController = null!;
    private InnovationGameDbContext _context = null!;
    private JwtMiddleware _middleware = null!;
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
            Type = CardTypeEnum.OpenAnswer
        };
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();

        Mock<BlobServiceClient> blob = new Mock<BlobServiceClient>();

        IMapper mapper = MockHelpers.CreateMapper();
        _cardController = new CardController(mapper, new CardService(_context, new CardValidator() ), new UserService(_context, new UserValidator()), new ImageUploadService(blob.Object));
        _token = await MockHelpers.GetLoginToken("admin", "password");

        TokenService tokenService = new TokenService(null!, new Mock<ILogger<TokenService>>().Object);
        _middleware = new JwtMiddleware(tokenService, new Mock<ILogger<JwtMiddleware>>().Object);
    }

    [TearDown]
    public async Task TearDown()
    {
        // Resets the content of the database lists to only the setup
        _context.Users.RemoveRange(_context.Users.ToList());
        _context.Cards.RemoveRange(_context.Cards.ToList());
        await _context.SaveChangesAsync();
    }

    [Test]
    public async Task TestCreateCard()
    {
        // Setup a card to create
        CreateCardDto createCardDto = new CreateCardDto
        {
            Name = "TestCard2",
            Body = "TestBody2",
            Type = 2
        };

        string json = JsonConvert.SerializeObject(createCardDto);



        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, _token);

        var count = _context.Cards.Count();

        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            HttpResponseData response = await _cardController.CreateCard(req, req.FunctionContext);

            // Assert that the card is created and that the new count equals to 2
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(_context.Cards.Count(), Is.EqualTo(2));
        }

        await _middleware.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestCreateCardProvidingInvalidCardData()
    {
        // Create a card with invalid content 
        CreateCardDto createCardDto = null!;

        string json = JsonConvert.SerializeObject(createCardDto);
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, _token);

        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            HttpResponseData response = await _cardController.CreateCard(req, req.FunctionContext);

            // Assert that response is 400 BadRequest, because the data is invalid
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        await _middleware.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestCreateCardUsingExistingName()
    {
        // Create a card with a name that already exists in the test db
        CreateCardDto createCardDto = new CreateCardDto
        {
            Name = "CardName",
            Body = "TestBody2",
            Type = 2
        };

        string json = JsonConvert.SerializeObject(createCardDto);
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, _token);


        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            HttpResponseData response = await _cardController.CreateCard(req, req.FunctionContext);

            // Assert that response is 400 BadRequest, because the card already exists
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        await _middleware.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestGetAllCards()
    {
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        // Call the controller endpoint
        var res = await _cardController.GetAllCards(req);
        // Assert that the response is OK and that the Cards count is 1
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_context.Cards.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task TestGetCardById()
    {
        // Create new card that will be used to search on ID and add to the test db
        Card newCard = new Card
        {
            Id = Guid.NewGuid(),
            Name = "TestCard",
            Body = "TestBody",
            Type = CardTypeEnum.OpenAnswer,
        };

        _context.Cards.Add(newCard);
        await _context.SaveChangesAsync();

        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        // Call the controller endpoint using the ID of the newly created card
        var res = await _cardController.GetCardById(req, newCard.Id);

        // Retrieve the card
        res.Body.Position = 0;
        CardDto result = JsonConvert.DeserializeObject<CardDto>(await new StreamReader(res.Body).ReadToEndAsync());

        // Assert that the response is OK, that the card has been retrieved and added to the test db
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(result.Id, Is.EqualTo(newCard.Id));
        Assert.That(result.CardName, Is.EqualTo(newCard.Name));
        Assert.That(_context.Cards.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task TestGetUserByIDUsingWrongID()
    {
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        // Call the controller endpoint with unknown ID
        var res = await _cardController.GetCardById(req, Guid.NewGuid());

        // Retrieve the card
        res.Body.Position = 0;
        UserDto result = JsonConvert.DeserializeObject<UserDto>(await new StreamReader(res.Body).ReadToEndAsync());

        // Assert that the card could not be found
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task TestEditCard()
    {
        // Create new card to edit and add it to the test db
        Card cardToEdit = new Card
        {
            Id = Guid.NewGuid(),
            Name = "CardNameToEdit",
            Body = "CardBodyToEdit",
            Type = CardTypeEnum.OpenAnswer,
        };
        _context.Cards.Add(cardToEdit);
        await _context.SaveChangesAsync();

        // Create EditCardDto to create changes to an already existing card
        EditCardDto editCardDto = new EditCardDto
        {
            Id = cardToEdit.Id,
            Name = "CardNameChanged",
            Body = "CardBodyChanged",
            Type = 1,
        };

        string json = JsonConvert.SerializeObject(editCardDto);
        // Forge a request containing an auth token
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");

        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            HttpResponseData response = await _cardController!.EditCard(req, req.FunctionContext);

            // Retrieve the newly edited card if possible
            Card? editedCard = await _context.Cards.FindAsync(cardToEdit.Id);

            // Assert that the response is OK and the card has been edited 
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editedCard?.Name, Is.EqualTo("CardNameChanged"));
            Assert.That(editedCard?.Body, Is.EqualTo("CardBodyChanged"));
            Assert.That(editedCard?.Type, Is.EqualTo(CardTypeEnum.Scale));
        }

        await _middleware.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestEditCardUsingUnknownID()
    {
        // Create EditCardDto to edit a card with an unknown ID
        EditCardDto editCardDto = new EditCardDto
        {
            Id = Guid.NewGuid(),
            Name = "CardNameChanged",
            Body = "CardBodyChanged",
            Type = 1,
        };

        string json = JsonConvert.SerializeObject(editCardDto);
        // Forge a request containing an auth token
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");

        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            HttpResponseData response = await _cardController!.EditCard(req, req.FunctionContext);
            // Assert that the response is a BadRequest, because the ID was unknown
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
            HttpResponseData res = await _cardController.DeleteCard(req, ctx, cardToDelete.Id);

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
            HttpResponseData res = await _cardController.DeleteCard(req, ctx, Guid.NewGuid());

            // Assert that the response is NotFound.
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(_context.Cards.Count(), Is.EqualTo(1));
        });
    }

    // test delete card by user who is not admin
}
