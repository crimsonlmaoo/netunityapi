using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NetUnityApi.Responses.Multiplayer.Friends
{
    public class RelationshipResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("expires")]
        public DateTime? Expires { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // FRIEND, BLOCK, FRIEND_REQUEST

        [JsonPropertyName("members")]
        public List<RelationshipMember> Members { get; set; }
    }

    public class RelationshipMember
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } // SENDER or TARGET

        [JsonPropertyName("profile")]
        public MemberProfile Profile { get; set; }
    }

    public class MemberProfile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class CreateFriendRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "FRIEND_REQUEST";

        [JsonPropertyName("members")]
        public List<MemberRequest> Members { get; set; }
    }

    public class MemberRequest
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "TARGET";

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
