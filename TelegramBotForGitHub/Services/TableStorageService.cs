using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Models;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Services
{
    public class TableStorageService : IDbService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly TableClient _tableClient;
        private readonly ILogger<TableStorageService> _logger;

        public TableStorageService(IConfiguration configuration, ILogger<TableStorageService> logger)
        {
            _logger = logger;

            var connectionString = configuration["TableStorage:ConnectionString"];
            _tableServiceClient = new TableServiceClient(connectionString);

            var tableName = configuration["TableStorage:TableName"] ?? "TelegramBotData";
            _tableClient = _tableServiceClient.GetTableClient(tableName);

            _tableClient.CreateIfNotExists();
        }
        
        #region GitHub OAuth Methods

        public async Task<GitHubOAuthToken> GetGitHubTokenAsync(long userId)
        {
            try
            {
                _logger.LogInformation("TableStorageService: Getting GitHub token for user {UserId}", userId);
                
                var response = await _tableClient.GetEntityAsync<GitHubOAuthTokenEntity>("GitHubOAuthToken", userId.ToString());
                var token = response.Value.ToGitHubOAuthToken();
                
                _logger.LogInformation("TableStorageService: Token query completed for user {UserId}, found: {Found}", 
                    userId, token != null);
                
                return token;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation("TableStorageService: No token found for user {UserId}", userId);
                return null;
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "TableStorageService: Table Storage service unavailable when getting GitHub token for {UserId}", userId);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TableStorageService: Error getting GitHub token for {UserId}", userId);
                throw;
            }
        }

        public async Task CreateOrUpdateTokenAsync(GitHubOAuthToken token)
        {
            try
            {
                var entity = GitHubOAuthTokenEntity.FromGitHubOAuthToken(token);
                await _tableClient.UpsertEntityAsync(entity);
                _logger.LogInformation("GitHub token saved for user {UserId}", token.UserId);
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "Table Storage service unavailable when saving GitHub token for {UserId}",
                    token.UserId);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving GitHub token for {UserId}", token.UserId);
                throw;
            }
        }

        public async Task DeactivateUserTokenAsync(long userId)
        {
            try
            {
                var token = await GetGitHubTokenAsync(userId);
                if (token != null)
                {
                    token.IsActive = false;
                    await CreateOrUpdateTokenAsync(token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating GitHub token for {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region GitHub Auth State Methods

        public async Task CreateAuthStateAsync(GitHubAuthState authState)
        {
            try
            {
                var entity = GitHubAuthStateEntity.FromGitHubAuthState(authState);
                await _tableClient.AddEntityAsync(entity);
                _logger.LogInformation("Auth state created for user {UserId}", authState.UserId);
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "Table Storage service unavailable when creating auth state for {UserId}",
                    authState.UserId);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating auth state for {UserId}", authState.UserId);
                throw;
            }
        }

        public async Task<GitHubAuthState> GetAuthStateAsync(string state)
        {
            try
            {
                var query = _tableClient.QueryAsync<GitHubAuthStateEntity>(
                    entity => entity.PartitionKey == "GitHubAuthState" && entity.State == state);

                await foreach (var entity in query)
                {
                    return entity.ToGitHubAuthState();
                }

                return null;
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "Table Storage service unavailable when getting auth state {State}", state);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auth state {State}", state);
                throw;
            }
        }

        public async Task UpdateAuthStateAsync(GitHubAuthState authState)
        {
            try
            {
                var entity = GitHubAuthStateEntity.FromGitHubAuthState(authState);
                await _tableClient.UpsertEntityAsync(entity);
                _logger.LogInformation("Auth state updated for user {UserId}", authState.UserId);
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "Table Storage service unavailable when updating auth state for {UserId}",
                    authState.UserId);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating auth state for {UserId}", authState.UserId);
                throw;
            }
        }

        #endregion

        #region ChatSubscription Methods
        
        public async Task<List<ChatSubscription>> GetSubscriptionsAsync(string repositoryUrl)
        {
            return await GetSubscriptionsForRepositoryAsync(repositoryUrl);
        }

        public async Task<ChatSubscription> GetSubscriptionAsync(long chatId, string repositoryUrl)
        {
            try
            {
                var rowKey = $"{chatId}_{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(repositoryUrl))}";
                var response = await _tableClient.GetEntityAsync<ChatSubscriptionEntity>("ChatSubscription", rowKey);
                return response.Value.ToChatSubscription();
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "Table Storage service unavailable when getting subscription for chat {ChatId}",
                    chatId);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription for chat {ChatId} and repository {RepositoryUrl}",
                    chatId, repositoryUrl);
                throw;
            }
        }

        public async Task CreateSubscriptionAsync(ChatSubscription subscription)
        {
            try
            {
                subscription.Id = Guid.NewGuid().ToString();
                subscription.CreatedAt = DateTime.UtcNow;
                subscription.UpdatedAt = DateTime.UtcNow;
                subscription.IsActive = true;
                subscription.UserId = subscription.ChatId;

                var entity = ChatSubscriptionEntity.FromChatSubscription(subscription);
                await _tableClient.AddEntityAsync(entity);
                _logger.LogInformation("Subscription created for chat {ChatId} and repository {RepositoryUrl}",
                    subscription.ChatId, subscription.RepositoryUrl);
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "Table Storage service unavailable when creating subscription for chat {ChatId}",
                    subscription.ChatId);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription for chat {ChatId} and repository {RepositoryUrl}",
                    subscription.ChatId, subscription.RepositoryUrl);
                throw;
            }
        }

        public async Task UpdateSubscriptionAsync(ChatSubscription subscription)
        {
            try
            {
                subscription.UpdatedAt = DateTime.UtcNow;
                var entity = ChatSubscriptionEntity.FromChatSubscription(subscription);
                await _tableClient.UpsertEntityAsync(entity);
                _logger.LogInformation("Subscription updated for chat {ChatId} and repository {RepositoryUrl}",
                    subscription.ChatId, subscription.RepositoryUrl);
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "Table Storage service unavailable when updating subscription for chat {ChatId}",
                    subscription.ChatId);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription for chat {ChatId} and repository {RepositoryUrl}",
                    subscription.ChatId, subscription.RepositoryUrl);
                throw;
            }
        }

        public async Task<List<ChatSubscription>> GetChatSubscriptionsAsync(long chatId)
        {
            try
            {
                var query = _tableClient.QueryAsync<ChatSubscriptionEntity>(
                    entity => entity.PartitionKey == "ChatSubscription" && entity.ChatId == chatId);

                var subscriptions = new List<ChatSubscription>();
                await foreach (var entity in query)
                {
                    subscriptions.Add(entity.ToChatSubscription());
                }

                return subscriptions;
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "Table Storage service unavailable when getting subscriptions for chat {ChatId}",
                    chatId);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions for chat {ChatId}", chatId);
                throw;
            }
        }

        public async Task<List<ChatSubscription>> GetSubscriptionsForRepositoryAsync(string repositoryUrl)
        {
            try
            {
                var query = _tableClient.QueryAsync<ChatSubscriptionEntity>(
                    entity => entity.PartitionKey == "ChatSubscription" && 
                              entity.RepositoryUrl == repositoryUrl && 
                              entity.IsActive == true);

                var subscriptions = new List<ChatSubscription>();
                await foreach (var entity in query)
                {
                    subscriptions.Add(entity.ToChatSubscription());
                }

                return subscriptions;
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex,
                    "Table Storage service unavailable when getting subscriptions for repository {RepositoryUrl}",
                    repositoryUrl);
                throw new InvalidOperationException(
                    "Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions for repository {RepositoryUrl}", repositoryUrl);
                throw;
            }
        }

        #endregion
        
        #region Notification Log Methods

        public async Task LogNotificationEntityAsync(NotificationLogEntity entity)
        {
            try
            {
                entity.RowKey = Guid.NewGuid().ToString();
                entity.CreatedAt = DateTime.UtcNow;

                await _tableClient.AddEntityAsync(entity);
                _logger.LogInformation("Notification log entity created for chat {ChatId}", entity.ChatId);
            }
            catch (RequestFailedException ex) when (ex.Status == 503)
            {
                _logger.LogWarning(ex, "Table Storage service unavailable when logging notification for chat {ChatId}", entity.ChatId);
                throw new InvalidOperationException("Database service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging notification for chat {ChatId}", entity.ChatId);
                throw;
            }
        }
        
        public async Task<List<NotificationLogEntity>> GetNotificationLogsAsync(long chatId)
        {
            try
            {
                var query = _tableClient.QueryAsync<NotificationLogEntity>(
                    entity => entity.PartitionKey == "NotificationLog" && entity.ChatId == chatId);

                var logs = new List<NotificationLogEntity>();
                await foreach (var entity in query)
                {
                    logs.Add(entity);
                }

                return logs
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(10)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification logs for chat {ChatId}", chatId);
                throw;
            }
        }
        
        #endregion
    }
}