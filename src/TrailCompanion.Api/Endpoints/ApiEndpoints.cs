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
        public const string DeleteRace = $"{RaceBase}/{{id:guid}}";
        private const string SegmentBase = $"{RaceBase}/{{raceId:guid}}/segments";
        public const string CreateSegment = SegmentBase;
        public const string UpdateSegment = $"{SegmentBase}/{{id:guid}}";
        public const string DeleteSegment = $"{SegmentBase}/{{id:guid}}";
        private const string ItemBase = $"{SegmentBase}/{{segmentId:guid}}/items";
        public const string CreateItem = ItemBase;
        public const string UpdateItem = $"{ItemBase}/{{id:guid}}";
        public const string DeleteItem = $"{ItemBase}/{{id:guid}}";
    }
}
