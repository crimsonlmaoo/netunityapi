using NetUnityApi.Live_Operations.Game_Backend.Leaderboard.ClientAPI;
using NetUnityApi.Multiplayer.Lobby.ClientAPI;
using NetUnityApi.Responses.Multiplayer.Lobby;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp20
{
    class Program
    {
        static UnityApiGlobals UAG = new UnityApiGlobals();
        private static bool _isRunning = true;
        private static string _activeLobbyId = null;
        private static CancellationTokenSource _heartbeatCts = null;

        static async Task Main()
        {
            Console.Write("Hello! This is just the startup process");
            Console.Write("\nWould you like to start off with a new account, or login with a sessionToken? (y/n): ");

            if (Console.ReadLine() == "y")
            {
                Console.Clear();
                Console.Write("You've chosen to use a sessionToken to log in. Please enter it here: ");
                string readToken = Console.ReadLine();
                Console.Clear();

                await AnsiConsole.Status()
                    .StartAsync("Initializing Unity APIs...", async ctx =>
                    {
                        await UAG.Init2(sessionToken: readToken);
                        await Task.Delay(2000);
                    });

                await MainLoop();
            }
            else
            {
                await UAG.Init();

                await MainLoop();
            }
        }

        private static async Task MainLoop()
        {
            while (_isRunning)
            {
                AnsiConsole.Clear();

                AnsiConsole.Write(
                    new FigletText("NetUnity Manager")
                        .Color(Color.Cyan1));

                var tab = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a [yellow]Service[/]")
                        .PageSize(10)
                        .AddChoices("Lobby", "Friends", "Relationships", "Profile", "Notifications", "Leaderboards", "Exit"));

                await RenderTabContent(tab);
            }
        }

        private static async Task RenderTabContent(string tab)
        {
            switch (tab)
            {
                case "Lobby":
                    await ShowLobbyTab();
                    break;

                case "Relationships":
                    await ShowRelationshipsTab();
                    break;
                case "Leaderboards":
                    await ShowLeaderboardsTab();
                    break;
                case "Friends":
                    await ShowFriendsTabWrapper();
                    break;

                case "Profile":
                    ShowProfileTab();
                    break;

                case "Notifications":
                    await ShowNotificationsTab();
                    break;

                case "Exit":
                    _isRunning = false;
                    // Stop heartbeat when exiting
                    StopHeartbeat();
                    break;
            }
        }

        private static async Task ShowLobbyTab()
        {
            var tab = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Lobby actions")
                    .PageSize(12)
                    .AddChoices(
                        "Query Lobbies",
                        "Join Lobby",
                        "Fetch Lobby Info",
                        "Create Lobby",
                        "Update Lobby",
                        "Delete Lobby",
                        "Remove Player",
                        "Update My Player",
                        "Quick Join",
                        "Reconnect",
                        "Back"));

            try
            {
                switch (tab)
                {
                    case "Query Lobbies": await QueryLobbies(); break;
                    case "Join Lobby": await JoinLobby(); break;
                    case "Fetch Lobby Info": await FetchLobbyInfo(); break;
                    case "Create Lobby": await CreateLobby(); break;
                    case "Update Lobby": await UpdateLobby(); break;
                    case "Delete Lobby": await DeleteLobby(); break;
                    case "Remove Player": await RemovePlayer(); break;
                    case "Update My Player": await UpdateMyPlayer(); break;
                    case "Quick Join": await QuickJoin(); break;
                    case "Reconnect": await ReconnectToLobby(); break;
                    case "Back": return;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            }

            AnsiConsole.MarkupLine("\n[grey]Press any key to return to menu...[/]");
            Console.ReadKey();
        }

        // Query public lobbies
        private static async Task QueryLobbies()
        {
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Lobby Name (ID)");
            table.AddColumn("Players");

            await AnsiConsole.Status()
                .StartAsync("Querying lobbies...", async ctx =>
                {
                    var response = await UAG.LobbyAPI.QueryLobbiesAsync(
                        new QueryRequest { Count = 20 });

                    if (response?.Results != null && response.Results.Count > 0)
                    {
                        foreach (var lobby in response.Results)
                            table.AddRow(
                                $"{lobby.Name} | {lobby.Id}".EscapeMarkup(),
                                $"{lobby.Players.Count}/{lobby.MaxPlayers}".EscapeMarkup());
                    }
                    else
                    {
                        table.AddRow("No lobbies found", "-");
                    }
                });

            AnsiConsole.Write(table);
        }

        // Join a lobby – provides choice between code and ID
        private static async Task JoinLobby()
        {
            var method = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Join by:")
                    .AddChoices("Lobby Code", "Lobby ID"));

            LobbyResponse joined = null;
            await AnsiConsole.Status()
                .StartAsync("Joining lobby...", async ctx =>
                {
                    if (method == "Lobby Code")
                    {
                        string code = AnsiConsole.Ask<string>("Enter lobby code:");
                        joined = await UAG.LobbyAPI.JoinLobbyByCodeAsync(new JoinByCodeRequest
                        {
                            LobbyCode = code,
                            Player = new LobbyPlayer { Id = UAG.userId }
                        });
                    }
                    else
                    {
                        string id = AnsiConsole.Ask<string>("Enter lobby ID:");
                        joined = await UAG.LobbyAPI.JoinLobbyByIdAsync(id, new JoinByIdRequest
                        {
                            Player = new LobbyPlayer { Id = UAG.userId }
                        });
                    }
                });

            if (joined != null)
            {
                AnsiConsole.MarkupLine($"[green]Joined lobby: {joined.Name} ({joined.Id})[/]");
                StartHeartbeat(joined.Id);
            }
            else
                AnsiConsole.MarkupLine("[red]Failed to join lobby.[/]");
        }

        // Fetch detailed lobby info
        private static async Task FetchLobbyInfo()
        {
            string lobbyId = AnsiConsole.Ask<string>("Lobby ID:");
            LobbyResponse lobby = null;

            await AnsiConsole.Status()
                .StartAsync("Fetching lobby...", async ctx =>
                {
                    lobby = await UAG.LobbyAPI.GetLobbyAsync(lobbyId);
                });

            if (lobby != null)
                AnsiConsole.Write(new Panel(lobby.ToString()).Border(BoxBorder.Rounded));
            else
                AnsiConsole.MarkupLine("[red]Lobby not found.[/]");
        }

        // Create a new lobby
        private static async Task CreateLobby()
        {
            var request = new CreateLobbyRequest
            {
                Name = AnsiConsole.Ask<string>("Lobby name:"),
                MaxPlayers = AnsiConsole.Ask<int>("Max players:"),
                IsPrivate = AnsiConsole.Confirm("Private?"),
                Player = new LobbyPlayer { Id = UAG.userId }
            };

            LobbyResponse created = null;
            await AnsiConsole.Status()
                .StartAsync("Creating lobby...", async ctx =>
                {
                    created = await UAG.LobbyAPI.CreateLobbyAsync(request);
                });

            if (created != null)
            {
                AnsiConsole.MarkupLine($"[green]Lobby created: {created.Name} ({created.Id})[/]");
                StartHeartbeat(created.Id);
            }
            else
                AnsiConsole.MarkupLine("[red]Lobby creation failed.[/]");
        }

        // Update existing lobby settings (host only)
        private static async Task UpdateLobby()
        {
            string lobbyId = AnsiConsole.Ask<string>("Lobby ID to update:");
            var updateReq = new UpdateLobbyRequest();

            // Ask for fields; leave blank to keep current
            updateReq.Name = AnsiConsole.Ask<string>("New name (blank = no change):", "");
            if (string.IsNullOrWhiteSpace(updateReq.Name)) updateReq.Name = null;

            string maxPlayersInput = AnsiConsole.Ask<string>("New max players (blank = no change):", "");
            if (int.TryParse(maxPlayersInput, out int maxPl))
                updateReq.MaxPlayers = maxPl;
            else
                updateReq.MaxPlayers = null;

            var privInput = AnsiConsole.Ask<string>("Private? (true/false, blank = no change):", "");
            if (bool.TryParse(privInput, out bool priv)) updateReq.IsPrivate = priv;
            var lockInput = AnsiConsole.Ask<string>("Locked? (true/false, blank = no change):", "");
            if (bool.TryParse(lockInput, out bool locked)) updateReq.IsLocked = locked;

            LobbyResponse updated = null;
            await AnsiConsole.Status()
                .StartAsync("Updating lobby...", async ctx =>
                {
                    updated = await UAG.LobbyAPI.UpdateLobbyAsync(lobbyId, updateReq);
                });

            if (updated != null)
                AnsiConsole.MarkupLine("[green]Lobby updated.[/]");
            else
                AnsiConsole.MarkupLine("[red]Update failed.[/]");
        }

        // Delete a lobby (host only)
        private static async Task DeleteLobby()
        {
            string lobbyId = AnsiConsole.Ask<string>("Lobby ID to delete:");
            if (!AnsiConsole.Confirm($"Permanently delete lobby {lobbyId}?"))
                return;

            bool ok = false;
            await AnsiConsole.Status()
                .StartAsync("Deleting lobby...", async ctx =>
                {
                    ok = await UAG.LobbyAPI.DeleteLobbyAsync(lobbyId);
                });

            if (ok)
            {
                AnsiConsole.MarkupLine("[green]Lobby deleted.[/]");
                StopHeartbeat(lobbyId);
            }
            else
                AnsiConsole.MarkupLine("[red]Deletion failed (are you the host?).[/]");
        }

        // Remove a player from the lobby (host only)
        private static async Task RemovePlayer()
        {
            string lobbyId = AnsiConsole.Ask<string>("Lobby ID:");
            string playerId = AnsiConsole.Ask<string>("Player ID to kick:");
            bool ok = false;

            await AnsiConsole.Status()
                .StartAsync("Removing player...", async ctx =>
                {
                    ok = await UAG.LobbyAPI.RemovePlayerAsync(lobbyId, playerId);
                });

            if (ok)
                AnsiConsole.MarkupLine("[green]Player removed.[/]");
            else
                AnsiConsole.MarkupLine("[red]Failed to remove player.[/]");
        }

        // Update the authenticated player’s data inside a lobby
        private static async Task UpdateMyPlayer()
        {
            string lobbyId = AnsiConsole.Ask<string>("Lobby ID:");
            // Example: set a "status" data field
            string status = AnsiConsole.Ask<string>("Enter your status (e.g., Ready):", "Ready");
            var updateReq = new UpdatePlayerRequest
            {
                Data = new Dictionary<string, LobbyDataObject>
                {
                    { "status", new LobbyDataObject { Value = status, Visibility = "member" } }
                }
            };

            LobbyResponse result = null;
            await AnsiConsole.Status()
                .StartAsync("Updating player...", async ctx =>
                {
                    result = await UAG.LobbyAPI.UpdatePlayerAsync(lobbyId, UAG.userId, updateReq);
                });

            if (result != null)
                AnsiConsole.MarkupLine("[green]Player data updated.[/]");
            else
                AnsiConsole.MarkupLine("[red]Update failed.[/]");
        }

        // Quick join a random public lobby
        private static async Task QuickJoin()
        {
            var request = new QuickJoinRequest
            {
                Player = new LobbyPlayer { Id = UAG.userId }
            };

            LobbyResponse joined = null;
            await AnsiConsole.Status()
                .StartAsync("Searching for a lobby...", async ctx =>
                {
                    joined = await UAG.LobbyAPI.QuickJoinAsync(request);
                });

            if (joined != null)
            {
                AnsiConsole.MarkupLine($"[green]Quick‑joined: {joined.Name} ({joined.Id})[/]");
                StartHeartbeat(joined.Id);
            }
            else
                AnsiConsole.MarkupLine("[red]No available lobby found.[/]");
        }

        // Reconnect to a previously joined lobby
        private static async Task ReconnectToLobby()
        {
            string lobbyId = AnsiConsole.Ask<string>("Lobby ID to reconnect:");
            LobbyResponse reconnected = null;

            await AnsiConsole.Status()
                .StartAsync("Reconnecting...", async ctx =>
                {
                    reconnected = await UAG.LobbyAPI.ReconnectToLobbyAsync(lobbyId);
                });

            if (reconnected != null)
            {
                AnsiConsole.MarkupLine($"[green]Reconnected to: {reconnected.Name}[/]");
                StartHeartbeat(reconnected.Id);
            }
            else
                AnsiConsole.MarkupLine("[red]Reconnection failed.[/]");
        }

        private static void StartHeartbeat(string lobbyId)
        {
            // Removes any heartbeats
            StopHeartbeat();
            _activeLobbyId = lobbyId;
            _heartbeatCts = new CancellationTokenSource();

            // Just fires the heartbeat loop
            _ = Task.Run(async () =>
            {
                while (!_heartbeatCts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), _heartbeatCts.Token);
                    if (!_heartbeatCts.IsCancellationRequested && _activeLobbyId != null)
                    {
                        bool ok = await UAG.LobbyAPI.HeartbeatLobbyAsync(_activeLobbyId);
                        if (!ok)
                        {
                            AnsiConsole.MarkupLine("[red]Heartbeat failed – you may have left the lobby.[/]");
                            _activeLobbyId = null;
                            break;
                        }
                    }
                }
            });
        }

        private static void StopHeartbeat(string lobbyId = null)
        {
            if (lobbyId != null && lobbyId != _activeLobbyId)
                return;
            _heartbeatCts?.Cancel();
            _heartbeatCts = null;
            _activeLobbyId = null;
        }

        private static async Task ShowFriendsTabWrapper()
        {
            // Fetch relationships, then display names
            var rr = await UAG.RelationshipAPI.RetrieveRelationshipList(UAG.idToken);
            if (rr == null || rr.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No friends found.[/]");
                Console.ReadKey();
                return;
            }

            var idList = new List<string>();
            var onlineList = new List<string>();
            foreach (var rel in rr)
            {
                // Each relationship has Members collection; we need the other user's ID
                string friendId = rel.Members?.FirstOrDefault(m => m.Id != UAG.userId)?.Id;
                if (friendId != null)
                {
                    idList.Add(friendId);
                    onlineList.Add($"{friendId} | {rel.Type}");
                }
            }

            await ShowFriendsTab(onlineList, idList);
        }

        private static async Task ShowFriendsTab(IEnumerable<string> displayLines, IEnumerable<string> idEach)
        {
            var table = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn("name")
                .AddColumn("id");

            Console.WriteLine("Loading friends list, please wait...");

            // This just fetches names, then returms them
            var nameTasks = idEach.Select(async friendId =>
            {
                try
                {
                    var nameResp = await UAG.NameAPI.GetName(friendId);
                    return (Id: friendId, Name: nameResp?.Name ?? "Unknown");
                }
                catch
                {
                    return (Id: friendId, Name: "Error");
                }
            });

            var results = await Task.WhenAll(nameTasks);
            foreach (var (id, name) in results)
            {
                table.AddRow($"[green]{name}[/]", $"[green]| {id}[/]");
            }

            var panel = new Panel(table)
            {
                Header = new PanelHeader("[bold blue]Friends[/]"),
                Padding = new Padding(2, 1, 2, 1),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Cyan1),
                Expand = true
            };

            AnsiConsole.Clear();
            AnsiConsole.Write(panel);
            AnsiConsole.MarkupLine("\n[grey]Press any key to return to menu...[/]");
            Console.ReadKey(true);
        }

        private static async Task ShowRelationshipsTab()
        {
            var tab = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Relationship options")
                    .AddChoices("Send Friend Request", "Delete Relationship / Friend"));

            try
            {
                switch (tab)
                {
                    case "Send Friend Request":
                        string targetId = AnsiConsole.Ask<string>("Target player's user ID:");
                        await AnsiConsole.Status()
                            .StartAsync("Sending friend request...", async ctx =>
                            {
                                var result = await UAG.RelationshipAPI.CreateRelationship(UAG.idToken, targetId);
                                if (result == null)
                                    AnsiConsole.MarkupLine("[yellow]Request may have failed – check ID or permissions.[/]");
                                else
                                    AnsiConsole.MarkupLine("[green]Friend request sent![/]");
                            });
                        break;

                    case "Delete Relationship / Friend":
                        string relId = AnsiConsole.Ask<string>("Relationship ID to delete:");
                        await AnsiConsole.Status()
                            .StartAsync("Deleting...", async ctx =>
                            {
                                await UAG.RelationshipAPI.DeleteRelationship(UAG.idToken, relId);

                                AnsiConsole.MarkupLine("[green]Relationship deleted.[/]");
                            });
                        break;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            }

            AnsiConsole.MarkupLine("\n[grey]Press any key to return...[/]");
            Console.ReadKey();
        }

        private static void ShowProfileTab()
        {
            AnsiConsole.Write(new Rule("[yellow]Your Profile[/]"));
            AnsiConsole.MarkupLine($"[bold]Project ID:[/] 77078b1e-f13b-4e91-a33f-52cc74d5bdb8");
            AnsiConsole.MarkupLine($"[bold]User Id:[/] {UAG.userId}");
            AnsiConsole.MarkupLine($"[bold]Id Token:[/] {UAG.idToken}");
            AnsiConsole.MarkupLine($"[bold]Session Token:[/] {UAG.sessionToken}");
            Console.ReadKey();
        }

        private static async Task ShowLeaderboardsTab()
        {
            var tab = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Leaderboard actions")
                    .PageSize(15)
                    .AddChoices(
                        "Submit Score",
                        "Get My Score",
                        "Top Scores",
                        "Scores by Player IDs",
                        "Scores by Tier",
                        "Player Range",
                        "Archived Versions",
                        "Create Leaderboard (admin)",
                        "Delete Leaderboard (admin)",
                        "List Configs (admin)",
                        "Get Config (admin)",
                        "Delete Player Score (admin)",
                        "Delete All Scores for Player (admin)",
                        "Bucket Scores (admin)",
                        "Bucket IDs (admin)",
                        "Back"));

            try
            {
                switch (tab)
                {
                    case "Submit Score": await SubmitScore(); break;
                    case "Get My Score": await GetMyScore(); break;
                    case "Top Scores": await GetTopScores(); break;
                    case "Scores by Player IDs": await ScoresByPlayerIds(); break;
                    case "Scores by Tier": await ScoresByTier(); break;
                    case "Player Range": await PlayerRange(); break;
                    case "Archived Versions": await ArchivedVersions(); break;
                    case "Create Leaderboard (admin)": await CreateLeaderboardAdmin(); break;
                    case "Delete Leaderboard (admin)": await DeleteLeaderboardAdmin(); break;
                    case "List Configs (admin)": await ListConfigsAdmin(); break;
                    case "Get Config (admin)": await GetConfigAdmin(); break;
                    case "Delete Player Score (admin)": await DeletePlayerScoreAdmin(); break;
                    case "Delete All Scores for Player (admin)": await DeleteAllScoresAdmin(); break;
                    case "Bucket Scores (admin)": await BucketScoresAdmin(); break;
                    case "Bucket IDs (admin)": await BucketIdsAdmin(); break;
                    case "Back": return;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
            }

            AnsiConsole.MarkupLine("\n[grey]Press any key to return...[/]");
            Console.ReadKey();
        }

        private static async Task SubmitScore()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            double score = AnsiConsole.Ask<double>("Score:");
            var metadata = AskForMetadata();

            var request = new AddPlayerScoreRequest
            {
                Score = score,
                Metadata = metadata
            };

            var result = await UAG.LeaderboardAPI.AddPlayerScoreAsync(boardId, UAG.userId, request);
            if (result != null)
                AnsiConsole.MarkupLine($"[green]Score submitted! Rank: {result.Rank}, Score: {result.Score}[/]");
            else
                AnsiConsole.MarkupLine("[red]Failed to submit score.[/]");
        }

        private static async Task GetMyScore()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            var score = await UAG.LeaderboardAPI.GetPlayerScoreAsync(boardId, UAG.userId);
            if (score != null)
                DisplayPlayerScore(score);
            else
                AnsiConsole.MarkupLine("[yellow]No score found for you.[/]");
        }

        private static async Task GetTopScores()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            int limit = AnsiConsole.Ask("How many scores?", 10);
            bool includeMeta = AnsiConsole.Confirm("Include metadata?");

            var request = new GetScoresRequest
            {
                Limit = limit,
                IncludeMetadata = includeMeta
            };

            var response = await UAG.LeaderboardAPI.GetScoresAsync(boardId, request);
            if (response?.Results != null)
                DisplayScoresTable(response.Results);
            else
                AnsiConsole.MarkupLine("[red]Failed to fetch scores.[/]");
        }

        private static async Task ScoresByPlayerIds()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            string idsInput = AnsiConsole.Ask<string>("Player IDs (comma‑separated):");
            var ids = idsInput.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

            var response = await UAG.LeaderboardAPI.GetScoresByPlayerIdsAsync(boardId, ids);
            if (response?.Results != null && response.Results.Count > 0)
                DisplayScoresTable(response.Results);
            else
                AnsiConsole.MarkupLine("[yellow]No scores found for those players.[/]");
        }

        private static async Task ScoresByTier()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            string tierId = AnsiConsole.Ask<string>("Tier ID:");
            int limit = AnsiConsole.Ask("Limit:", 10);

            var request = new GetScoresRequest { Limit = limit };
            var response = await UAG.LeaderboardAPI.GetScoresByTierAsync(boardId, tierId, request);
            if (response?.Results != null)
                DisplayScoresTable(response.Results);
            else
                AnsiConsole.MarkupLine("[red]No scores for this tier.[/]");
        }

        private static async Task PlayerRange()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            string playerId = AnsiConsole.Ask<string>("Player ID (leave blank for you):", UAG.userId);
            if (string.IsNullOrWhiteSpace(playerId)) playerId = UAG.userId;

            var range = await UAG.LeaderboardAPI.GetPlayerRangeAsync(boardId, playerId);

            if (range?.Results != null && range.Results.Count > 0)
            {
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("Position");
                table.AddColumn("Player ID");
                table.AddColumn("Score");

                var myScore = range.Results.FirstOrDefault(s => s.PlayerId == playerId);
                var aboveMe = range.Results.Where(s => myScore != null && s.Rank < myScore.Rank).ToList();
                var belowMe = range.Results.Where(s => myScore != null && s.Rank > myScore.Rank).ToList();

                void AddRows(IEnumerable<LeaderboardEntry> entries, string label)
                {
                    foreach (var e in entries)
                        table.AddRow($"{e.Rank} ({label})", e.PlayerId, e.Score.ToString());
                }

                if (myScore != null)
                    table.AddRow($"{myScore.Rank} (you)", myScore.PlayerId, myScore.Score.ToString());

                if (aboveMe.Any()) AddRows(aboveMe, "above");
                if (belowMe.Any()) AddRows(belowMe, "below");

                AnsiConsole.Write(table);
            }
            else
                AnsiConsole.MarkupLine("[yellow]No range data.[/]");
        }

        private static async Task ArchivedVersions()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            var versionsResponse = await UAG.LeaderboardAPI.GetArchivedVersionsAsync(boardId);
            if (versionsResponse?.Results != null && versionsResponse.Results.Count > 0)
            {
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("Version ID");
                table.AddColumn("Created");
                foreach (var version in versionsResponse.Results)
                    table.AddRow(version.Id, version.Start.ToString("g"));
                AnsiConsole.Write(table);

                if (AnsiConsole.Confirm("View scores from a version?"))
                {
                    string verId = AnsiConsole.Ask<string>("Version ID:");
                    var scoresReq = new GetScoresRequest { Limit = 20 };
                    var scores = await UAG.LeaderboardAPI.GetArchivedVersionScoresAsync(boardId, verId, scoresReq);
                    if (scores?.Results != null)
                        DisplayScoresTable(scores.Results);
                    else
                        AnsiConsole.MarkupLine("[yellow]No scores in that version.[/]");
                }
            }
            else
                AnsiConsole.MarkupLine("[yellow]No archived versions.[/]");
        }
        private static async Task CreateLeaderboardAdmin()
        {
            var request = new CreateLeaderboardRequest
            {
                Name = AnsiConsole.Ask<string>("Leaderboard name:"),
                SortOrder = AnsiConsole.Prompt(
                    new SelectionPrompt<string>().Title("Sort order").AddChoices("asc", "desc")),
                UpdateStrategy = AnsiConsole.Prompt(
                    new SelectionPrompt<string>().Title("Update strategy").AddChoices("best", "latest", "sum")),
                MaxEntries = AnsiConsole.Ask("Max entries:", 100)
            };

            var result = await UAG.LeaderboardAPI.CreateLeaderboardAsync(request);
            if (result != null)
                AnsiConsole.MarkupLine($"[green]Leaderboard created! ID: {result.Id}[/]");
            else
                AnsiConsole.MarkupLine("[red]Admin creation failed (needs service token?).[/]");
        }

        private static async Task DeleteLeaderboardAdmin()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID to delete:");
            bool ok = await UAG.LeaderboardAPI.DeleteLeaderboardAsync(boardId);
            AnsiConsole.MarkupLine(ok ? "[green]Deleted.[/]" : "[red]Failed.[/]");
        }

        private static async Task ListConfigsAdmin()
        {
            var configs = await UAG.LeaderboardAPI.GetLeaderboardConfigsAsync();
            if (configs != null)
            {
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("ID");
                table.AddColumn("Name");
                table.AddColumn("Sort");
                table.AddColumn("Strategy");
                foreach (var c in configs)
                    table.AddRow(c.Id, c.Name, c.SortOrder, c.UpdateStrategy);
                AnsiConsole.Write(table);
            }
            else
                AnsiConsole.MarkupLine("[red]Could not fetch configs.[/]");
        }

        private static async Task GetConfigAdmin()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            var config = await UAG.LeaderboardAPI.GetLeaderboardConfigAsync(boardId);
            if (config != null)
                AnsiConsole.Write(new Panel(config.ToString()));
            else
                AnsiConsole.MarkupLine("[red]Not found.[/]");
        }

        private static async Task DeletePlayerScoreAdmin()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            string playerId = AnsiConsole.Ask<string>("Player ID:");
            bool ok = await UAG.LeaderboardAPI.DeletePlayerScoreAsync(boardId, playerId);
            AnsiConsole.MarkupLine(ok ? "[green]Score deleted.[/]" : "[red]Failed.[/]");
        }

        private static async Task DeleteAllScoresAdmin()
        {
            string playerId = AnsiConsole.Ask<string>("Player ID:");
            bool ok = await UAG.LeaderboardAPI.DeletePlayerScoreFromAllAsync(playerId);
            AnsiConsole.MarkupLine(ok ? "[green]All scores deleted for that player.[/]" : "[red]Failed.[/]");
        }

        private static async Task BucketScoresAdmin()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            string bucketId = AnsiConsole.Ask<string>("Bucket ID:");
            int limit = AnsiConsole.Ask("Limit:", 10);

            var request = new GetScoresRequest { Limit = limit };
            var response = await UAG.LeaderboardAPI.GetBucketScoresAsync(boardId, bucketId, request);
            if (response?.Results != null)
                DisplayScoresTable(response.Results);
            else
                AnsiConsole.MarkupLine("[red]No bucket scores.[/]");
        }

        private static async Task BucketIdsAdmin()
        {
            string boardId = AnsiConsole.Ask<string>("Leaderboard ID:");
            var buckets = await UAG.LeaderboardAPI.GetBucketIdsAsync(boardId);
            if (buckets != null)
            {
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("Bucket ID");
                table.AddColumn("Name");
                foreach (var b in buckets)
                    table.AddRow(b.Id, b.Name);
                AnsiConsole.Write(table);
            }
            else
                AnsiConsole.MarkupLine("[red]Could not retrieve buckets.[/]");
        }

        private static void DisplayPlayerScore(PlayerScoreResponse score)
        {
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Rank");
            table.AddColumn("Score");
            table.AddColumn("Metadata");
            table.AddRow(score.Rank.ToString(), score.Score.ToString(),
                score.Metadata != null ? string.Join(", ", score.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "-");
            AnsiConsole.Write(table);
        }

        private static void DisplayScoresTable(List<LeaderboardEntry> entries)
        {
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Rank");
            table.AddColumn("Player ID");
            table.AddColumn("Score");
            table.AddColumn("Metadata");
            foreach (var e in entries)
                table.AddRow(e.Rank.ToString(), e.PlayerId, e.Score.ToString(),
                    e.Metadata != null ? string.Join(", ", e.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "-");
            AnsiConsole.Write(table);
        }

        private static Dictionary<string, string> AskForMetadata()
        {
            var metadata = new Dictionary<string, string>();
            if (!AnsiConsole.Confirm("Add metadata?")) return metadata;

            while (true)
            {
                string key = AnsiConsole.Prompt(
                    new TextPrompt<string>("Metadata key (or empty to finish):")
                        .AllowEmpty());
                if (string.IsNullOrWhiteSpace(key)) break;
                string value = AnsiConsole.Ask<string>($"Value for '{key}':");
                metadata[key] = value;
            }
            return metadata;
        }

        private static async Task ShowNotificationsTab()
        {
            AnsiConsole.Write(new Rule("[yellow]Notifications[/]"));
            try
            {
                var notifs = await UAG.PlayerAuthAPI.GetNotificationsAsync(UAG.userId);
                if (notifs == null || notifs.Count == 0)
                {
                    AnsiConsole.MarkupLine("No notifications.");
                }
                else
                {
                    var table = new Table().Border(TableBorder.Rounded);
                    table.AddColumn("Type");
                    table.AddColumn("Content");
                    table.AddColumn("Date");
                    foreach (var n in notifs)
                        table.AddRow(n.Type ?? "", n.Content ?? "", n.CreatedAt.ToString("g"));
                    AnsiConsole.Write(table);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Could not fetch notifications: {ex.Message.EscapeMarkup()}[/]");
            }

            Console.ReadKey();
        }
    }
}