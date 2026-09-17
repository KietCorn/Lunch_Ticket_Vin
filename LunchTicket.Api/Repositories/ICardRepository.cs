using LunchTicket.Api.Models;

namespace LunchTicket.Api.Repositories;

public interface ICardRepository
{
    Task<Card?> GetByTokenAsync(string cardToken);
    Task<Card?> GetByIdAsync(int id);
    Task<Card?> GetActiveByStudentIdAsync(int studentId);
    Task<Staff?> GetStaffByUsernameAsync(string username);
    Task AddCardAsync(Card card);
    Task<Student?> GetStudentByIdAsync(int studentId);
}
