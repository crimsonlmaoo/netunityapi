using NetUnityApi;
using NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsoleApp20
{
    public class UnityApiGlobals
    {
        public string idToken = "";
        public string userId = "";
        public string sessionToken = "";
        public NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.ClientAPI.API PlayerAuthAPI { get; private set; }
        public NetUnityApi.Multiplayer.Lobby.ClientAPI.API LobbyAPI { get; private set; }
        public NetUnityApi.Multiplayer.Name.ClientAPI.API NameAPI { get; private set; }
        public NetUnityApi.Multiplayer.Friends.ClientAPI.Messaging.API MessagingAPI { get; private set; }
        public NetUnityApi.Multiplayer.Friends.ClientAPI.Relationships.API RelationshipAPI { get; private set; }
        public NetUnityApi.Multiplayer.Friends.ClientAPI.Notifications.API NotificationsAPI { get; private set; }
        public NetUnityApi.Multiplayer.Friends.ClientAPI.Presence.API PresenceAPI { get; private set; }
        public NetUnityApi.Live_Operations.Game_Backend.Leaderboard.ClientAPI.API LeaderboardAPI { get; private set; }
        public async Task Init2(string projectId = "77078b1e-f13b-4e91-a33f-52cc74d5bdb8", string sessionToken = "")
        {
            if (sessionToken == null || string.IsNullOrEmpty(sessionToken))
            {
                await Init(projectId);
            }

            PlayerAuthAPI = new NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.ClientAPI.API(projectId);

            try
            {
                Console.WriteLine("Authenticating...");

                var gPid = await PlayerAuthAPI.SignInWithSessionTokenAsync(sessionToken);

                string token = gPid.IdToken;
                userId = gPid.UserId;
                idToken = gPid.IdToken;
                sessionToken = gPid.SessionToken;

                LobbyAPI = new NetUnityApi.Multiplayer.Lobby.ClientAPI.API(idToken);
                NameAPI = new NetUnityApi.Multiplayer.Name.ClientAPI.API(idToken);
                MessagingAPI = new NetUnityApi.Multiplayer.Friends.ClientAPI.Messaging.API();
                RelationshipAPI = new NetUnityApi.Multiplayer.Friends.ClientAPI.Relationships.API();
                NotificationsAPI = new NetUnityApi.Multiplayer.Friends.ClientAPI.Notifications.API();
                PresenceAPI = new NetUnityApi.Multiplayer.Friends.ClientAPI.Presence.API();
                LeaderboardAPI = new NetUnityApi.Live_Operations.Game_Backend.Leaderboard.ClientAPI.API(projectId, token);

                Console.WriteLine("All APIs successfully initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to initialize APIs: {ex.Message}");
                throw;
            }
        }
        public async Task Init(string projectId = "77078b1e-f13b-4e91-a33f-52cc74d5bdb8")
        {
            PlayerAuthAPI = new NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.ClientAPI.API(projectId);

            string token = "";

            AuthResponse auth = null;

            var types = Enum.GetNames(typeof(Globals.AuthType)).ToList();

            var tab = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [yellow]login type[/]")
                    .PageSize(10)
                    .AddChoices(types));

            switch (tab)
            {
                case "Anonymous":
                    auth = await PlayerAuthAPI.SignInAnonymousAsync();
                    break;

                case "SessionToken":
                    var sessionTok = AnsiConsole.Prompt(new TextPrompt<string>("Enter your session token:").Secret());
                    auth = await PlayerAuthAPI.SignInWithSessionTokenAsync(sessionTok);
                    break;

                case "UsernamePassword":
                    var upChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Do you want to sign up or sign in?")
                            .AddChoices("Sign In", "Sign Up"));
                    var username = AnsiConsole.Prompt(
                        new TextPrompt<string>("Username:"));
                    var password = AnsiConsole.Prompt(
                        new TextPrompt<string>("Password:").Secret());

                    if (upChoice == "Sign Up")
                        auth = await PlayerAuthAPI.SignUpWithUsernamePasswordAsync(username, password);
                    else
                        auth = await PlayerAuthAPI.SignInWithUsernamePasswordAsync(username, password);
                    break;

                case "ExternalToken":
                    var providerChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select an identity provider:")
                            .AddChoices(Enum.GetNames(typeof(Globals.ExternalIdProvider))));

                    var provider = Enum.Parse<Globals.ExternalIdProvider>(providerChoice);

                    var extToken = AnsiConsole.Prompt(new TextPrompt<string>($"Enter the {provider} token:").Secret());
                    var signInOnly = AnsiConsole.Prompt(new ConfirmationPrompt("Sign‑in only? (otherwise creates account if needed)"));

                    var extReq = new ExternalTokenRequest
                    {
                        Token = extToken,
                        SignInOnly = signInOnly
                    };
                    auth = await PlayerAuthAPI.SignInWithExternalTokenAsync(provider.ToString(), extReq);
                    break;

                case "CustomId":
                    var customId = AnsiConsole.Prompt(new TextPrompt<string>("Enter your custom ID:"));
                    var customSignInOnly = AnsiConsole.Prompt(new ConfirmationPrompt("Sign‑in only?"));
                    var accessToken = AnsiConsole.Prompt(new TextPrompt<string>("Access token (optional):").AllowEmpty());
                    auth = await PlayerAuthAPI.SignInWithCustomIdAsync(
                        customId, customSignInOnly, string.IsNullOrEmpty(accessToken) ? null : accessToken);
                    break;

                case "CodeLink":
                    var codeChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Code‑link action:")
                            .AddChoices("Generate code", "Sign in with code"));

                    if (codeChoice == "Generate code")
                    {
                        var codeChallenge = AnsiConsole.Prompt(
                            new TextPrompt<string>("Code challenge (base64‑url encoded SHA256 of verifier):"));
                        var identifier = AnsiConsole.Prompt(
                            new TextPrompt<string>("Identifier (e.g., player ID or device ID):"));
                        var genResponse = await PlayerAuthAPI.GenerateCodeLinkAsync(codeChallenge, identifier);
                        Console.WriteLine("Code generated. Give this sign‑in code to the other device:");

                        var code = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(genResponse));
                        Console.WriteLine(code.GetProperty("signInCode").GetString());
                    }
                    else
                    {
                        var sessionId = AnsiConsole.Prompt(
                            new TextPrompt<string>("Session ID (from the code‑link URL):"));
                        var codeVerifier = AnsiConsole.Prompt(
                            new TextPrompt<string>("Code verifier (original string before hashing):"));
                        auth = await PlayerAuthAPI.SignInWithCodeAsync(sessionId, codeVerifier) as AuthResponse;
                    }
                    break;

                default:
                    Console.WriteLine("Unknown authentication type.");
                    return;
            }

            if (auth != null)
            {
                Console.WriteLine("Authenticating...");
                token = auth.IdToken;
                userId = auth.UserId;
                idToken = auth.IdToken;
                sessionToken = auth.SessionToken;

                Console.WriteLine("All APIs successfully initialized.");
            }
            else
            {
                Console.WriteLine("Authentication failed.");
            }

            LobbyAPI = new NetUnityApi.Multiplayer.Lobby.ClientAPI.API(token);
            NameAPI = new NetUnityApi.Multiplayer.Name.ClientAPI.API(token);
            MessagingAPI = new NetUnityApi.Multiplayer.Friends.ClientAPI.Messaging.API();
            RelationshipAPI = new NetUnityApi.Multiplayer.Friends.ClientAPI.Relationships.API();
            NotificationsAPI = new NetUnityApi.Multiplayer.Friends.ClientAPI.Notifications.API();
            PresenceAPI = new NetUnityApi.Multiplayer.Friends.ClientAPI.Presence.API();
            LeaderboardAPI = new NetUnityApi.Live_Operations.Game_Backend.Leaderboard.ClientAPI.API(projectId, token);
        }
    }
}