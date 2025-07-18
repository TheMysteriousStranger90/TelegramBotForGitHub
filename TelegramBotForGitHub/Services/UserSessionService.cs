using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramBotForGitHub.Configuration;
using TelegramBotForGitHub.Models;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Services;

public class UserSessionService : IUserSessionService
{
    private readonly Container _container;
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(CosmosClient cosmosClient, IOptions<BotConfiguration> config,
        ILogger<UserSessionService> logger)
    {
        _logger = logger;
        var database = cosmosClient.GetDatabase(config.Value.CosmosDB.DatabaseName);
        _container = database.GetContainer(config.Value.CosmosDB.ContainerName);
    }

    public async Task<UserSession?> GetUserSessionAsync(long telegramUserId)
    {
        try
        {
            var response = await _container.ReadItemAsync<UserSession>(
                telegramUserId.ToString(),
                new PartitionKey(telegramUserId.ToString())
            );
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user session for {TelegramUserId}", telegramUserId);
            throw;
        }
    }

    public async Task<UserSession> CreateUserSessionAsync(long telegramUserId)
    {
        try
        {
            var session = new UserSession
            {
                Id = telegramUserId.ToString(),
                PartitionKey = telegramUserId.ToString(),
                TelegramUserId = telegramUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var response = await _container.CreateItemAsync(session, new PartitionKey(session.PartitionKey));
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user session for {TelegramUserId}", telegramUserId);
            throw;
        }
    }

    public async Task<UserSession> UpdateUserSessionAsync(UserSession session)
    {
        try
        {
            session.UpdatedAt = DateTime.UtcNow;
            var response = await _container.UpsertItemAsync(session, new PartitionKey(session.PartitionKey));
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user session for {TelegramUserId}", session.TelegramUserId);
            throw;
        }
    }

    public async Task DeleteUserSessionAsync(long telegramUserId)
    {
        try
        {
            await _container.DeleteItemAsync<UserSession>(
                telegramUserId.ToString(),
                new PartitionKey(telegramUserId.ToString())
            );
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("User session for {TelegramUserId} not found for deletion", telegramUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user session for {TelegramUserId}", telegramUserId);
            throw;
        }
    }

    public async Task<bool> IsUserAuthorizedAsync(long telegramUserId)
    {
        var session = await GetUserSessionAsync(telegramUserId);
        return session?.GitHubToken != null;
    }
}