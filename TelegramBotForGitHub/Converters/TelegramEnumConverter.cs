using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TelegramBotForGitHub.Converters
{
    public class TelegramEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string enumText = reader.Value.ToString();
                
                if (objectType.Namespace?.StartsWith("Telegram.Bot") == true)
                {
                    enumText = ConvertSnakeCaseToPascalCase(enumText);
                }
                
                try
                {
                    if (Enum.TryParse(objectType, enumText, true, out var result))
                    {
                        return result;
                    }
                }
                catch
                {
                }
            }
            
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
        
        private string ConvertSnakeCaseToPascalCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
                return snakeCase;
                
            var parts = snakeCase.Split('_');
            var result = "";
            
            foreach (var part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    result += char.ToUpper(part[0]) + part.Substring(1).ToLower();
                }
            }
            
            return result;
        }
    }
}