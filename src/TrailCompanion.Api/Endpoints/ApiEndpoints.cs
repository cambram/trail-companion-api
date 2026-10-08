namespace TrailCompanion.Api.Endpoints;

internal static class ApiEndpoints
{
    private const string ApiBase = "api";

    internal static class Users
    {
        public const string UserBase = $"{ApiBase}/users";
        public const string RegisterUser = $"{UserBase}/register";
        public const string LoginUser = $"{UserBase}/login";
        public const string GetMyUser = $"{UserBase}/me";
        public const string UpdateMyUser = $"{UserBase}/me";
        public const string DeleteMyUser = $"{UserBase}/me";
        public const string ChangePassword = $"{UserBase}/password";
        public const string RefreshToken = $"{UserBase}/refresh";
        public const string Logout = $"{UserBase}/logout";
        private const string RaceBase = $"{ApiBase}/races";
        public const string CreateRace = RaceBase;
        public const string GetRaces = RaceBase;
        public const string GetRace = $"{RaceBase}/{{id:guid}}";
        public const string DeleteRace = $"{RaceBase}/{{id:guid}}";
        public const string UpdateRace = $"{RaceBase}/{{id:guid}}";
        public const string JoinRace = $"{RaceBase}/join";
        public const string RegenerateRacePin = $"{RaceBase}/{{id:guid}}/pin";
        private const string CrewBase = $"{RaceBase}/{{raceId:guid}}/crew";
        public const string GetRaceCrew = CrewBase;
        public const string RemoveCrewMember = $"{CrewBase}/{{memberId:guid}}";
        private const string SegmentBase = $"{RaceBase}/{{raceId:guid}}/segments";
        public const string CreateSegment = SegmentBase;
        public const string ReorderSegments = $"{SegmentBase}/order";
        public const string UpdateSegment = $"{SegmentBase}/{{id:guid}}";
        public const string DeleteSegment = $"{SegmentBase}/{{id:guid}}";
        private const string ItemBase = $"{SegmentBase}/{{segmentId:guid}}/items";
        public const string CreateItem = ItemBase;
        public const string ReorderItems = $"{ItemBase}/order";
        public const string DeleteItem = $"{ItemBase}/{{id:guid}}";
        private const string AidStationBase = $"{RaceBase}/{{raceId:guid}}/aid-stations";
        public const string CreateAidStation = AidStationBase;
        public const string UpdateAidStation = $"{AidStationBase}/{{id:guid}}";
        public const string DeleteAidStation = $"{AidStationBase}/{{id:guid}}";
        public const string MoveAidStation = $"{AidStationBase}/{{id:guid}}/move";
        private const string AidStationItemBase = $"{AidStationBase}/{{aidStationId:guid}}/items";
        public const string CreateAidStationItem = AidStationItemBase;
        public const string ReorderAidStationItems = $"{AidStationItemBase}/order";
        public const string DeleteAidStationItem = $"{AidStationItemBase}/{{id:guid}}";
        private const string NutritionItemBase = $"{ApiBase}/nutrition-items";
        public const string GetNutritionItems = NutritionItemBase;
        public const string CreateNutritionItem = NutritionItemBase;
        public const string UpdateNutritionItem = $"{NutritionItemBase}/{{id:guid}}";
        public const string DeleteNutritionItem = $"{NutritionItemBase}/{{id:guid}}";
    }
}
