namespace LunchTicket.Api.DTOs;

public record StudentDto(int Id, string StudentId, string FullName, string? Email, DateTime CreatedAt, DateTime? UpdatedAt);

public record CreateStudentRequest(string StudentId, string FullName, string? Email, string Password);

public record StaffDto(int Id, string Username, string FullName, bool IsAdmin, bool IsActive, DateTime CreatedAt);

public record CreateStaffRequest(string Username, string FullName, string Password, bool IsAdmin);
