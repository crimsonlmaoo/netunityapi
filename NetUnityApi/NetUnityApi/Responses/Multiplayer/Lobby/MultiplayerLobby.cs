using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetUnityApi.Responses.Multiplayer.Lobby
{
    public class LobbyResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("upid")] public string Upid { get; set; }
        [JsonPropertyName("environmentId")] public string EnvironmentId { get; set; }
        [JsonPropertyName("hostId")] public string HostId { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("maxPlayers")] public int MaxPlayers { get; set; }
        [JsonPropertyName("availableSlots")] public int AvailableSlots { get; set; }
        [JsonPropertyName("isPrivate")] public bool IsPrivate { get; set; }
        [JsonPropertyName("isLocked")] public bool IsLocked { get; set; }
        [JsonPropertyName("lobbyCode")] public string LobbyCode { get; set; }
        [JsonPropertyName("created")] public DateTime Created { get; set; }
        [JsonPropertyName("lastUpdated")] public DateTime LastUpdated { get; set; }
        [JsonPropertyName("version")] public int Version { get; set; }
        [JsonPropertyName("players")] public List<LobbyPlayer> Players { get; set; } = new List<LobbyPlayer>();
        [JsonPropertyName("data")] public Dictionary<string, LobbyDataObject> Data { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public class LobbyPlayer
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("allocationId")] public string AllocationId { get; set; }
        [JsonPropertyName("joined")] public DateTime Joined { get; set; }
        [JsonPropertyName("lastUpdated")] public DateTime LastUpdated { get; set; }
        [JsonPropertyName("profile")] public PlayerProfile Profile { get; set; }
        [JsonPropertyName("data")] public Dictionary<string, LobbyDataObject> Data { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public class PlayerProfile
    {
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public class LobbyDataObject
    {
        [JsonPropertyName("value")] public string Value { get; set; }
        [JsonPropertyName("visibility")] public string Visibility { get; set; } // "public", "private", "member"
        [JsonPropertyName("index")] public string Index { get; set; } // "S1", "N1", etc.
    }

    public class QueryResponse
    {
        [JsonPropertyName("results")] public List<LobbyResponse> Results { get; set; }
        [JsonPropertyName("continuationToken")] public string ContinuationToken { get; set; }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}