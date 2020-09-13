namespace AmongUsDiscordIntegration {
    internal class Launcher {
        public static void Main(string[] args) {
            if (args.Length < 1) {
                new Program(false).Init();
            } else if (args[0].ToLower().Equals("http")) {
                new Program(true).Init();
            }
        }
    }
}