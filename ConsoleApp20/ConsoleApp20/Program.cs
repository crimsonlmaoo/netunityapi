using NetUnityApi;
using NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.ClientAPI;
using NetUnityApi.Live_Operations.Game_Backend.Player_Authentication.Models;

namespace ConsoleApp20
{
    class Program
    {
        public static API PlayerAuth = new API("77078b1e-f13b-4e91-a33f-52cc74d5bdb8");
        static async Task Main()
        {
            Task<AuthResponse> r = PlayerAuth.SignInAnonymousAsync();

            if (r.Result.IdToken != null)
            {
                var Name = new NetUnityApi.Multiplayer.Name.ClientAPI.API(r.Result.IdToken);
                var Lobby = new NetUnityApi.Multiplayer.Lobby.ClientAPI.API(r.Result.IdToken);

                Console.WriteLine("we in");

                Console.Write("1. change name\n2. get name\n3. query lobbies\n4. join lobby");

                Console.Write("\n#> ");

                string c = Console.ReadLine();

                switch (c)
                {
                    case "1":
                        Console.Write("name #> ");
                        var r2 = Name.SetName(new NetUnityApi.Responses.Multiplayer.Name.NameRequest() { name = Console.ReadLine() }, r.Result.UserId);
                        Console.WriteLine($"{r2.Result.Name} : {r2.Result.Id}");
                        break;
                    case "2":
                        var r21 = Name.GetName(r.Result.UserId);
                        Console.WriteLine($"{r21.Result.Name} : {r21.Result.Id}");
                        break;
                    case "3":
                        var r211 = Lobby.QueryLobbiesAsync(new NetUnityApi.Multiplayer.Lobby.ClientAPI.QueryLobbiesRequest() { Count = 100 });

                        if (r211.Result.Results.Count == 0 || r211.Result.Results == null)
                            return;

                        foreach (var i in r211.Result.Results)
                        {
                            Console.WriteLine($"{i.Id} : {i.Name} : {i.Created} : {i.AvailableSlots}");
                        }
                        break;
                    case "4":
                        //fawk this one
                        Console.Write("code #> ");
                        Lobby.CreateLobbyAsync(new NetUnityApi.Multiplayer.Lobby.ClientAPI.CreateLobbyRequest() { MaxPlayers = 5, IsPrivate = false, Player = new NetUnityApi.Responses.Multiplayer.Lobby.LobbyPlayer() { Id = r.Result.UserId }, Name = "diddy" });
                        //Lobby.JoinLobbyByCodeAsync(new NetUnityApi.Multiplayer.Lobby.ClientAPI.JoinLobbyRequest() { LobbyCode = Console.ReadLine(), Player = new NetUnityApi.Responses.Multiplayer.Lobby.LobbyPlayer() { Id = r.Result.UserId } });
                        break;
                }
                Console.ReadKey();
            }
            Console.ReadKey();
        }
    }
}