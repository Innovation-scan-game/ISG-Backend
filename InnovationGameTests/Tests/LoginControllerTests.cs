using System.Net;
using Azure.Storage.Blobs;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using IsolatedFunctions.Controllers;
using IsolatedFunctions.DTO.Validators;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Services;

namespace InnovationGameTests.Tests;

public class LoginControllerTests
{
    private InnovationGameDbContext _context = null!;

    private LoginController _loginController = null!;

    private User? _admin;
    private User? _user;


    [SetUp]
    public async Task Setup()
    {
        DbContextOptions<InnovationGameDbContext> options = new DbContextOptionsBuilder<InnovationGameDbContext>()
            .UseInMemoryDatabase(databaseName: "InnovationGameDB")
            .Options;

        _context = new InnovationGameDbContext(options);

        _admin = new User
        {
            Id = Guid.NewGuid(),
            Name = "testAdmin",
            Email = "admin@mail.com",
            Role = UserRoleEnum.Admin,
            Password = BCrypt.Net.BCrypt.HashPassword("password")
        };

        _user = new User
        {
            Id = Guid.NewGuid(),
            Name = "testUser",
            Email = "test@mail.com",
            Role = UserRoleEnum.User,
            Password = BCrypt.Net.BCrypt.HashPassword("userPassword")
        };

        _context.Users.Add(_admin);
        _context.Users.Add(_user);
        await _context.SaveChangesAsync();

        var logFactory = new Mock<ILoggerFactory>();
        var mapper = MockHelpers.CreateMapper();
        var blob = new Mock<BlobServiceClient>();

        var loginLogger = new Mock<ILogger<LoginController>>();


        var tokenService = new TokenService(null!, logFactory.Object.CreateLogger<TokenService>());
        _loginController = new LoginController(tokenService, loginLogger.Object, mapper, new UserService(_context, new UserValidator()));

    }


    [TearDown]
    public async Task TearDown()
    {
        // Resets the content of the database list to only the setup
        _context.Users.RemoveRange(_context.Users.ToList());
        await _context.SaveChangesAsync();
    }

    // TODO: FIX FIRST TEST
    [Test]
    public async Task TestUserLoggingIn()
    {
        LoginRequest loginRequest = new LoginRequest
        {
            Username = _user.Name,
            Password = "userPassword",
        };
        string json = JsonConvert.SerializeObject(loginRequest);

        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);
        // Call the controller endpoint
        var res = await _loginController.Login(req);

        // Assert that the response is OK and that the user has been added to the test db
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task TestUserLoggingInUsingInvalidData()
    {
        //Creates a loginRequest with invalid data
        LoginRequest loginRequest = null;

        string json = JsonConvert.SerializeObject(loginRequest);

        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);
        // Call the controller endpoint
        var res = await _loginController.Login(req);
        // Assert that the response is a Badrequest because the data is invalid
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TestUserLoggingInUsingUnknownName()
    {
        // Create a loginRequest with unknown data
        LoginRequest loginRequest = new LoginRequest
        {
            Username = "UnknownName",
            Password = "UnknownPassword",
        };
        string json = JsonConvert.SerializeObject(loginRequest);

        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);
        // Call the controller endpoint
        var res = await _loginController.Login(req);
        // Assert that the response is a NotFound
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task TestUserLoggingInUsingWrongPassword()
    {
        // Create a loginRequest with an invalid password
        LoginRequest loginRequest = new LoginRequest
        {
            Username = _user.Name,
            Password = "UnknownName",
        };
        string json = JsonConvert.SerializeObject(loginRequest);

        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);
        // Call the controller endpoint
        var res = await _loginController.Login(req);
        // Assert that the response is a Badrequest because the password is invalid
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
