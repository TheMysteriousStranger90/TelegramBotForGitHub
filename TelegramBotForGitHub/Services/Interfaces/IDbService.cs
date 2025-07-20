using TelegramBotForGitHub.Models;

namespace TelegramBotForGitHub.Services.Interfaces
{
    public interface IDbService
    {
        // UserToken methods
        Task<UserTokenEntity?> GetUserTokenEntityAsync(long userId);
        Task SaveUserTokenEntityAsync(UserTokenEntity entity);
        Task DeleteUserTokenEntityAsync(long userId);
        
        // ChatSubscription methods
        Task<ChatSubscription> GetSubscriptionAsync(long chatId, string repositoryUrl);
        Task CreateSubscriptionAsync(ChatSubscription subscription);
        Task UpdateSubscriptionAsync(ChatSubscription subscription);
        Task<List<ChatSubscription>> GetChatSubscriptionsAsync(long chatId);
        Task<List<ChatSubscription>> GetSubscriptionsForRepositoryAsync(string repositoryUrl);
        Task<List<ChatSubscription>> GetSubscriptionsAsync(string repositoryUrl);
        Task<bool> IsUserAuthorizedAsync(long userId);
        
        // GitHub OAuth methods
        Task<GitHubOAuthToken> GetGitHubTokenAsync(long userId);
        Task CreateOrUpdateTokenAsync(GitHubOAuthToken token);
        Task DeactivateUserTokenAsync(long userId);
        
        // GitHub Auth State methods
        Task CreateAuthStateAsync(GitHubAuthState authState);
        Task<GitHubAuthState> GetAuthStateAsync(string state);
        Task UpdateAuthStateAsync(GitHubAuthState authState);
        
        // Notification logging
        Task LogNotificationEntityAsync(NotificationLogEntity entity);
    }
}