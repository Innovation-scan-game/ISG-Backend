using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DAL.Data;

public class InnovationGameDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<GameSession> GameSessions { get; set; } = null!;
    public DbSet<SessionResponse> SessionResponses { get; set; } = null!;

    public InnovationGameDbContext()
    {
    }

    public InnovationGameDbContext(DbContextOptions options) : base(options)
    {
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json")
                .Build();
            var connectionString = configuration.GetConnectionString("SqlConnectionString");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}
