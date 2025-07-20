using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using Telegram.Bot;

namespace TelegramBotForGitHub.Functions;

public class SetupWebhookFunction
{
    private readonly ILogger<SetupWebhookFunction> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IConfiguration _configuration;

    public SetupWebhookFunction(
        ILogger<SetupWebhookFunction> logger,
        ITelegramBotClient telegramBotClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _configuration = configuration;
    }

    [Function("SetupWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "setup/webhook")] HttpRequestData req)
    {
        _logger.LogInformation("Setting up Telegram webhook");

        try
        {
            var baseUrl = _configuration["BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogError("BaseUrl not configured");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var webhookUrl = $"{baseUrl}/api/webhook/telegram";
            
            await _telegramBotClient.SetWebhook(webhookUrl);
            
            _logger.LogInformation("Telegram webhook set to {WebhookUrl}", webhookUrl);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            var result = new
            {
                status = "success",
                webhook_url = webhookUrl,
                message = "Telegram webhook has been set successfully"
            };
            
            await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(result));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up Telegram webhook");
            
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "application/json");
            
            var result = new
            {
                status = "error",
                message = ex.Message
            };
            
            await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(result));
            return response;
        }
    }
}