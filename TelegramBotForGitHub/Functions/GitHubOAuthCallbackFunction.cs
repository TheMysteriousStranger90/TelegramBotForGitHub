using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Functions
{
    public class GitHubOAuthCallbackFunction
    {
        private readonly IGitHubService _gitHubService;
        private readonly IUserSessionService _userSessionService;
        private readonly ITelegramBotService _telegramBotService;
        private readonly ILogger<GitHubOAuthCallbackFunction> _logger;

        public GitHubOAuthCallbackFunction(
            IGitHubService gitHubService,
            IUserSessionService userSessionService,
            ITelegramBotService telegramBotService,
            ILogger<GitHubOAuthCallbackFunction> logger)
        {
            _gitHubService = gitHubService;
            _userSessionService = userSessionService;
            _telegramBotService = telegramBotService;
            _logger = logger;
        }

        [Function("GitHubOAuthCallback")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "github/callback")] HttpRequestData req)
        {
            try
            {
                var query = HttpUtility.ParseQueryString(req.Url.Query);
                var code = query["code"];
                var state = query["state"];

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                {
                    _logger.LogWarning("Missing code or state parameter");
                    return await CreateErrorResponse(req, "Required parameters are missing.");
                }

                var accessToken = await _gitHubService.ExchangeCodeForTokenAsync(code, state);
                var user = await _gitHubService.GetCurrentUserAsync(accessToken);

                // Find user session by state (this logic should be implemented securely)
                // For now, assume the state contains the Telegram user ID

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/html; charset=utf-8");

                var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>GitHub Authorization</title>
                    <meta charset='utf-8'>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            text-align: center;
                            margin-top: 50px;
                            background-color: #f5f5f5;
                        }}
                        .container {{
                            background-color: white;
                            padding: 30px;
                            border-radius: 10px;
                            display: inline-block;
                            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                        }}
                        .success {{
                            color: #28a745;
                            font-size: 24px;
                            margin-bottom: 20px;
                        }}
                        .username {{
                            color: #007bff;
                            font-weight: bold;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='success'>✅ Authorization successful!</div>
                        <p>Welcome, <span class='username'>{user.Login}</span>!</p>
                        <p>You may now close this page and return to the bot in Telegram.</p>
                    </div>
                </body>
                </html>";

                await response.WriteStringAsync(html);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GitHub OAuth callback");
                return await CreateErrorResponse(req, "An error occurred while processing authorization.");
            }
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");

            var html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Authorization Error</title>
                <meta charset='utf-8'>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        text-align: center;
                        margin-top: 50px;
                        background-color: #f5f5f5;
                    }}
                    .container {{
                        background-color: white;
                        padding: 30px;
                        border-radius: 10px;
                        display: inline-block;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                    }}
                    .error {{
                        color: #dc3545;
                        font-size: 24px;
                        margin-bottom: 20px;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='error'>❌ Error</div>
                    <p>{message}</p>
                    <p>Please try authorizing again via the Telegram bot.</p>
                </div>
            </body>
            </html>";

            await response.WriteStringAsync(html);
            return response;
        }
    }
}
