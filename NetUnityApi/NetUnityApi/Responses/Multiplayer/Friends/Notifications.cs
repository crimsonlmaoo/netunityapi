using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NetUnityApi.Responses.Multiplayer.Friends
{
    public class NotificationsResponse
    {
        [JsonPropertyName("token")]
        public string token { get; set; }

        [JsonPropertyName("channel")]
        public string channel { get; set; }
    }
}
