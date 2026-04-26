using NetUnityApi.Responses.Multiplayer.Friends;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NetUnityApi.Multiplayer.Friends.ClientAPI.Messaging
{
    public class API
    {

        public static async Task SendMessage(string token, string id, string msgPayload)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.Write("Account idToken empty!");
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new InviteResponse
                {
                    Id = id,
                    Message = new InviteMessage()
                    {
                        LobbyDeeplink = msgPayload //can't be under 10kb
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{Globals.BaseUrlSocial}/messaging/", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Message sent");
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Message send failed: {response.StatusCode} - {error}");
                }
            }

            return;
        }
    }
}
