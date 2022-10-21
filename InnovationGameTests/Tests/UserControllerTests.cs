using System.Net;
using Azure.Storage.Blobs;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using InnovationGameTests.DTOs;
using IsolatedFunctions.Controllers;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Security;
using IsolatedFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace InnovationGameTests.Tests;

public class UserControllerTests
{
    private InnovationGameDbContext? _context;

    private LoginController? _loginController;
    private UserController? _userController;

    private User? _admin;
    private User? _user;

    private string? _token;
    private JwtMiddleware? _middleware;

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
        _context.Users.Add(_admin);
        await _context.SaveChangesAsync();

        var logFactory = new Mock<ILoggerFactory>();
        var mapper = MockHelpers.CreateMapper();
        var blob = new Mock<BlobServiceClient>();

        var loginLogger = new Mock<ILogger<LoginController>>();

        var jwtLogger = new Mock<ILogger<JwtMiddleware>>();

        _userController = new UserController(logFactory.Object, _context, mapper, blob.Object);

        var tokenService = new TokenService(null, logFactory.Object.CreateLogger<TokenService>());
        _loginController = new LoginController(tokenService, loginLogger.Object, _context, mapper);

        _middleware = new JwtMiddleware(tokenService, jwtLogger.Object);


        _token = await GetLoginToken(_admin.Name, "password");
    }


    [TearDown]
    public async Task TearDown()
    {
        _context.Users.RemoveRange(_context.Users.ToList());
        await _context.SaveChangesAsync();
    }

    [Test]
    public async Task TestCreateUser()
    {
        CreateUserDto createUserDto = new CreateUserDto
        {
            Username = "testUser",
            Password = "testPassword",
            Email = "test@mail.com"
        };
        string json = JsonConvert.SerializeObject(createUserDto);

        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);

        HttpResponseData response = await _userController.CreateUser(req, req.FunctionContext);


        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_context.Users.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task TestGetAllUsers()
    {
        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        HttpResponseData response = await _userController.GetAllUsers(req);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Assert.That(_context.Users.Count(), Is.EqualTo(2));
    }

    private async Task<string> GetLoginToken(string username, string password)
    {
        LoginRequest loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };
        string json = JsonConvert.SerializeObject(loginRequest);

        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);

        var res = await _loginController.Login(req);

        res.Body.Position = 0;
        LoginResultDto result = JsonConvert.DeserializeObject<LoginResultDto>(await new StreamReader(res.Body).ReadToEndAsync());
        return result.AccessToken;
    }

    [Test]
    public async Task TestUpdateOwnUser()
    {
        EditUserDto editUserDto = new EditUserDto
        {
            Email = "test@mail.com"
        };
        string json = JsonConvert.SerializeObject(editUserDto);
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");


        async Task Next(FunctionContext context)
        {
            var res = await _userController!.UpdateUser(req, req.FunctionContext);

            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((await _context!.Users.FindAsync(_admin.Id)).Email, Is.EqualTo("test@mail.com"));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestUpdateOtherUser()
    {
        User userToEdit = new User
        {
            Id = Guid.NewGuid(),
            Name = "editUser",
            Email = "edit@mail.com",
            Role = UserRoleEnum.User,
            Password = "pw"
        };
        _context.Users.Add(userToEdit);
        await _context.SaveChangesAsync();

        EditUserDto editUserDto = new EditUserDto
        {
            Id = userToEdit.Id.ToString(),
            Email = "changed@mail.com",
            Username = "changedUsername",
            Password = "changedPassword"
        };

        string json = JsonConvert.SerializeObject(editUserDto);
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");

        async Task Next(FunctionContext context)
        {
            HttpResponseData response = await _userController!.UpdateUser(req, req.FunctionContext);

            User? editedUser = await _context.Users.FindAsync(userToEdit.Id);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editedUser?.Email, Is.EqualTo("changed@mail.com"));
            Assert.That(editedUser?.Name, Is.EqualTo("changedUsername"));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }


    [Test]
    public async Task TestDeleteUser()
    {
        User userToDelete = new User
        {
            Id = Guid.NewGuid(),
            Name = "deleteUser",
            Email = "edit@mail.com",
            Role = UserRoleEnum.User,
            Password = "pw"
        };
        _context!.Users.Add(userToDelete);
        await _context.SaveChangesAsync();

        HttpRequestData req = MockHelpers.CreateHttpRequestData(token: _token, method: "delete");

        async Task Next(FunctionContext context)
        {
            Assert.That(_context.Users.Count(), Is.EqualTo(2));

            HttpResponseData response = await _userController.DeleteUser(req, req.FunctionContext, userToDelete.Id.ToString());
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_context.Users.Count(), Is.EqualTo(1));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestDeleteUserNotAdmin()
    {
        User userToDelete = new User
        {
            Id = Guid.NewGuid(),
            Name = "deleteUser",
            Email = "edit@mail.com",
            Role = UserRoleEnum.User,
            Password = "pw"
        };
        _context!.Users.Add(userToDelete);
        await _context.SaveChangesAsync();

        string userToken = await GetLoginToken("testUser", "userPassword");

        HttpRequestData req = MockHelpers.CreateHttpRequestData(token: userToken, method: "delete");

        async Task Next(FunctionContext context)
        {
            HttpResponseData response = await _userController.DeleteUser(req, req.FunctionContext, userToDelete.Id.ToString());
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(_context.Users.Count(), Is.EqualTo(2));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }
}
