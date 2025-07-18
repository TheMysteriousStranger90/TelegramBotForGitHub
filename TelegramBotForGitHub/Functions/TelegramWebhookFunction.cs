using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Functions;

public class TelegramWebhookFunction
{
    private readonly ITelegramBotService _telegramBotService;
    private readonly ILogger<TelegramWebhookFunction> _logger;

    public TelegramWebhookFunction(ITelegramBotService telegramBotService, ILogger<TelegramWebhookFunction> logger)
    {
        _telegramBotService = telegramBotService;
        _logger = logger;
    }

    [Function("TelegramWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telegram/webhook")]
        HttpRequestData req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogWarning("Empty request body received");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var update = JsonSerializer.Deserialize<Update>(requestBody);
            if (update == null)
            {
                _logger.LogWarning("Failed to deserialize update");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            await _telegramBotService.HandleUpdateAsync(update);

            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram webhook");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}