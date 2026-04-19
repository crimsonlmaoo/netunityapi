using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NetUnityApi.Responses.Multiplayer.Lobby;

namespace NetUnityApi.Multiplayer.Lobby.ClientAPI
{
    public class CreateLobbyRequest
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("maxPlayers")] public int MaxPlayers { get; set; }
        [JsonPropertyName("isPrivate")] public bool IsPrivate { get; set; }
        [JsonPropertyName("password")] public string Password { get; set; }
        [JsonPropertyName("data")] public Dictionary<string, LobbyDataObject> Data { get; set; }
        [JsonPropertyName("player")] public LobbyPlayer Player { get; set; }
    }

    public class JoinLobbyRequest
    {
        [JsonPropertyName("lobbyCode")] public string LobbyCode { get; set; }
        [JsonPropertyName("password")] public string Password { get; set; }
        [JsonPropertyName("player")] public LobbyPlayer Player { get; set; }
    }

    public class QueryLobbiesRequest
    {
        [JsonPropertyName("count")] public int Count { get; set; } = 10;
        [JsonPropertyName("skip")] public int Skip { get; set; } = 0;
        [JsonPropertyName("sampleResults")] public bool SampleResults { get; set; } = false;
    }

    public class API
    {
        // other scripts had the worst code so i decided to clean it up with some lambda functions and some intialization for if you wanted to spam join parties and such.
        private readonly HttpClient _client;
        private const string BaseUrl = "https://lobby.services.api.unity.com/v1";
        private readonly JsonSerializerOptions _jsonOptions;

        public API(string bearerToken)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

            _jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<LobbyResponse> GetLobbyAsync(string lobbyId) =>
            await ProcessResponse<LobbyResponse>(await _client.GetAsync($"{BaseUrl}/{lobbyId}"));

        public async Task<LobbyResponse> CreateLobbyAsync(CreateLobbyRequest request) =>
            await ProcessResponse<LobbyResponse>(await _client.PostAsync($"{BaseUrl}/create", CreateJsonContent(request)));

        public async Task<QueryResponse> QueryLobbiesAsync(QueryLobbiesRequest request) =>
            await ProcessResponse<QueryResponse>(await _client.PostAsync($"{BaseUrl}/query", CreateJsonContent(request)));

        public async Task<LobbyResponse> JoinLobbyByIdAsync(string lobbyId, JoinLobbyRequest request) =>
            await ProcessResponse<LobbyResponse>(await _client.PostAsync($"{BaseUrl}/{lobbyId}/join", CreateJsonContent(request)));

        public async Task<LobbyResponse> JoinLobbyByCodeAsync(JoinLobbyRequest request) =>
            await ProcessResponse<LobbyResponse>(await _client.PostAsync($"{BaseUrl}/joinbycode", CreateJsonContent(request)));

        public async Task<bool> HeartbeatLobbyAsync(string lobbyId) =>
            (await _client.PostAsync($"{BaseUrl}/{lobbyId}/heartbeat", new StringContent("{}", Encoding.UTF8, "application/json"))).IsSuccessStatusCode;

        public async Task<bool> DeleteLobbyAsync(string lobbyId) =>
            (await _client.DeleteAsync($"{BaseUrl}/{lobbyId}")).IsSuccessStatusCode;

        private StringContent CreateJsonContent(object obj) =>
            new StringContent(JsonSerializer.Serialize(obj, _jsonOptions), Encoding.UTF8, "application/json");

        private async Task<T> ProcessResponse<T>(HttpResponseMessage response)
        {
            var jsonString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);

            Console.WriteLine($"Api error: {response.StatusCode} - {jsonString}");
            return default;
        }
    }
}