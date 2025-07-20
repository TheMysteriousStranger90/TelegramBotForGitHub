using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Converters;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Functions
{
    public class TelegramBotFunction
    {
        private readonly ITelegramService _telegramService;
        private readonly ILogger<TelegramBotFunction> _logger;

        public TelegramBotFunction(ITelegramService telegramService, ILogger<TelegramBotFunction> logger)
        {
            _telegramService = telegramService;
            _logger = logger;
        }

        [Function("TelegramBot")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "webhook/telegram")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                await response.WriteStringAsync("{ \"status\": \"ok\" }");
                return response;
            }

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogWarning("Received empty webhook body.");
                    await response.WriteStringAsync("{ \"status\": \"ok\", \"message\": \"Empty body acknowledged\" }");
                    return response;
                }

                _logger.LogInformation("Webhook body: {Body}", requestBody);

                var jsonSettings = new JsonSerializerSettings
                {
                    Converters = { new UnixDateTimeConverter(), new TelegramEnumConverter() },
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    Error = (sender, args) =>
                    {
                        _logger.LogWarning("JSON deserialization error ignored: {Error}", args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    }
                };

                var update = JsonConvert.DeserializeObject<Update>(requestBody, jsonSettings);

                if (update == null)
                {
                    _logger.LogWarning("Failed to deserialize update or update is null. Body: {Body}", requestBody);
                    await response.WriteStringAsync("{ \"status\": \"ok\", \"message\": \"Null update acknowledged\" }");
                    return response;
                }

                if (update.Message != null)
                {
                    await _telegramService.HandleUpdateAsync(update.Message);
                    _logger.LogInformation("Update {UpdateId} processed successfully.", update.Id);
                }
                else
                {
                    _logger.LogInformation("Received an update of type '{UpdateType}' which is not a message. Skipping.", update.Type);
                }

                await response.WriteStringAsync("{ \"status\": \"ok\", \"message\": \"Webhook processed\" }");
                return response;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "CRITICAL: Failed to deserialize webhook body.");
                await response.WriteStringAsync("{ \"status\": \"ok\", \"message\": \"Invalid JSON format acknowledged\" }");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL: An unhandled error occurred while processing the webhook.");
                await response.WriteStringAsync("{ \"status\": \"ok\", \"message\": \"Critical error acknowledged\" }");
                return response;
            }
        }
    }
}