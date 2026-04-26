using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetUnityApi.Live_Operations.Game_Backend.Leaderboard.ClientAPI
{
    //ai for the generation + summaries - didn't want to spend 2+ hours on a single api to move onto others
    // Models updated to match Unity Leaderboards v1 OpenAPI Spec

    /// <summary>Request body for submitting a new score.</summary>
    public class AddPlayerScoreRequest
    {
        [JsonPropertyName("score")] public double Score { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, string> Metadata { get; set; }
        [JsonPropertyName("versionId")] public string VersionId { get; set; }
    }

    /// <summary>Filter parameters for fetching scores.</summary>
    public class GetScoresRequest
    {
        [JsonPropertyName("limit")] public int Limit { get; set; } = 10;
        [JsonPropertyName("offset")] public int Offset { get; set; } = 0;
        [JsonPropertyName("includeMetadata")] public bool IncludeMetadata { get; set; } = false;
    }

    /// <summary>Request body for getting scores by multiple player IDs.</summary>
    public class PlayerIdsRequest
    {
        [JsonPropertyName("playerIds")] public List<string> PlayerIds { get; set; }
    }

    /// <summary>Request to create a new leaderboard configuration (admin).</summary>
    public class CreateLeaderboardRequest
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("sortOrder")] public string SortOrder { get; set; } = "desc"; // asc or desc
        [JsonPropertyName("updateStrategy")] public string UpdateStrategy { get; set; } = "best"; // best, latest, sum
        [JsonPropertyName("maxEntries")] public int MaxEntries { get; set; } = 100;
        [JsonPropertyName("tiers")] public List<TierConfig> Tiers { get; set; }
        [JsonPropertyName("buckets")] public List<BucketConfig> Buckets { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, string> Metadata { get; set; }
    }

    public class TierConfig
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("from")] public double From { get; set; }
        [JsonPropertyName("to")] public double To { get; set; }
    }

    public class BucketConfig
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public class LeaderboardConfig
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("sortOrder")] public string SortOrder { get; set; }
        [JsonPropertyName("updateStrategy")] public string UpdateStrategy { get; set; }
        [JsonPropertyName("maxEntries")] public int MaxEntries { get; set; }
        [JsonPropertyName("tiers")] public List<TierConfig> Tiers { get; set; }
        [JsonPropertyName("buckets")] public List<BucketConfig> Buckets { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, string> Metadata { get; set; }
        [JsonPropertyName("created")] public DateTime Created { get; set; }
        [JsonPropertyName("updated")] public DateTime Updated { get; set; }
    }

    public class LeaderboardEntry
    {
        [JsonPropertyName("playerId")] public string PlayerId { get; set; }
        [JsonPropertyName("playerName")] public string PlayerName { get; set; }
        [JsonPropertyName("rank")] public int Rank { get; set; }
        [JsonPropertyName("score")] public double Score { get; set; }
        [JsonPropertyName("tier")] public string Tier { get; set; }
        [JsonPropertyName("metadata")] public Dictionary<string, string> Metadata { get; set; }
        [JsonPropertyName("updatedTime")] public DateTime UpdatedTime { get; set; }
    }

    public class PlayerScoreResponse : LeaderboardEntry
    {
        [JsonPropertyName("version")] public ArchivedVersion Version { get; set; }
    }

    public class PaginatedScoresResponse
    {
        [JsonPropertyName("tier")] public string Tier { get; set; }
        [JsonPropertyName("version")] public ArchivedVersion Version { get; set; }
        [JsonPropertyName("offset")] public int Offset { get; set; }
        [JsonPropertyName("limit")] public int Limit { get; set; }
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("results")] public List<LeaderboardEntry> Results { get; set; }
    }

    public class PlayerIdsResponse
    {
        [JsonPropertyName("version")] public ArchivedVersion Version { get; set; }
        [JsonPropertyName("results")] public List<LeaderboardEntry> Results { get; set; }
        [JsonPropertyName("entriesNotFoundForPlayerIds")] public List<string> EntriesNotFoundForPlayerIds { get; set; }
    }

    public class PlayerRangeResponse
    {
        [JsonPropertyName("version")] public ArchivedVersion Version { get; set; }
        [JsonPropertyName("results")] public List<LeaderboardEntry> Results { get; set; }
    }

    public class ArchivedVersion
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("start")] public DateTime Start { get; set; }
        [JsonPropertyName("end")] public DateTime End { get; set; }
    }

    public class PaginatedVersionsResponse
    {
        [JsonPropertyName("leaderboardId")] public string LeaderboardId { get; set; }
        [JsonPropertyName("nextReset")] public DateTime NextReset { get; set; }
        [JsonPropertyName("versionId")] public string VersionId { get; set; }
        [JsonPropertyName("totalArchivedVersions")] public int TotalArchivedVersions { get; set; }
        [JsonPropertyName("results")] public List<ArchivedVersion> Results { get; set; }
    }

    public class BucketInfo
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    /// <summary>
    /// Full Leaderboards v1 API client covering both Player and Admin operations.
    /// Uses the same HttpClient / JSON pattern as other services.
    /// </summary>
    public class API
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        public string ProjectId { get; set; }

        /// <summary>Initialises the client with a Project ID and Bearer token.</summary>
        /// <param name="projectId">The Unity Project ID.</param>
        /// <param name="bearerToken">Unity authentication idToken (or service token for admin endpoints).</param>
        public API(string projectId, string bearerToken)
        {
            ProjectId = projectId;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

            _jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>Submit a new score for a player on a leaderboard.</summary>
        public async Task<PlayerScoreResponse> AddPlayerScoreAsync(
            string leaderboardId, string playerId, AddPlayerScoreRequest request)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/scores/players/{playerId}";
            return await PostAsync<PlayerScoreResponse>(url, request);
        }

        /// <summary>Retrieve the latest score and rank for a player on a leaderboard.</summary>
        public async Task<PlayerScoreResponse> GetPlayerScoreAsync(
            string leaderboardId, string playerId, bool includeMetadata = false)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/scores/players/{playerId}";
            if (includeMetadata) url += "?includeMetadata=true";
            return await GetAsync<PlayerScoreResponse>(url);
        }

        /// <summary>Get a paginated list of scores for a leaderboard.</summary>
        public async Task<PaginatedScoresResponse> GetScoresAsync(
            string leaderboardId, GetScoresRequest request = null)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/scores";
            string query = BuildQueryString(request);
            return await GetAsync<PaginatedScoresResponse>($"{url}?{query}");
        }

        /// <summary>Get scores for a specific tier on a leaderboard.</summary>
        public async Task<PaginatedScoresResponse> GetScoresByTierAsync(
            string leaderboardId, string tierId, GetScoresRequest request = null)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/tiers/{tierId}/scores";
            string query = BuildQueryString(request);
            return await GetAsync<PaginatedScoresResponse>($"{url}?{query}");
        }

        /// <summary>Get scores for a specific list of players on a leaderboard.</summary>
        public async Task<PlayerIdsResponse> GetScoresByPlayerIdsAsync(
            string leaderboardId, List<string> playerIds, bool includeMetadata = false)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/scores/players";
            if (includeMetadata) url += "?includeMetadata=true";

            var request = new PlayerIdsRequest { PlayerIds = playerIds };
            return await PostAsync<PlayerIdsResponse>(url, request);
        }

        /// <summary>Get a player's score and their immediate neighbours.</summary>
        public async Task<PlayerRangeResponse> GetPlayerRangeAsync(
            string leaderboardId, string playerId, int rangeLimit = 5, bool includeMetadata = false)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/scores/players/{playerId}/range";
            url += $"?rangeLimit={rangeLimit}&includeMetadata={includeMetadata.ToString().ToLower()}";
            return await GetAsync<PlayerRangeResponse>(url);
        }

        /// <summary>List all archived (reset) versions of a leaderboard.</summary>
        public async Task<PaginatedVersionsResponse> GetArchivedVersionsAsync(
            string leaderboardId, int limit = 10)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/versions?limit={limit}";
            return await GetAsync<PaginatedVersionsResponse>(url);
        }

        /// <summary>Get scores from a specific archived version of a leaderboard.</summary>
        public async Task<PaginatedScoresResponse> GetArchivedVersionScoresAsync(
            string leaderboardId, string versionId, GetScoresRequest request = null)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/versions/{versionId}/scores";
            string query = BuildQueryString(request);
            return await GetAsync<PaginatedScoresResponse>($"{url}?{query}");
        }

        /// <summary>Get scores for a specific tier in an archived leaderboard.</summary>
        public async Task<PaginatedScoresResponse> GetArchivedVersionScoresByTierAsync(
            string leaderboardId, string versionId, string tierId, GetScoresRequest request = null)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/versions/{versionId}/tiers/{tierId}/scores";
            string query = BuildQueryString(request);
            return await GetAsync<PaginatedScoresResponse>($"{url}?{query}");
        }

        /// <summary>Get scores for a specific list of players on an archived leaderboard version.</summary>
        public async Task<PlayerIdsResponse> GetArchivedVersionScoresByPlayerIdsAsync(
            string leaderboardId, string versionId, List<string> playerIds, bool includeMetadata = false)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/versions/{versionId}/scores/players";
            if (includeMetadata) url += "?includeMetadata=true";

            var request = new PlayerIdsRequest { PlayerIds = playerIds };
            return await PostAsync<PlayerIdsResponse>(url, request);
        }

        /// <summary>Get a specific player's score from an archived version.</summary>
        public async Task<PlayerScoreResponse> GetArchivedVersionPlayerScoreAsync(
            string leaderboardId, string versionId, string playerId, bool includeMetadata = false)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/versions/{versionId}/scores/players/{playerId}";
            if (includeMetadata) url += "?includeMetadata=true";
            return await GetAsync<PlayerScoreResponse>(url);
        }

        /// <summary>Get a player's range from an archived leaderboard version.</summary>
        public async Task<PlayerRangeResponse> GetArchivedVersionPlayerRangeAsync(
            string leaderboardId, string versionId, string playerId, int rangeLimit = 5, bool includeMetadata = false)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/versions/{versionId}/scores/players/{playerId}/range";
            url += $"?rangeLimit={rangeLimit}&includeMetadata={includeMetadata.ToString().ToLower()}";
            return await GetAsync<PlayerRangeResponse>(url);
        }

        /// <summary>Create a new leaderboard configuration (admin).</summary>
        public async Task<LeaderboardConfig> CreateLeaderboardAsync(
            CreateLeaderboardRequest request)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards";
            return await PostAsync<LeaderboardConfig>(url, request);
        }

        /// <summary>Delete a leaderboard configuration (admin).</summary>
        public async Task<bool> DeleteLeaderboardAsync(string leaderboardId)
        {
            var response = await _client.DeleteAsync($"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}");
            return response.IsSuccessStatusCode;
        }

        /// <summary>Get a leaderboard's configuration (admin).</summary>
        public async Task<LeaderboardConfig> GetLeaderboardConfigAsync(string leaderboardId)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}";
            return await GetAsync<LeaderboardConfig>(url);
        }

        /// <summary>List all leaderboard configurations for the project (admin).</summary>
        public async Task<List<LeaderboardConfig>> GetLeaderboardConfigsAsync()
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards";
            return await GetAsync<List<LeaderboardConfig>>(url);
        }

        /// <summary>Delete a specific player's score from a leaderboard (admin).</summary>
        public async Task<bool> DeletePlayerScoreAsync(string leaderboardId, string playerId)
        {
            var response = await _client.DeleteAsync(
                $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/scores/{playerId}");
            return response.IsSuccessStatusCode;
        }

        /// <summary>Delete all scores for a player across all live leaderboards (admin).</summary>
        public async Task<bool> DeletePlayerScoreFromAllAsync(string playerId)
        {
            var response = await _client.DeleteAsync(
                $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/scores/{playerId}");
            return response.IsSuccessStatusCode;
        }

        /// <summary>Get scores as admin (same as player but may include extra info).</summary>
        public async Task<PaginatedScoresResponse> AdminGetScoresAsync(
            string leaderboardId, GetScoresRequest request = null)
        {
            return await GetScoresAsync(leaderboardId, request);
        }

        /// <summary>Get scores for a specific bucket on a leaderboard (admin).</summary>
        public async Task<PaginatedScoresResponse> GetBucketScoresAsync(
            string leaderboardId, string bucketId, GetScoresRequest request = null)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/buckets/{bucketId}/scores";
            string query = BuildQueryString(request);
            return await GetAsync<PaginatedScoresResponse>($"{url}?{query}");
        }

        /// <summary>Get scores for a specific tier within a bucket (admin).</summary>
        public async Task<PaginatedScoresResponse> GetBucketScoresByTierAsync(
            string leaderboardId, string bucketId, string tierId, GetScoresRequest request = null)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/buckets/{bucketId}/scores/tiers/{tierId}";
            string query = BuildQueryString(request);
            return await GetAsync<PaginatedScoresResponse>($"{url}?{query}");
        }

        /// <summary>List all bucket IDs for a leaderboard (admin).</summary>
        public async Task<List<BucketInfo>> GetBucketIdsAsync(string leaderboardId)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/buckets";
            return await GetAsync<List<BucketInfo>>(url);
        }

        /// <summary>Get scores from a specific bucket in an archived version (admin).</summary>
        public async Task<PaginatedScoresResponse> GetVersionBucketScoresAsync(
            string leaderboardId, string versionId, string bucketId, GetScoresRequest request = null)
        {
            var url = $"{Globals.BaseUrlLeaderboard}projects/{ProjectId}/leaderboards/{leaderboardId}/versions/{versionId}/buckets/{bucketId}/scores";
            string query = BuildQueryString(request);
            return await GetAsync<PaginatedScoresResponse>($"{url}?{query}");
        }

        /// <summary>Get a player's score from a specific version (admin may use this too).</summary>
        public async Task<PlayerScoreResponse> AdminGetVersionPlayerScoreAsync(
            string leaderboardId, string versionId, string playerId)
        {
            return await GetArchivedVersionPlayerScoreAsync(leaderboardId, versionId, playerId);
        }

        private async Task<T> GetAsync<T>(string url)
        {
            var response = await _client.GetAsync(url);
            return await ProcessResponse<T>(response);
        }

        private async Task<T> PostAsync<T>(string url, object body)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(body, _jsonOptions),
                Encoding.UTF8,
                "application/json");
            var response = await _client.PostAsync(url, content);
            return await ProcessResponse<T>(response);
        }

        private async Task<T> ProcessResponse<T>(HttpResponseMessage response)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);

            Console.WriteLine($"Leaderboard API error: {response.StatusCode} - {jsonString}");
            return default;
        }

        private static string BuildQueryString(GetScoresRequest request)
        {
            if (request == null) return "";
            var queryParams = new List<string>();
            if (request.Limit > 0) queryParams.Add($"limit={request.Limit}");
            if (request.Offset > 0) queryParams.Add($"offset={request.Offset}");
            if (request.IncludeMetadata) queryParams.Add("includeMetadata=true");
            return string.Join("&", queryParams);
        }
    }
}
