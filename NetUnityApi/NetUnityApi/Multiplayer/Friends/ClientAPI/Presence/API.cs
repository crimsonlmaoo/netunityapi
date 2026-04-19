using NetUnityApi.Responses.Multiplayer.Friends;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NetUnityApi.Multiplayer.Friends.ClientAPI.Presence
{
    public enum ActivityType
    {
        ONLINE = 0,
        BUSY = 1, 
        AWAY = 2,
        INVISIBLE = 3,
        OFFLINE = 4
    }

    public class API
    {
        private static string APIURL = "https://social.services.api.unity.com/v1/presence";
        public static async Task<PresenceResponse> GetPresence(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Account idToken empty!");
                return null;
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

                try
                {
                    HttpResponseMessage response = await client.GetAsync(APIURL);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Retrieved presence");

                        string jsonResponse = await response.Content.ReadAsStringAsync();

                        PresenceResponse presence = JsonSerializer.Deserialize<PresenceResponse>(jsonResponse);

                        return presence;
                    }
                    else
                    {
                        Console.WriteLine($"Error when getting presence: {response.StatusCode}");
                        string errorDetail = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(errorDetail);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"exc: {ex.Message}");
                }
            }

            return null;
        }
        public static async Task<PresenceResponse> SetPresence(string token, ActivityType type, PlayerActivity activity)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.Write("Account idToken empty!");
                return null;
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new PresenceResponse
                {
                    Activity = activity, //customvalues = new Dictionary<string, object>() i hate this code
                    Availability = Enum.GetName(typeof(ActivityType), type)
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(APIURL, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Presence set");

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    PresenceResponse presence = JsonSerializer.Deserialize<PresenceResponse>(jsonResponse, options);

                    return presence;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed setting presence: {response.StatusCode} - {error}");
                }
            }

            return null;
        }
    }
}
