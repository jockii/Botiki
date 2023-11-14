using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace BOTiki;

public class BOTikiConfig : BasePluginConfig
{
    [JsonPropertyName("BotMode")]
    public string BotMode { get; set; } = "balanced"; // fill, match, balanced

    [JsonPropertyName("bot_count")]
    public int bot_count { get; set; } = 1; // used only fill mode

    [JsonPropertyName("max_player_to_bot_kick")]
    public int max_player_to_bot_kick { get; set; } = 5; // after this player count all bot_kick
}
