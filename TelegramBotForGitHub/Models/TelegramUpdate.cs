using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace TelegramBotForGitHub.Models;

public class TelegramUpdate
{
    [JsonProperty("update_id")]
    public int UpdateId { get; set; }
        
    [JsonProperty("message")]
    public TelegramMessage Message { get; set; }
        
    [JsonProperty("callback_query")]
    public CallbackQuery CallbackQuery { get; set; }
}