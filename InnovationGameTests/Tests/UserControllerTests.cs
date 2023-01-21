using System.Net;
using AutoMapper;
using Azure.Storage.Blobs;
using DAL.Data;
using Domain.Enums;
using Domain.Models;
using InnovationGameTests.DTOs;
using IsolatedFunctions.Controllers;
using IsolatedFunctions.DTO.UserDTOs;
using IsolatedFunctions.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Services;

namespace InnovationGameTests.Tests;

public class UserControllerTests
{
    private InnovationGameDbContext _context = null!;
    private ImageUploadService _imageUploadService = null!;
    private LoginController _loginController = null!;
    private UserController _userController = null!;
    private User _admin = null!;
    private User _user = null!;

    private string? _token;
    private JwtMiddleware _middleware = null!;

    [SetUp]
    public async Task Setup()
    {
        DbContextOptions<InnovationGameDbContext> options = new DbContextOptionsBuilder<InnovationGameDbContext>()
            .UseInMemoryDatabase(databaseName: "InnovationGameDB")
            .Options;

        _context = new InnovationGameDbContext(options);
        _imageUploadService = new ImageUploadService(new Mock<BlobServiceClient>().Object);

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

        Mock<ILoggerFactory> logFactory = new Mock<ILoggerFactory>();
        IMapper mapper = MockHelpers.CreateMapper();
        Mock<BlobServiceClient> blob = new Mock<BlobServiceClient>();

        Mock<ILogger<LoginController>> loginLogger = new Mock<ILogger<LoginController>>();
        Mock<ILogger<JwtMiddleware>> jwtLogger = new Mock<ILogger<JwtMiddleware>>();

        _userController = new UserController(logFactory.Object, new UserService(_context), mapper, _imageUploadService);

        TokenService tokenService = new TokenService(null!, logFactory.Object.CreateLogger<TokenService>());
        _loginController = new LoginController(tokenService, loginLogger.Object, mapper, new UserService(_context));

        _middleware = new JwtMiddleware(tokenService, jwtLogger.Object);


        _token = await GetLoginToken(_admin.Name, "password");
    }


    [TearDown]
    public async Task TearDown()
    {
        // Resets the content of the database list to only the setup
        _context.Users.RemoveRange(_context.Users.ToList());
        await _context.SaveChangesAsync();
    }

    [Test]
    public async Task TestGetAllUsers()
    {
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(token: _token);
        // Call the controller endpoint
        // Assert that the response is OK and that the users are retrieved


        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            HttpResponseData response = await _userController.GetAllUsers(req, req.FunctionContext);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_context.Users.Count(), Is.EqualTo(2));
        }

        await _middleware.Invoke(req.FunctionContext, Next);
    }


    [Test]
    public async Task TestCreateUser()
    {
        // Create new user using CreateUserDto
        CreateUserDto createUserDto = new CreateUserDto
        {
            Username = "testUser2",
            Password = "testPassword2",
            Email = "test2@mail.com"
        };
        string json = JsonConvert.SerializeObject(createUserDto);
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);
        // Call the controller endpoint
        HttpResponseData response = await _userController.CreateUser(req, req.FunctionContext);
        // Assert that the response is OK and that the user has been added to the test db
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_context.Users.Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task TestCreateUserUsingAlreadyExistingName()
    {
        // Create new user with a name that already exists
        CreateUserDto createUserDto = new CreateUserDto
        {
            Username = "testUser",
            Password = "testPassword2",
            Email = "test@mail.com"
        };
        string json = JsonConvert.SerializeObject(createUserDto);
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json);
        // Call the controller endpoint
        HttpResponseData response = await _userController.CreateUser(req, req.FunctionContext);
        // Assert that the response is a Badrequest, because the name is already taken
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TestGetUserByID()
    {
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        // Call the controller endpoint using the ID of the user created during the setup
        var res = await _userController.GetUserById(req, _user.Id.ToString());
        // Retrieve the suer
        res.Body.Position = 0;
        UserDto result = JsonConvert.DeserializeObject<UserDto>(await new StreamReader(res.Body).ReadToEndAsync());
        // Assert that the response is OK and that the user has been retrieved
        Assert.That(result, Is.Not.Null);
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(result.Username, Is.EqualTo(_user.Name));
        Assert.That(result.Email, Is.EqualTo(_user.Email));
    }

    [Test]
    public async Task TestGetUserByIDUsingWrongID()
    {
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData();
        // Call the controller endpoint using an unknown user Guid
        var res = await _userController.GetUserById(req, Guid.NewGuid().ToString());
        // Retrieve user
        res.Body.Position = 0;
        UserDto result = JsonConvert.DeserializeObject<UserDto>(await new StreamReader(res.Body).ReadToEndAsync());
        // Assert that the response is NotFound
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
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
        // Create a EditUserDto so that a user can edit their own Email
        EditUserDto editUserDto = new EditUserDto
        {
            Email = "testnew@mail.com"
        };
        string json = JsonConvert.SerializeObject(editUserDto);
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");


        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            var res = await _userController!.UpdateUser(req, req.FunctionContext);
            // Assert that the response is OK and that the testuser' email equals to the new email
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((await _context.Users.FindAsync(_admin.Id))?.Email, Is.EqualTo("testnew@mail.com"));
        }

        await _middleware.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestUpdateOwnUserChangingNameToAlreadyExistingName()
    {
        // Create a EditUserDto to change the email to an address that already exists
        EditUserDto editUserDto = new EditUserDto
        {
            Email = "test@mail.com"
        };
        string json = JsonConvert.SerializeObject(editUserDto);
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");


        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            var res = await _userController!.UpdateUser(req, req.FunctionContext);
            // Assert that the response is a BadRequest, because the new email already exists
            Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestUpdateOtherUser()
    {
        // Create user to edit and add to the test db
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
        // Create EditUserDto to change the data of a user
        EditUserDto editUserDto = new EditUserDto
        {
            Id = userToEdit.Id.ToString(),
            Email = "changed@mail.com",
            Username = "changedUsername",
            Password = "changedPassword"
        };

        string json = JsonConvert.SerializeObject(editUserDto);
        // Forge a request containing an auth token
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");

        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            HttpResponseData response = await _userController!.UpdateUser(req, req.FunctionContext);
            // Retrieve the editedUser if possible
            User? editedUser = await _context.Users.FindAsync(userToEdit.Id);
            // Assert that the response is OK and that the user has been updated
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editedUser?.Email, Is.EqualTo("changed@mail.com"));
            Assert.That(editedUser?.Name, Is.EqualTo("changedUsername"));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestUpdateOtherUserChangingNameToAlreadyExistingName()
    {
        // Create user to edit and add to the test db
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
        // Create EditUserDto to change the data of a user to an already existing name
        EditUserDto editUserDto = new EditUserDto
        {
            Id = userToEdit.Id.ToString(),
            Email = "test@mail.com",
            Username = "testUser",
            Password = "newPassword"
        };

        string json = JsonConvert.SerializeObject(editUserDto);
        // Forge a request containing an auth token
        HttpRequestData req = MockHelpers.CreateHttpRequestData(json, token: _token, method: "put");

        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint
            HttpResponseData response = await _userController!.UpdateUser(req, req.FunctionContext);
            // Retrieve the editedUser if possible
            User? editedUser = await _context.Users.FindAsync(userToEdit.Id);
            // Assert that the response is a BadRequest, because the name was already taken
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestDeleteUser()
    {
        // Create a user and add to test db
        User userToDelete = new User
        {
            Id = Guid.NewGuid(),
            Name = "deleteUser",
            Email = "edit@mail.com",
            Role = UserRoleEnum.User,
            Password = "pw"
        };
        _context.Users.Add(userToDelete);
        await _context.SaveChangesAsync();
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(token: _token, method: "delete");

        async Task Next(FunctionContext context)
        {
            // Assert that the user has been added to the test db
            Assert.That(_context.Users.Count(), Is.EqualTo(3));
            // Call the controller endpoint with the userToDelete ID
            HttpResponseData response = await _userController.DeleteUser(req, req.FunctionContext, userToDelete.Id.ToString());
            // Assert that the response is OK and that the user has been deleted
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_context.Users.Count(), Is.EqualTo(2));
        }

        await _middleware.Invoke(req.FunctionContext, Next);
    }

    [Test]
    public async Task TestDeleteUserNotAdmin()
    {
        // Create a user and add to the test db
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
        // Forge a request
        HttpRequestData req = MockHelpers.CreateHttpRequestData(token: userToken, method: "delete");

        async Task Next(FunctionContext context)
        {
            // Call the controller endpoint using userToDelte ID
            HttpResponseData response = await _userController.DeleteUser(req, req.FunctionContext, userToDelete.Id.ToString());
            // Assert that the response is Unauthorized, because no admin was logged in
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(_context.Users.Count(), Is.EqualTo(3));
        }

        await _middleware!.Invoke(req.FunctionContext, Next);
    }
}
