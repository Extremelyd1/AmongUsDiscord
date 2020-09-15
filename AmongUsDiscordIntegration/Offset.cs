namespace AmongUsDiscordIntegration {
    public class Offset {
        public static string PlayerControl_Pointer = "GameAssembly.dll+DA5A84";  //GameAssembly.dll+E22AE8
        public static string PlayerControl_GetData = "55 8B EC 80 3D BD B0 ??";

        public static string AmongUsClient_Pointer = "GameAssembly.dll+00D14F1C";
        public static string[] AmongUsClient_Offsets = {"5C", "8", "8", "18", "0"};
        
        public static string ShipStatus_Pointer = "GameAssembly.dll+00DA04CC";
        public static string[] ShipStatus_Offsets = {"5C", "0", "8", "18", "0"};

        public static string MeetingHud_Pointer = "GameAssembly.dll+00DA58D0";
        public static string[] MeetingHud_Offsets = {"5C", "0", "8", "94", "18", "0"};
    }
}