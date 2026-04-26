using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NetUnityApi.Responses.Multiplayer.Lobby;
using NetUnityApi;

namespace NetUnityApi.Multiplayer.Lobby.ClientAPI
{
    //let deepseek create summaries for each class and function, pretty nice!

    /// <summary>Request body for creating a new lobby.</summary>
    public class CreateLobbyRequest
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("maxPlayers")] public int MaxPlayers { get; set; }
        [JsonPropertyName("isPrivate")] public bool IsPrivate { get; set; }
        [JsonPropertyName("isLocked")] public bool IsLocked { get; set; }
        [JsonPropertyName("password")] public string Password { get; set; }
        [JsonPropertyName("data")] public Dictionary<string, LobbyDataObject> Data { get; set; }
        [JsonPropertyName("player")] public LobbyPlayer Player { get; set; }
    }

    /// <summary>Request body for joining a lobby using a short lobby code.</summary>
    public class JoinByCodeRequest
    {
        [JsonPropertyName("lobbyCode")] public string LobbyCode { get; set; }
        [JsonPropertyName("password")] public string Password { get; set; }
        [JsonPropertyName("player")] public LobbyPlayer Player { get; set; }
    }

    /// <summary>Request body for joining a lobby directly by its ID.</summary>
    public class JoinByIdRequest
    {
        [JsonPropertyName("password")] public string Password { get; set; }
        [JsonPropertyName("player")] public LobbyPlayer Player { get; set; }
    }

    /// <summary>Request body for the Quick Join endpoint (query + join random lobby).</summary>
    public class QuickJoinRequest
    {
        [JsonPropertyName("filter")] public List<QueryFilter> Filter { get; set; }
        [JsonPropertyName("player")] public LobbyPlayer Player { get; set; }
    }

    /// <summary>A filter object used in lobby queries and Quick Join.</summary>
    public class QueryFilter
    {
        [JsonPropertyName("field")] public string Field { get; set; }
        [JsonPropertyName("op")] public string Op { get; set; }       // EQ, NE, GT, GE, LT, LE, CONTAINS
        [JsonPropertyName("value")] public string Value { get; set; }
    }

    /// <summary>Request body for querying public lobbies.</summary>
    public class QueryRequest
    {
        [JsonPropertyName("count")] public int Count { get; set; } = 10;
        [JsonPropertyName("skip")] public int Skip { get; set; } = 0;
        [JsonPropertyName("sampleResults")] public bool SampleResults { get; set; } = false;
        [JsonPropertyName("filter")] public List<QueryFilter> Filter { get; set; }
        [JsonPropertyName("order")] public List<QueryOrder> Order { get; set; }
        [JsonPropertyName("continuationToken")] public string ContinuationToken { get; set; }
    }

    /// <summary>Sorting criteria for lobby queries.</summary>
    public class QueryOrder
    {
        [JsonPropertyName("field")] public string Field { get; set; }
        [JsonPropertyName("asc")] public bool Asc { get; set; }
    }

    /// <summary>Request body for updating an existing lobby.</summary>
    public class UpdateLobbyRequest
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("maxPlayers")] public int? MaxPlayers { get; set; }
        [JsonPropertyName("isPrivate")] public bool? IsPrivate { get; set; }
        [JsonPropertyName("isLocked")] public bool? IsLocked { get; set; }
        [JsonPropertyName("password")] public string Password { get; set; }
        [JsonPropertyName("hostId")] public string HostId { get; set; }
        [JsonPropertyName("data")] public Dictionary<string, LobbyDataObject> Data { get; set; }
    }

    /// <summary>Request body for updating a player’s data inside a lobby.</summary>
    public class UpdatePlayerRequest
    {
        [JsonPropertyName("data")] public Dictionary<string, LobbyDataObject> Data { get; set; }
        [JsonPropertyName("connectionInfo")] public string ConnectionInfo { get; set; }
        [JsonPropertyName("allocationId")] public string AllocationId { get; set; }
    }

    /// <summary>
    /// Full Lobby v1 API client. Provides all endpoints defined in the Unity Lobby service documentation.
    /// </summary>
    public class API
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>Initialises the lobby client with a Bearer token.</summary>
        /// <param name="bearerToken">A valid Unity authentication idToken.</param>
        public API(string bearerToken)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

            _jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        private void AddOptionalHeaders(string serviceId = null, string impersonatedUserId = null,
                                        string ifMatch = null, string ifNoneMatch = null)
        {
            if (!string.IsNullOrEmpty(serviceId))
                _client.DefaultRequestHeaders.Add("service-id", serviceId);
            if (!string.IsNullOrEmpty(impersonatedUserId))
                _client.DefaultRequestHeaders.Add("impersonated-user-id", impersonatedUserId);
            if (!string.IsNullOrEmpty(ifMatch))
                _client.DefaultRequestHeaders.Add("if-match", ifMatch);
            if (!string.IsNullOrEmpty(ifNoneMatch))
                _client.DefaultRequestHeaders.Add("if-none-match", ifNoneMatch);
        }

        private void ClearOptionalHeaders()
        {
            _client.DefaultRequestHeaders.Remove("service-id");
            _client.DefaultRequestHeaders.Remove("impersonated-user-id");
            _client.DefaultRequestHeaders.Remove("if-match");
            _client.DefaultRequestHeaders.Remove("if-none-match");
        }

        /// <summary>Queries public lobbies with optional filters, ordering, and pagination.</summary>
        /// <param name="request">Query parameters and filters.</param>
        /// <param name="serviceId">Optional service account ID for Unity Game Services.</param>
        /// <param name="impersonatedUserId">Optional user ID to impersonate.</param>
        /// <returns>A <see cref="QueryResponse"/> containing matching lobbies and a continuation token.</returns>
        public async Task<QueryResponse> QueryLobbiesAsync(QueryRequest request,
            string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId);
            var result = await ProcessResponse<QueryResponse>(
                await _client.PostAsync($"{Globals.BaseUrlLobby}/query", CreateJsonContent(request)));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Creates a new lobby with the given settings.</summary>
        /// <param name="request">Lobby creation details.</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>The newly created lobby’s state.</returns>
        public async Task<LobbyResponse> CreateLobbyAsync(CreateLobbyRequest request,
            string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId);
            var result = await ProcessResponse<LobbyResponse>(
                await _client.PostAsync($"{Globals.BaseUrlLobby}/create", CreateJsonContent(request)));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Joins an existing lobby using a short lobby code.</summary>
        /// <param name="request">Join details including lobby code.</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>The lobby state after joining.</returns>
        public async Task<LobbyResponse> JoinLobbyByCodeAsync(JoinByCodeRequest request,
            string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId);
            var result = await ProcessResponse<LobbyResponse>(
                await _client.PostAsync($"{Globals.BaseUrlLobby}/joinbycode", CreateJsonContent(request)));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Joins an existing lobby directly by its ID.</summary>
        /// <param name="lobbyId">The unique lobby ID.</param>
        /// <param name="request">Join details (password, player data).</param>
        /// <param name="ifMatch">Optional ETag for optimistic concurrency.</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>The lobby state after joining.</returns>
        public async Task<LobbyResponse> JoinLobbyByIdAsync(string lobbyId, JoinByIdRequest request,
            string ifMatch = null, string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId, ifMatch);
            var result = await ProcessResponse<LobbyResponse>(
                await _client.PostAsync($"{Globals.BaseUrlLobby}/{lobbyId}/join", CreateJsonContent(request)));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Creates a lobby with the given ID if it does not exist, or joins it if it already exists.</summary>
        /// <param name="lobbyId">The desired lobby ID.</param>
        /// <param name="request">Lobby creation details (used only if creating).</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>The lobby state after the operation.</returns>
        public async Task<LobbyResponse> CreateOrJoinLobbyAsync(string lobbyId, CreateLobbyRequest request,
            string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId);
            var result = await ProcessResponse<LobbyResponse>(
                await _client.PostAsync($"{Globals.BaseUrlLobby}/{lobbyId}/createorjoin", CreateJsonContent(request)));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Reconnects the authenticated user to a lobby they were previously a member of.</summary>
        /// <param name="lobbyId">The ID of the lobby to reconnect to.</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>The lobby state after reconnecting.</returns>
        public async Task<LobbyResponse> ReconnectToLobbyAsync(string lobbyId,
            string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId);
            var result = await ProcessResponse<LobbyResponse>(
                await _client.PostAsync($"{Globals.BaseUrlLobby}/{lobbyId}/reconnect", null));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Finds a random public lobby matching optional filters and joins it immediately.</summary>
        /// <param name="request">Filter criteria and player data.</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>The lobby that was joined.</returns>
        public async Task<LobbyResponse> QuickJoinAsync(QuickJoinRequest request,
            string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId);
            var result = await ProcessResponse<LobbyResponse>(
                await _client.PostAsync($"{Globals.BaseUrlLobby}/quickjoin", CreateJsonContent(request)));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Retrieves full details about a lobby.</summary>
        /// <param name="lobbyId">Lobby ID.</param>
        /// <param name="ifNoneMatch">Optional ETag for caching (returns 304 if unchanged).</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>The lobby state.</returns>
        public async Task<LobbyResponse> GetLobbyAsync(string lobbyId,
            string ifNoneMatch = null, string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId, ifNoneMatch: ifNoneMatch);
            var result = await ProcessResponse<LobbyResponse>(
                await _client.GetAsync($"{Globals.BaseUrlLobby}/{lobbyId}"));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Deletes the specified lobby (host only).</summary>
        /// <param name="lobbyId">Lobby ID to delete.</param>
        /// <param name="ifMatch">Optional ETag to avoid accidentally deleting a modified lobby.</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>True if deletion succeeded.</returns>
        public async Task<bool> DeleteLobbyAsync(string lobbyId,
            string ifMatch = null, string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId, ifMatch);
            var response = await _client.DeleteAsync($"{Globals.BaseUrlLobby}/{lobbyId}");
            ClearOptionalHeaders();
            return response.IsSuccessStatusCode;
        }

        /// <summary>Updates properties of an existing lobby (host only).</summary>
        /// <param name="lobbyId">Lobby ID.</param>
        /// <param name="request">Fields to change (null fields are left unchanged).</param>
        /// <param name="ifMatch">Optional ETag for concurrency control.</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>The updated lobby state.</returns>
        public async Task<LobbyResponse> UpdateLobbyAsync(string lobbyId, UpdateLobbyRequest request,
            string ifMatch = null, string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId, ifMatch);
            var result = await ProcessResponse<LobbyResponse>(
                await _client.PatchAsync($"{Globals.BaseUrlLobby}/{lobbyId}", CreateJsonContent(request)));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Kicks a player from a lobby (host only).</summary>
        /// <param name="lobbyId">Lobby ID.</param>
        /// <param name="playerId">ID of the player to remove.</param>
        /// <param name="ifMatch">Optional ETag for concurrency control.</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>True if the player was removed.</returns>
        public async Task<bool> RemovePlayerAsync(string lobbyId, string playerId,
            string ifMatch = null, string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId, ifMatch);
            var response = await _client.DeleteAsync(
                $"{Globals.BaseUrlLobby}/{lobbyId}/players/{playerId}");
            ClearOptionalHeaders();
            return response.IsSuccessStatusCode;
        }

        /// <summary>Updates the authenticated player's data inside a lobby.</summary>
        /// <param name="lobbyId">Lobby ID.</param>
        /// <param name="playerId">Player ID (must match the authenticated user).</param>
        /// <param name="request">New data and optional allocation/connection info.</param>
        /// <param name="ifMatch">Optional ETag for concurrency control.</param>
        /// <param name="serviceId">Optional service account ID.</param>
        /// <param name="impersonatedUserId">Optional impersonated user ID.</param>
        /// <returns>The lobby state after the player update.</returns>
        public async Task<LobbyResponse> UpdatePlayerAsync(string lobbyId, string playerId,
            UpdatePlayerRequest request,
            string ifMatch = null, string serviceId = null, string impersonatedUserId = null)
        {
            AddOptionalHeaders(serviceId, impersonatedUserId, ifMatch);
            var result = await ProcessResponse<LobbyResponse>(
                await _client.PatchAsync($"{Globals.BaseUrlLobby}/{lobbyId}/players/{playerId}",
                    CreateJsonContent(request)));
            ClearOptionalHeaders();
            return result;
        }

        /// <summary>Sends a heartbeat ping to keep the player active in the lobby.</summary>
        /// <param name="lobbyId">Lobby ID to ping.</param>
        /// <returns>True if the heartbeat was acknowledged.</returns>
        public async Task<bool> HeartbeatLobbyAsync(string lobbyId)
        {
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlLobby}/{lobbyId}/heartbeat",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        private StringContent CreateJsonContent(object obj) =>
            new StringContent(JsonSerializer.Serialize(obj, _jsonOptions),
                Encoding.UTF8, "application/json");

        private async Task<T> ProcessResponse<T>(HttpResponseMessage response)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);

            Console.WriteLine($"API error: {response.StatusCode} - {jsonString}");
            return default;
        }
    }
}