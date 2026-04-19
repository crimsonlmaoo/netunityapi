using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NetUnityApi.Responses.Multiplayer.Name;

namespace NetUnityApi.Multiplayer.Name.ClientAPI
{

    public class API
    {
        // other scripts had the worst code so i decided to clean it up with some lambda functions and some intialization for if you wanted to spam join parties and such.
        private readonly HttpClient _client;
        private const string BaseUrl = "https://social.services.api.unity.com/v1/names";
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

        public async Task<NameResponse> GetName(string playerId) =>
            await ProcessResponse<NameResponse>(await _client.GetAsync($"{BaseUrl}/{playerId}"));

        public async Task<NameResponse> SetName(NameRequest request, string playerId) =>
            await ProcessResponse<NameResponse>(await _client.PostAsync($"{BaseUrl}/{playerId}", CreateJsonContent(request)));

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