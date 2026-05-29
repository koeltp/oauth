using System.Text.Json.Serialization;

namespace OAuth.Contracts.Auth;

public class Verify2FARequest
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}
