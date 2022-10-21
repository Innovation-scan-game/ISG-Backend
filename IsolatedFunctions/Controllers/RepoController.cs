using Domain.Models;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;

namespace IsolatedFunctions.Controllers;

public class RepoController
{
    // private readonly UserRepository _userRepo;
    private Repository<User> _userRepo;

    public RepoController(DbContext context)
    {
        _userRepo = new Repository<User>(context);
        Console.WriteLine("ctor");
        // _userRepo = userRepo;
    }

    [Function(nameof(TestRepo))]
    public async Task TestRepo([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test/repo")] HttpRequestData req)
    {
        Console.WriteLine("dbg");

        var all = _userRepo.GetAll();
        // var spec = _userRepo.Find(u => u.Name == "TestUser");
        //
        // var test = _userRepo.GetAll();
        Console.WriteLine("test");
    }
}
