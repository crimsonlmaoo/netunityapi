using NetUnityApi.Responses.Multiplayer.Friends;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NetUnityApi.Multiplayer.Friends.ClientAPI.Relationships
{
    public class API
    {

        private static string APIURL = "https://social.services.api.unity.com/v1/relationships/";
        public static async Task<List<RelationshipResponse>> RetrieveRelationshipList(string token)
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
                        Console.WriteLine("Retrieved relationships");

                        string jsonResponse = await response.Content.ReadAsStringAsync();

                        List<RelationshipResponse> relationships = JsonSerializer.Deserialize<List<RelationshipResponse>>(jsonResponse);

                        return relationships;
                    }
                    else
                    {
                        Console.WriteLine($"Error when retrieving: {response.StatusCode}");
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
        public static async Task<RelationshipResponse> CreateRelationship(string token, string userId)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.Write("Account idToken empty!");
                return null;
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new CreateFriendRequest
                {
                    Members = new List<MemberRequest>
                    {
                        new MemberRequest { Id = userId }
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(APIURL, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Friend request sent");

                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    RelationshipResponse relationship = JsonSerializer.Deserialize<RelationshipResponse>(jsonResponse);

                    return relationship;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed sending request: {response.StatusCode} - {error}");
                }
            }

            return null;
        }
        public static async Task DeleteRelationship(string token, string relationshipId)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.Write("Account idToken empty!");
                return;
            }

            using (HttpClient client = new HttpClient())
            { 
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response = await client.DeleteAsync(APIURL + relationshipId);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Friend request deleted");
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed sending request: {response.StatusCode} - {error}");
                }
            }
        }
    }
}
