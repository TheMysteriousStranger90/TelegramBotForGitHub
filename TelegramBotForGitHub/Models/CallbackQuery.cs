using Newtonsoft.Json;

namespace TelegramBotForGitHub.Models;

public class CallbackQuery
{
    [JsonProperty("id")]
    public string Id { get; set; }
        
    [JsonProperty("from")]
    public TelegramUser From { get; set; }
        
    [JsonProperty("message")]
    public TelegramMessage Message { get; set; }
        
    [JsonProperty("data")]
    public string Data { get; set; }
}