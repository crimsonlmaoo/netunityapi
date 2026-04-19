using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NetUnityApi.Responses.Multiplayer.Friends
{
    public class InviteResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("message")]
        public InviteMessage Message { get; set; }
    }

    public class InviteMessage
    {
        [JsonPropertyName("lobby_deeplink")]
        public string LobbyDeeplink { get; set; }
    }
}
