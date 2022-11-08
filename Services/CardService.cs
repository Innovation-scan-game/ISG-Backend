using DAL.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services;

public class CardService : ICardService
{
    private readonly InnovationGameDbContext _context;

    public CardService(InnovationGameDbContext context)
    {
        _context = context;
    }

    public async Task AddCard(Card card)
    {
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCard(Card card)
    {
        _context.Cards.Update(card);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Card>> GetAllCards()
    {
        return await _context.Cards.ToListAsync();
    }

    public async Task<Card?> GetCardById(Guid id)
    {
        return await _context.Cards.FirstOrDefaultAsync(c => c.Id == id);
    }


    public async Task<bool> CardExists(string name)
    {
        return await _context.Cards.AnyAsync(c => c.Name == name);
    }

    public async Task RemoveCard(Card card)
    {
        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
    }
}
