using NetUnityApi.Responses.Multiplayer.Friends;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NetUnityApi.Multiplayer.Friends.ClientAPI.Notifications
{
    public class API
    {
        public static string APIURL = "https://social.services.api.unity.com/v1/notifications/auth";

        public static async Task<NotificationsResponse> GetNotificationAuthDetails(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.Write("Account idToken empty!");
                return null;
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response = await client.GetAsync(APIURL);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Got notifications");

                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    NotificationsResponse notification = JsonSerializer.Deserialize<NotificationsResponse>(jsonResponse);

                    return notification;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed getting notifications: {response.StatusCode} - {error}");
                }
            }

            return null;
        }
    }
}
