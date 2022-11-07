using Domain.Models;

namespace IsolatedFunctions.Services.Interfaces;

public interface ICardService
{
    Task AddCard(Card card);
    Task UpdateCard(Card card);
    Task<List<Card>> GetAllCards();
    Task<Card?> GetCardById(Guid id);
    Task RemoveCard(Card card);
    Task<bool> CardExists(string name);
}
