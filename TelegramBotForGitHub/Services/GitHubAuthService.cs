using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TelegramBotForGitHub.Models;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Services
{
    public class GitHubAuthService : IGitHubAuthService
    {
        private readonly IDbService _dbService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubAuthService> _logger;

        public GitHubAuthService(
            IDbService cosmosDbService,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<GitHubAuthService> logger)
        {
            _dbService = cosmosDbService;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> IsUserAuthorizedAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Checking authorization for user {UserId}", userId);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var token = await _dbService.GetGitHubTokenAsync(userId);
                return token != null && token.IsActive;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout on auth check for user {UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authorization for user {UserId}", userId);
                return false;
            }
        }

        public async Task<string> GetAuthorizationUrl(long userId)
        {
            var clientId = _configuration["GitHub:ClientId"];
            var baseUrl = _configuration["BaseUrl"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(baseUrl))
                throw new InvalidOperationException("OAuth configuration missing");

            var state = Guid.NewGuid().ToString();
            var authState = new GitHubAuthState
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                State = state,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };
            await _dbService.CreateAuthStateAsync(authState);

            var redirectUri = $"{baseUrl}/api/auth/github/callback";
            
            var scopes = new[]
            {
                "repo",           
                "read:notifications",  
                "user:email",     
                "read:user"      
            };
            
            var scope = string.Join(" ", scopes);
            
            var authUrl = $"https://github.com/login/oauth/authorize" +
                         $"?client_id={clientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                         $"&scope={Uri.EscapeDataString(scope)}" +
                         $"&state={state}";

            _logger.LogInformation("Generated auth URL for user {UserId} with scopes: {Scopes}", userId, scope);
            return authUrl;
        }

        public async Task<GitHubOAuthToken?> ExchangeCodeForTokenAsync(string code, string state)
        {
            try
            {
                _logger.LogInformation("Exchanging code for token with state {State}", state);

                var authState = await _dbService.GetAuthStateAsync(state);
                if (authState == null)
                {
                    _logger.LogWarning("Auth state not found: {State}", state);
                    return null;
                }

                if (authState.IsUsed)
                {
                    _logger.LogWarning("Auth state already used: {State}", state);
                    return null;
                }

                if (authState.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Auth state expired: {State}", state);
                    return null;
                }

                authState.IsUsed = true;
                await _dbService.UpdateAuthStateAsync(authState);

                var clientId = _configuration["GitHub:ClientId"];
                var clientSecret = _configuration["GitHub:ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("GitHub OAuth credentials not configured");
                    return null;
                }

                var tokenRequest = new
                {
                    client_id = clientId,
                    client_secret = clientSecret,
                    code = code
                };

                var json = JsonConvert.SerializeObject(tokenRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");

                var response = await _httpClient.PostAsync("https://github.com/login/oauth/access_token", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to exchange code for token. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Token exchange response: {Response}", responseContent);
                
                var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (tokenResponse?.access_token == null)
                {
                    _logger.LogError("No access token in response: {Response}", responseContent);
                    return null;
                }

                var token = new GitHubOAuthToken
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = authState.UserId,
                    AccessToken = tokenResponse.access_token,
                    TokenType = tokenResponse.token_type ?? "bearer",
                    Scope = tokenResponse.scope ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Token created for user {UserId} with scopes: {Scopes}", 
                    authState.UserId, token.Scope);

                await _dbService.CreateOrUpdateTokenAsync(token);

                var grantedScopes = await GetTokenScopesAsync(token.AccessToken);
                _logger.LogInformation("Verified token scopes for user {UserId}: {VerifiedScopes}", 
                    authState.UserId, string.Join(", ", grantedScopes));

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for token");
                return null;
            }
        }

        public async Task<string[]> GetTokenScopesAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

                var response = await _httpClient.GetAsync("https://api.github.com/user");

                if (response.Headers.TryGetValues("X-OAuth-Scopes", out var scopes))
                {
                    return scopes.First().Split(',').Select(s => s.Trim()).ToArray();
                }

                return Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token scopes");
                return Array.Empty<string>();
            }
        }

        public async Task<GitHubUserProfile?> GetUserProfileAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

                var response = await _httpClient.GetAsync("https://api.github.com/user");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get user profile. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GitHubUserProfile>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return null;
            }
        }

        public async Task<GitHubOAuthToken?> GetUserTokenAsync(long userId)
        {
            try
            {
                return await _dbService.GetGitHubTokenAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user token for {UserId}", userId);
                return null;
            }
        }

        public async Task LogoutUserAsync(long userId)
        {
            try
            {
                await _dbService.DeactivateUserTokenAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out user {UserId}", userId);
                throw;
            }
        }
    }
}