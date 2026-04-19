using System.Text.Json.Serialization;

namespace NetUnityApi.Responses.Multiplayer.Friends
{
    public class PresenceResponse
    {
        [JsonPropertyName("availability")]
        public string Availability { get; set; }

        [JsonPropertyName("activity")]
        public PlayerActivity Activity { get; set; }
    }

    public class PlayerActivity
    {
        // Known Unity fields
        [JsonPropertyName("Activity")]
        public int ActivityType { get; set; }

        [JsonPropertyName("Platform")]
        public int Platform { get; set; }

        // This captures EVERYTHING else the user wants to add
        [JsonExtensionData]
        public Dictionary<string, object> CustomValues { get; set; } = new Dictionary<string, object>();
    }
}