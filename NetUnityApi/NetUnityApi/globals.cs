using System;

namespace NetUnityApi
{
    public static class Globals
    {
        public const string BaseUrlLobby = "https://lobby.services.api.unity.com/v1";
        public const string BaseUrlSocial = "https://social.services.api.unity.com/v1";
        public const string BaseUrlLeaderboard = "https://leaderboards.services.api.unity.com/v1/";
        public const string BaseUrlPlayerAuth = "https://player-auth.services.api.unity.com/v1";

        public enum AuthType
        {
            Anonymous = 0,
            SessionToken = 1,
            UsernamePassword = 2,
            ExternalToken = 3,
            CustomId = 4,
            CodeLink = 5
        }

        public enum ExternalIdProvider
        {
            Apple,
            Facebook,
            Google,
            GooglePlayGames,
            Oculus,
            OpenIDConnect,
            Steam,
        }
    }
}