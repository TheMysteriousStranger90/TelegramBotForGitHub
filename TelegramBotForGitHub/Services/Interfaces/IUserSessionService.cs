using TelegramBotForGitHub.Models;

namespace TelegramBotForGitHub.Services.Interfaces;

public interface IUserSessionService
{
    Task<UserSession?> GetUserSessionAsync(long telegramUserId);
    Task<UserSession> CreateUserSessionAsync(long telegramUserId);
    Task<UserSession> UpdateUserSessionAsync(UserSession session);
    Task DeleteUserSessionAsync(long telegramUserId);
    Task<bool> IsUserAuthorizedAsync(long telegramUserId);
}