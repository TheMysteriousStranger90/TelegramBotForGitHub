using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Functions;

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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "github/callback")]
        HttpRequestData req)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(req.Url.Query);
            var code = query["code"];
            var state = query["state"];
            var error = query["error"];

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("GitHub OAuth error: {Error}", error);
                return await CreateErrorResponse(req, "Authorization was canceled by the user.");
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                _logger.LogWarning("Missing code or state parameter");
                return await CreateErrorResponse(req, "Required parameters are missing.");
            }

            if (!_gitHubService.IsValidAuthState(state))
            {
                _logger.LogWarning("Invalid or expired state parameter: {State}", state);
                return await CreateErrorResponse(req, "Invalid or expired authorization token.");
            }

            var (accessToken, telegramUserId) = await _gitHubService.ExchangeCodeForTokenAsync(code, state);
            var user = await _gitHubService.GetCurrentUserAsync(accessToken);

            // Update user session
            var session = await _userSessionService.GetUserSessionAsync(telegramUserId);
            if (session != null)
            {
                session.GitHubToken = accessToken;
                session.GitHubUsername = user.Login;
                await _userSessionService.UpdateUserSessionAsync(session);

                // Send notification to Telegram
                try
                {
                    await _telegramBotService.SendMessageAsync(
                        telegramUserId,
                        $"✅ Authorization successful!\n\nWelcome, <b>{user.Login}</b>!\n\nYou can now use all bot features.",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending notification to Telegram user {UserId}", telegramUserId);
                }
            }

            return await CreateSuccessResponse(req, user.Login, user.AvatarUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub OAuth callback");
            return await CreateErrorResponse(req, "An error occurred while processing authorization.");
        }
    }

    private async Task<HttpResponseData> CreateSuccessResponse(HttpRequestData req, string username,
        string avatarUrl)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");

        var html = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <title>GitHub Authorization</title>
                            <meta charset='utf-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1'>
                            <style>
                                body {{
                                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                                    text-align: center;
                                    margin: 0;
                                    padding: 20px;
                                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                                    min-height: 100vh;
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                }}
                                .container {{
                                    background-color: white;
                                    padding: 40px;
                                    border-radius: 15px;
                                    box-shadow: 0 10px 30px rgba(0,0,0,0.3);
                                    max-width: 400px;
                                    width: 100%;
                                }}
                                .success {{
                                    color: #28a745;
                                    font-size: 48px;
                                    margin-bottom: 20px;
                                }}
                                .avatar {{
                                    width: 80px;
                                    height: 80px;
                                    border-radius: 50%;
                                    margin: 20px auto;
                                    display: block;
                                    border: 3px solid #e9ecef;
                                }}
                                .title {{
                                    color: #333;
                                    font-size: 24px;
                                    margin-bottom: 15px;
                                    font-weight: 600;
                                }}
                                .username {{
                                    color: #007bff;
                                    font-weight: bold;
                                    font-size: 18px;
                                }}
                                .description {{
                                    color: #666;
                                    font-size: 16px;
                                    line-height: 1.5;
                                    margin-top: 20px;
                                }}
                                .countdown {{
                                    color: #999;
                                    font-size: 14px;
                                    margin-top: 15px;
                                }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='success'>✅</div>
                                <div class='title'>Authorization Successful!</div>
                                <p>Welcome, <span class='username'>{username}</span>!</p>
                                <p class='description'>
                                    You can now use all the bot features for working with GitHub.
                                    Please return to Telegram to continue.
                                </p>
                                <p class='countdown'>This window will close automatically in <span id='timer'>5</span> seconds</p>
                            </div>
                            <script>
                                let seconds = 5;
                                const timer = document.getElementById('timer');
                                const countdown = setInterval(() => {{
                                    seconds--;
                                    timer.textContent = seconds;
                                    if (seconds <= 0) {{
                                        clearInterval(countdown);
                                        window.close();
                                    }}
                                }}, 1000);
                            </script>
                        </body>
                        </html>";

        await response.WriteStringAsync(html);
        return response;
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
                <meta name='viewport' content='width=device-width, initial-scale=1'>
                <style>
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                        text-align: center;
                        margin: 0;
                        padding: 20px;
                        background: linear-gradient(135deg, #ff6b6b 0%, #ee5a52 100%);
                        min-height: 100vh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                    }}
                    .container {{
                        background-color: white;
                        padding: 40px;
                        border-radius: 15px;
                        box-shadow: 0 10px 30px rgba(0,0,0,0.3);
                        max-width: 400px;
                        width: 100%;
                    }}
                    .error {{
                        color: #dc3545;
                        font-size: 48px;
                        margin-bottom: 20px;
                    }}
                    .title {{
                        color: #333;
                        font-size: 24px;
                        margin-bottom: 15px;
                        font-weight: 600;
                    }}
                    .description {{
                        color: #666;
                        font-size: 16px;
                        line-height: 1.5;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='error'>❌</div>
                    <div class='title'>Authorization Error</div>
                    <p class='description'>{message}</p>
                    <p class='description'>Please try authorizing again via the Telegram bot.</p>
                </div>
            </body>
            </html>";

        await response.WriteStringAsync(html);
        return response;
    }
}