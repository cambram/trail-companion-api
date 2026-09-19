namespace TrailCompanion.Api.Endpoints;

internal static class ApiEndpoints
{
    private const string ApiBase = "api";

    internal static class Users
    {
        private const string UserBase = $"{ApiBase}/users";
        public const string RegisterUser = $"{UserBase}/register";
        public const string LoginUser = $"{UserBase}/login";
        public const string GetMyUser = $"{UserBase}/me";
        private const string RaceBase = $"{ApiBase}/races";
        public const string CreateRace = RaceBase;
        public const string GetRaces = RaceBase;
    }
}
