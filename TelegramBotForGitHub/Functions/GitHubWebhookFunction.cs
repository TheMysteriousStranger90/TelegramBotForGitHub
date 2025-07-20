using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using TelegramBotForGitHub.Models;
using TelegramBotForGitHub.Services;
using Microsoft.Extensions.Configuration;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Functions;

public class GitHubWebhookFunction
{
    private readonly ILogger<GitHubWebhookFunction> _logger;
    private readonly IDbService _dbService;
    private readonly ITelegramService _telegramService;
    private readonly IConfiguration _configuration;

    public GitHubWebhookFunction(
        ILogger<GitHubWebhookFunction> logger,
        IDbService cosmosDbService,
        ITelegramService telegramService,
        IConfiguration configuration)
    {
        _logger = logger;
        _dbService = cosmosDbService;
        _telegramService = telegramService;
        _configuration = configuration;
    }

    [Function("GitHubWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhook/github")] HttpRequestData req)
    {
        _logger.LogInformation("GitHub webhook received");

        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            
            var signature = req.Headers.GetValues("X-Hub-Signature-256").FirstOrDefault();
            if (!VerifySignature(body, signature))
            {
                _logger.LogWarning("Invalid webhook signature");
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var eventType = req.Headers.GetValues("X-GitHub-Event").FirstOrDefault();
            if (string.IsNullOrEmpty(eventType))
            {
                _logger.LogWarning("Missing X-GitHub-Event header");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            await ProcessGitHubEvent(eventType, body);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync("{\"status\":\"ok\"}");
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    private bool VerifySignature(string body, string? signature)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        var secret = _configuration["GitHub:WebhookSecret"];
        if (string.IsNullOrEmpty(secret))
            return false;

        var expectedSignature = "sha256=" + ComputeHmacSha256(body, secret);
        return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    private string ComputeHmacSha256(string message, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    private async Task ProcessGitHubEvent(string eventType, string body)
    {
        try
        {
            var repositoryUrl = ExtractRepositoryUrl(body);
            if (string.IsNullOrEmpty(repositoryUrl))
            {
                _logger.LogWarning("Could not extract repository URL from webhook");
                return;
            }

            var subscriptions = await _dbService.GetSubscriptionsAsync(repositoryUrl);
            if (!subscriptions.Any())
            {
                _logger.LogInformation("No subscriptions found for repository {RepositoryUrl}", repositoryUrl);
                return;
            }

            object? eventData = eventType switch
            {
                "push" => JsonConvert.DeserializeObject<GitHubPushEvent>(body),
                "pull_request" => JsonConvert.DeserializeObject<GitHubPullRequestEvent>(body),
                "issues" => JsonConvert.DeserializeObject<GitHubIssueEvent>(body),
                _ => null
            };

            if (eventData == null)
            {
                _logger.LogWarning("Unsupported event type: {EventType}", eventType);
                return;
            }

            var message = await _telegramService.FormatGitHubNotificationAsync(eventType, eventData);

            foreach (var subscription in subscriptions)
            {
                if (subscription.Events.Contains(eventType))
                {
                    try
                    {
                        await _telegramService.SendNotificationAsync(subscription.ChatId, message);
                        
                        await _dbService.LogNotificationEntityAsync(
                            new NotificationLogEntity() 
                            {
                                ChatId = subscription.ChatId,
                                RepositoryUrl = repositoryUrl,
                                EventType = eventType,
                                Message = message,
                                Success = true
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending notification to chat {ChatId}", subscription.ChatId);
                        
                        await _dbService.LogNotificationEntityAsync(
                            new NotificationLogEntity() 
                            {
                                ChatId = subscription.ChatId,
                                RepositoryUrl = repositoryUrl,
                                EventType = eventType,
                                Message = message,
                                Success = false
                            });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub event {EventType}", eventType);
        }
    }

    private string? ExtractRepositoryUrl(string body)
    {
        try
        {
            var json = JsonConvert.DeserializeObject<dynamic>(body);
            return json?.repository?.html_url?.ToString();
        }
        catch
        {
            return null;
        }
    }
}