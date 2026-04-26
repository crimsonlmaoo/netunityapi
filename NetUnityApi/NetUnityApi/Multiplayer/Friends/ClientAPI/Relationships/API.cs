using NetUnityApi.Responses.Multiplayer.Friends;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using NetUnityApi;
using System.Text;
using System.Text.Json;

namespace NetUnityApi.Multiplayer.Friends.ClientAPI.Relationships
{
    public class API
    {
        public async Task<List<RelationshipResponse>> RetrieveRelationshipList(string token)
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
                    HttpResponseMessage response = await client.GetAsync($"{Globals.BaseUrlSocial}/relationships");

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
                    throw new Exception($"exc: {ex.Message}");
                }
            }

            return null;
        }
        public async Task<RelationshipResponse> CreateRelationship(string token, string userId)
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

                HttpResponseMessage response = await client.PostAsync($"{Globals.BaseUrlSocial}/relationships", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Friend request sent");

                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    RelationshipResponse relationship = JsonSerializer.Deserialize<RelationshipResponse>(jsonResponse);

                    Console.WriteLine($"Relationship id: {relationship.Id}");

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
        public async Task DeleteRelationship(string token, string relationshipId)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.Write("Account idToken empty!");
                return;
            }

            using (HttpClient client = new HttpClient())
            { 
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response = await client.DeleteAsync($"{Globals.BaseUrlSocial}/relationships/{relationshipId}");

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
