using DAL.Data;
using Domain.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;

namespace Services;

public class CardService : ICardService
{
    private readonly InnovationGameDbContext _context;
    private readonly IValidator<Card> _validator;

    public CardService(InnovationGameDbContext context,IValidator<Card> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task AddCard(Card card)
    {
        if (await GetCardById(card.Id) is not null)
        {
            throw new Exception("Card already exists");
        }

        var validation = await _validator.ValidateAsync(card);
        if (!validation.IsValid)
        {
            throw new Exception(validation.Errors[0].ErrorMessage);
        }

        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCard(Card card)
    {
        if (await GetCardById(card.Id) is null)
        {
            throw new Exception("Card does not exist");
        }

        var validation = await _validator.ValidateAsync(card);
        if (!validation.IsValid)
        {
            throw new Exception(validation.Errors[0].ErrorMessage);
        }

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
