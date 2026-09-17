using LunchTicket.Api.Data;
using LunchTicket.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LunchTicket.Api.Repositories;

public class CardRepository : ICardRepository
{
    private readonly AppDbContext _db;

    public CardRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Card?> GetByTokenAsync(string cardToken) =>
        _db.Cards.FirstOrDefaultAsync(c => c.CardToken == cardToken);

    public Task<Card?> GetByIdAsync(int id) =>
        _db.Cards.FirstOrDefaultAsync(c => c.Id == id);

    public Task<Card?> GetActiveByStudentIdAsync(int studentId) =>
        _db.Cards.FirstOrDefaultAsync(c => c.StudentId == studentId && c.Status == CardStatus.Active);

    public Task<Staff?> GetStaffByUsernameAsync(string username) =>
        _db.Staff.FirstOrDefaultAsync(s => s.Username == username);

    public async Task AddCardAsync(Card card) => await _db.Cards.AddAsync(card);

    public Task<Student?> GetStudentByIdAsync(int studentId) =>
        _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
}
