using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Telegram.Bot;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Functions
{
    public class GitHubOAuthFunction
    {
        private readonly ILogger<GitHubOAuthFunction> _logger;
        private readonly IGitHubAuthService _authService;
        private readonly ITelegramBotClient _telegramBotClient;

        public GitHubOAuthFunction(
            ILogger<GitHubOAuthFunction> logger,
            IGitHubAuthService authService,
            ITelegramBotClient telegramBotClient)
        {
            _logger = logger;
            _authService = authService;
            _telegramBotClient = telegramBotClient;
        }

        [Function("GitHubOAuthCallback")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/github/callback")] HttpRequestData req)
        {
            _logger.LogInformation("GitHub OAuth callback received");

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var code = query["code"];
                var state = query["state"];
                var error = query["error"];

                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("OAuth error: {Error}", error);
                    return CreateErrorResponse(req, "Authorization was denied or failed.");
                }

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                {
                    _logger.LogWarning("Missing code or state parameter");
                    return CreateErrorResponse(req, "Invalid authorization response.");
                }

                var token = await _authService.ExchangeCodeForTokenAsync(code, state);
                if (token == null)
                {
                    _logger.LogWarning("Failed to exchange code for token");
                    return CreateErrorResponse(req, "Failed to complete authorization.");
                }

                var userProfile = await _authService.GetUserProfileAsync(token.AccessToken);
                if (userProfile != null)
                {
                    var successMessage = $"✅ **Authorization Successful!**\n\n" +
                                       $"Welcome, {userProfile.Name ?? userProfile.Login}!\n\n" +
                                       $"Your GitHub account has been successfully connected.\n" +
                                       $"You can now use all bot features:\n\n" +
                                       $"• `/profile` - View your GitHub profile\n" +
                                       $"• `/myrepos` - List your repositories\n" +
                                       $"• `/subscribe owner/repo` - Subscribe to notifications\n" +
                                       $"Happy coding! 🚀";

                    try
                    {
                        await _telegramBotClient.SendMessage(token.UserId, successMessage, 
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        _logger.LogInformation("User {UserId} authorized successfully", token.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send authorization confirmation to user {UserId}", token.UserId);
                    }
                }

                return CreateSuccessResponse(req);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GitHub OAuth callback");
                return CreateErrorResponse(req, "An unexpected error occurred.");
            }
        }

        private HttpResponseData CreateSuccessResponse(HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");
            
            var html = """
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Authorization Success</title>
                    <style>
                        body { font-family: Arial, sans-serif; text-align: center; margin: 50px; }
                        .success { color: #28a745; }
                        .container { max-width: 500px; margin: 0 auto; }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <h1 class="success">✅ Authorization Successful!</h1>
                        <p>Your GitHub account has been successfully connected to the Telegram bot.</p>
                        <p>You can now return to Telegram and use all bot features.</p>
                        <p><strong>This window can be closed.</strong></p>
                    </div>
                </body>
                </html>
                """;
            
            response.WriteString(html);
            return response;
        }

        private HttpResponseData CreateErrorResponse(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");
            
            var html = $"""
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Authorization Error</title>
                </head>
                <body>
                    <div class="container">
                        <h1 class="error">❌ Authorization Failed</h1>
                        <p>{message}</p>
                        <p>Please return to Telegram and try again with the /auth command.</p>
                    </div>
                </body>
                </html>
                """;
            
            response.WriteString(html);
            return response;
        }
    }
}