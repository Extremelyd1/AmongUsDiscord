namespace AmongUsDiscordIntegration {
    public class Offset {
        public static string PlayerControl_Pointer = "GameAssembly.dll+DA050C";  //GameAssembly.dll+E22AE8
        public static string PlayerControl_GetData = "55 8B EC 80 3D B5 5A ??";

        public static string AmongUsClient_Pointer = "GameAssembly.dll+00DA3E1C";
        public static string[] AmongUsClient_Offsets = {"5C", "18", "10", "0"};
        
        public static string ShipStatus_Pointer = "GameAssembly.dll+00DA04CC";
        public static string[] ShipStatus_Offsets = {"5C", "0", "8", "18", "0"};

        public static string MeetingHud_Pointer = "GameAssembly.dll+00D04CBC";
        public static string[] MeetingHud_Offsets = {"5C", "10", "8", "18", "0"};
    }
}