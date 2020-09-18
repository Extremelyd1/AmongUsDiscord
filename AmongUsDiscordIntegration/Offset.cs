namespace AmongUsDiscordIntegration {
    public static class Offset {
        public const string PlayerControlPointer = "GameAssembly.dll+DA5A84";
        public const string PlayerControlGetData = "55 8B EC 80 3D BD B0 ??";

        public const string AmongUsClientPointer = "GameAssembly.dll+DA5ACC";
        public static readonly string[] AmongUsClientOffsets = {"5C", "0", "0"};

        public const string ShipStatusPointer = "GameAssembly.dll+DA5A50";
        public static readonly string[] ShipStatusOffsets = {"5C", "0", "0"};

        public const string MeetingHudPointer = "GameAssembly.dll+DA58D0";
        public static readonly string[] MeetingHudOffsets = {"5C", "0", "0"};
    }
}