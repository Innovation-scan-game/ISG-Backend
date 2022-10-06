using DAL.Data;

Console.WriteLine("Initializing database...");

using var context = new InnovationGameDbContext();

var sessions = context.GameSessions.ToList();

context.GameSessions.RemoveRange(sessions);

Console.WriteLine($"Found {sessions.Count} sessions");
context.SaveChanges();
