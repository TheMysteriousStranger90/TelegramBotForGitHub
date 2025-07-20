using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TelegramBotForGitHub.Models
{
    public class TelegramMessage
    {
        [JsonProperty("message_id")]
        public int MessageId { get; set; }

        [JsonProperty("from")]
        public TelegramUser From { get; set; }

        [JsonProperty("date")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public System.DateTime Date { get; set; }

        [JsonProperty("chat")]
        public TelegramChat Chat { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("entities")]
        public List<MessageEntity> Entities { get; set; }

        [JsonProperty("reply_to_message")]
        public TelegramMessage ReplyToMessage { get; set; }
    }
}