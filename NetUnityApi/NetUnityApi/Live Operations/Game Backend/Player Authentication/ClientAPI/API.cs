using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.Models
{
    public class AuthResponse
    {
        [JsonPropertyName("expiresIn")] public int ExpiresIn { get; set; }
        [JsonPropertyName("idToken")] public string IdToken { get; set; }
        [JsonPropertyName("sessionToken")] public string SessionToken { get; set; }
        [JsonPropertyName("userId")] public string UserId { get; set; }
        [JsonPropertyName("user")] public AuthUser User { get; set; }
    }

    public class AuthUser
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("disabled")] public bool Disabled { get; set; }
    }

    public class ExternalTokenRequest
    {
        [JsonPropertyName("token")] public string Token { get; set; }
        [JsonPropertyName("nonce")] public string Nonce { get; set; }
        [JsonPropertyName("signInOnly")] public bool SignInOnly { get; set; }
        [JsonPropertyName("oculusConfig")] public object OculusConfig { get; set; }
        [JsonPropertyName("appleGameCenterConfig")] public object AppleGameCenterConfig { get; set; }
        [JsonPropertyName("steamConfig")] public object SteamConfig { get; set; }
    }
}

namespace NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.ClientAPI
{
    using NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.Models;

    public class API
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "https://player-auth.services.api.unity.com/v1";
        private readonly JsonSerializerOptions _jsonOptions;

        public API(string projectId, string environment = "production")
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("ProjectId", projectId);
            _client.DefaultRequestHeaders.Add("UnityEnvironment", environment);

            _jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<AuthResponse> SignInAnonymousAsync(string nonce = null) =>
            await ProcessResponse<AuthResponse>(await _client.PostAsync($"{BaseUrl}/authentication/anonymous", CreateJsonContent(new { nonce })));

        public async Task<AuthResponse> SignInWithExternalTokenAsync(string idProvider, ExternalTokenRequest request) =>
            await ProcessResponse<AuthResponse>(await _client.PostAsync($"{BaseUrl}/authentication/external-token/{idProvider}", CreateJsonContent(request)));

        public async Task<AuthResponse> SignInWithSessionTokenAsync(string sessionToken) =>
            await ProcessResponse<AuthResponse>(await _client.PostAsync($"{BaseUrl}/authentication/session-token", CreateJsonContent(new { sessionToken })));

        private StringContent CreateJsonContent(object obj) =>
            new StringContent(JsonSerializer.Serialize(obj, _jsonOptions), Encoding.UTF8, "application/json");

        private async Task<T> ProcessResponse<T>(HttpResponseMessage response)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);

            Console.WriteLine($"Auth Error: {response.StatusCode} - {jsonString}");
            return default;
        }
    }
}