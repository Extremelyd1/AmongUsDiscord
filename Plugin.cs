using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using InnerNet;

namespace AmongUsDiscord {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin {
        // Static log source so other classes can use logging 
        internal new static ManualLogSource Log;
        
        // Static HTTP client so patches can use it
        private static HttpClient _httpClient;

        // BepInEx config entries
        public ConfigEntry<string> ConfigIp { get; private set; }
        public ConfigEntry<int> ConfigPort { get; private set; }

        public override void Load() {
            Log = base.Log;

            CreateConfigEntries();

            _httpClient = new HttpClient(this);

            InitializeHarmony();
        }

        /// <summary>
        /// Create the config entries for this mod.
        /// </summary>
        private void CreateConfigEntries() {
            ConfigIp = Config.Bind(
                "General",
                "HTTP IP",
                "",
                "The IP address of the HTTP webserver to send requests to"
            );
            ConfigPort = Config.Bind(
                "General",
                "HTTP Port",
                -1,
                "The port of the HTTP webserver to send requests to"
            );
        }

        /// <summary>
        /// Initialize the Harmony instance for patching methods.
        /// </summary>
        private void InitializeHarmony() {
            var harmony = new Harmony("com.extremelyd1.AmongUsDiscord");

            harmony.PatchAll();
        }

        /// <summary>
        /// Patch the getter of the AmBanned property in StatsManager to ensure that our client
        /// doesn't think we are banned.
        /// </summary>
        [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.AmBanned), MethodType.Getter)]
        public static class StatsManagerAmBannedPatch {
            public static void Postfix(out bool __result) {
                __result = false;
            }
        }

        /// <summary>
        /// Send a game start request using the HTTP client with all the player names.
        /// </summary>
        private static void SendGameStartRequest() {
            Log.LogMessage("Game has started!");
            
            // Get a list of all the player names
            var playerNames = new List<string>();
            foreach (var playerInfo in GameData.Instance.AllPlayers) {
                playerNames.Add(playerInfo.PlayerName);
            }

            _httpClient.SendStartRequest(playerNames);
        }

        /// <summary>
        /// Patch that executes when the intro cutscene shows that our local player is
        /// a crew mate.
        /// </summary>
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        public static class IntroCutsceneBeginCrewmatePatch {
            public static void Prefix() {
                SendGameStartRequest();
            }
        }

        /// <summary>
        /// Patch that executes when the intro cutscene shows that our local player is
        /// an impostor.
        /// </summary>
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
        public static class IntroCutsceneBeginImpostorPatch {
            public static void Prefix() {
                SendGameStartRequest();
            }
        }

        /// <summary>
        /// Patch that executes when the meeting hud starts and thus a meeting is called.
        /// </summary>
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        public static class MeetingHudStartPatch {
            public static void Prefix() {
                Log.LogMessage("Meeting was called!");
                
                _httpClient.SendMeetingCalledRequest();
            }
        }

        /// <summary>
        /// Send a meeting end request using the HTTP client.
        /// </summary>
        /// <param name="exileController">The exile controller responsible for the meeting end.</param>
        private static void SendMeetingEndRequest(ExileController exileController) {
            var exiled = exileController.exiled;
            
            var gameData = GameData.Instance;
            if (gameData != null) {
                var players = gameData.AllPlayers;
                if (players != null) {
                    var crewCount = 0;
                    var imposterCount = 0;

                    // Count how many of the alive players are impostor and crew mate 
                    foreach (var player in players) {
                        // Skip dead and the exiled player (if they exist)
                        if (player.IsDead || exiled != null && player.PlayerId == exiled.PlayerId) {
                            continue;
                        }

                        if (player.Role.IsImpostor) {
                            imposterCount++;
                        } else {
                            crewCount++;
                        }
                    }

                    // We skip sending the meeting end request, since this meeting will end the game
                    // Therefore, we prevent sending unnecessary requests
                    if (imposterCount == 0 || imposterCount >= crewCount) {
                        return;
                    }
                }
            }

            if (exiled == null) {
                Log.LogMessage("Meeting has ended!");
                    
                _httpClient.SendMeetingEndRequest();
            } else {
                var playerName = exiled.PlayerName;
                    
                Log.LogMessage("Meeting has ended, exiled player: " + playerName + "!");
                    
                _httpClient.SendMeetingEndRequest(playerName);
            }
        }

        /// <summary>
        /// Patch that executes when the exile screen is wrapping up and thus the
        /// meeting is entirely finished.
        /// </summary>
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        public static class ExileControllerWrapUpPatch {
            public static void Prefix(ExileController __instance) {
                SendMeetingEndRequest(__instance);
            }
        }

        // Called when the exile screen is finished on airship
        /// <summary>
        /// Patch that executes when the exile screen on airship is wrapping up and
        /// thus the meeting is entirely finished.
        /// </summary>
        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
        public static class AirshipExileControllerWrapUpAndSpawnPatch {
            public static void Prefix(AirshipExileController __instance) {
                SendMeetingEndRequest(__instance);
            }
        }

        /// <summary>
        /// Checks whether we are in a running game (not lobby) and sends a player
        /// death request with the given player name.
        /// </summary>
        /// <param name="playerName">The name of the player that died.</param>
        private static void CheckSendPlayerDeathRequest(string playerName) {
            var amongUsClient = AmongUsClient.Instance;
            if (amongUsClient == null) {
                return;
            }

            // If the game state is not started, the death doesn't have to be sent
            var gameState = amongUsClient.GameState;
            if (gameState != InnerNetClient.GameStates.Started) {
                return;
            }
            
            _httpClient.SendPlayerDeathRequest(playerName);
        }

        /// <summary>
        /// Patch that executes when a player dies in a running game.
        /// This is triggered for kills, disconnecting and exiling.
        /// </summary>
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
        public static class PlayerControlDiePatch {
            public static void Prefix(PlayerControl __instance, DeathReason reason) {
                // Only check and send a player death request if the reason for their
                // death is a kill
                if (reason == DeathReason.Kill) {
                    Log.LogMessage("Player '" + __instance.name + "' has died!");

                    CheckSendPlayerDeathRequest(__instance.name);
                }
            }
        }
        
        /// <summary>
        /// Patch that executes when a player leaves the game, either in a running
        /// game or in the lobby.
        /// </summary>
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
        public static class AmongUsClientOnPlayerLeftPatch {
            public static void Prefix(ClientData data) {
                Log.LogMessage("Player '" + data.PlayerName + "' has left!");
                
                // If we can successfully check whether the player is dead and
                // they are in fact dead, we skip sending a death request
                var playerControl = data.Character;
                if (playerControl != null) {
                    var playerInfo = playerControl.Data;
                    if (playerInfo != null) {
                        if (playerInfo.IsDead) {
                            return;
                        }
                    }
                }

                CheckSendPlayerDeathRequest(data.PlayerName);
            }
        }

        /// <summary>
        /// Patch that executes when a running game ends.
        /// </summary>
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        public static class AmongUsClientOnGameEndPatch {
            public static void Prefix() {
                Log.LogMessage("Game has ended!");
                
                _httpClient.SendEndRequest();
            }
        }

        /// <summary>
        /// Patch that executes when the local user disconnects from the game.
        /// This is not executed for user-initiated action, but rather from
        /// connection issues or similar.
        /// </summary>
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
        public static class AmongUsClientOnDisconnectedPatch {
            public static void Prefix() {
                Log.LogMessage("Local user has disconnected!");
                
                _httpClient.SendEndRequest();
            }
        }
        
        /// <summary>
        /// Patch that executes when the local user manually exits the game by
        /// pressing the 'leave game' button.
        /// </summary>
        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
        public static class AmongUsClientExitGamePatch {
            public static void Prefix() {
                Log.LogMessage("Local user has exited game!");
                
                _httpClient.SendEndRequest();
            }
        }
    }
}
