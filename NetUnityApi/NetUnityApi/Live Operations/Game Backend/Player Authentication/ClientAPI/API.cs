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

    public class PlayerInfoResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("disabled")] public bool Disabled { get; set; }
        [JsonPropertyName("externalIds")] public List<ExternalId> ExternalIds { get; set; }
        [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    }

    public class ExternalId
    {
        [JsonPropertyName("provider")] public string Provider { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
    }

    public class Notification
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; }
        [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    }
}

namespace NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.ClientAPI
{
    using NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.Models;

    public class API
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public API(string projectId, string environment = "production")
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("ProjectId", projectId);
            if (!string.IsNullOrEmpty(environment))
                _client.DefaultRequestHeaders.Add("UnityEnvironment", environment);

            _jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<AuthResponse> SignInAnonymousAsync(string nonce = null)
        {
            var body = new { nonce };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/anonymous",
                CreateJsonContent(body));
            var auth = await ProcessResponse<AuthResponse>(response);
            
            return auth;
        }

        public async Task<AuthResponse> SignInWithExternalTokenAsync(
            string idProvider, ExternalTokenRequest request)
        {
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/external-token/{idProvider}",
                CreateJsonContent(request));
            var auth = await ProcessResponse<AuthResponse>(response);
            
            return auth;
        }

        public async Task<AuthResponse> SignInWithSessionTokenAsync(string sessionToken)
        {
            var body = new { sessionToken };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/session-token",
                CreateJsonContent(body));
            var auth = await ProcessResponse<AuthResponse>(response);
            
            return auth;
        }

        public async Task<AuthResponse> SignUpWithUsernamePasswordAsync(
            string username, string password)
        {
            var body = new { username, password };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/usernamepassword/sign-up",
                CreateJsonContent(body));
            var auth = await ProcessResponse<AuthResponse>(response);
            
            return auth;
        }

        public async Task<AuthResponse> SignInWithUsernamePasswordAsync(
            string username, string password)
        {
            var body = new { username, password };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/usernamepassword/sign-in",
                CreateJsonContent(body));
            var auth = await ProcessResponse<AuthResponse>(response);
            
            return auth;
        }

        public async Task<AuthResponse> SignInWithCustomIdAsync(
            string externalId, bool signInOnly = false, string accessToken = null)
        {
            var body = new { externalId, signInOnly, accessToken };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/custom-id",
                CreateJsonContent(body));
            var auth = await ProcessResponse<AuthResponse>(response);
            
            return auth;
        }
        public async Task<AuthResponse> LinkExternalIdAsync(
            string idProvider, string token, bool forceLink = false)
        {
            var body = new { token, forceLink };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/external/{idProvider}/link",
                CreateJsonContent(body));
            return await ProcessResponse<AuthResponse>(response);
        }

        public async Task<AuthResponse> UnlinkExternalIdAsync(
            string idProvider, string externalId)
        {
            var body = new { externalId };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/external/{idProvider}/unlink",
                CreateJsonContent(body));
            return await ProcessResponse<AuthResponse>(response);
        }

        public async Task<AuthResponse> UpdatePasswordAsync(
            string password, string newPassword)
        {
            var body = new { password, newPassword };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/usernamepassword/update-password",
                CreateJsonContent(body));
            return await ProcessResponse<AuthResponse>(response);
        }

        public async Task<PlayerInfoResponse> GetPlayerAsync(string playerId = null)
        {
            var id = playerId;
            var response = await _client.GetAsync(
                $"{Globals.BaseUrlPlayerAuth}/users/{id}");
            return await ProcessResponse<PlayerInfoResponse>(response);
        }

        public async Task DeletePlayerAsync(string playerId = null)
        {
            var id = playerId;
            var response = await _client.DeleteAsync(
                $"{Globals.BaseUrlPlayerAuth}/users/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<Notification>> GetNotificationsAsync(string playerId = null)
        {
            var id = playerId;
            var response = await _client.GetAsync(
                $"{Globals.BaseUrlPlayerAuth}/users/{id}/notifications");
            return await ProcessResponse<List<Notification>>(response);
        }
        public async Task<object> GenerateCodeLinkAsync(
            string codeChallenge, string identifier)
        {
            var body = new { codeChallenge, identifier };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/code-link/generate",
                CreateJsonContent(body));
            return await ProcessResponse<object>(response);
        }

        public async Task<object> SignInWithCodeAsync(
            string sessionId, string codeVerifier)
        {
            var body = new { codeVerifier };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/code-link/sign-in/{sessionId}",
                CreateJsonContent(body));
            return await ProcessResponse<object>(response);
        }

        public async Task<object> GetCodeInfoAsync(string signInCode)
        {
            var body = new { signInCode };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/code-link/info",
                CreateJsonContent(body));
            return await ProcessResponse<object>(response);
        }

        public async Task<object> ConfirmCodeAsync(
            string signInCode, string sessionToken,
            string idProvider = null, string externalToken = null)
        {
            var body = new { signInCode, sessionToken, idProvider, externalToken };
            var response = await _client.PostAsync(
                $"{Globals.BaseUrlPlayerAuth}/authentication/code-link/confirm",
                CreateJsonContent(body));
            return await ProcessResponse<object>(response);
        }
        private StringContent CreateJsonContent(object obj)
        {
            var json = JsonSerializer.Serialize(obj, _jsonOptions);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<T> ProcessResponse<T>(HttpResponseMessage response)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);

            Console.WriteLine($"Error {response.StatusCode}: {jsonString}");
            return default;
        }
    }
}