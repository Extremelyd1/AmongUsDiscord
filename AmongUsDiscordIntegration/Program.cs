using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace AmongUsDiscordIntegration {
    public class Program {
        private const string AmongUsProcessName = "Among Us";
        private const string Ip = "192.168.2.200";
        private const string Port = "7919";

        private const int OverlapWaitTime = 1000;

        public static Memory Mem;
        public static ProcessMemory ProcessMemory;

        private readonly bool _useHttp;

        private readonly HttpClient _httpClient;

        private readonly Keyboard _keyboard;

        private bool _isMuted;
        private bool _isDeafened;
        
        private GameState _gameState;
        private bool _inMeeting;
        private bool _isDead;
        private bool _newDeath;

        private bool _lastExileState;

        private bool _votedOff;
        private bool _voteCausesEnd;

        private readonly Dictionary<string, bool> _lastPlayerAliveState;

        public Program(bool useHttp) {
            _useHttp = useHttp;

            if (_useHttp) {
                _httpClient = new HttpClient(Ip, Port);
            }
            
            Mem = new Memory();
            
            _keyboard = new Keyboard();

            _isMuted = false;
            _isDeafened = false;

            _gameState = GameState.Menu;
            _inMeeting = false;
            _isDead = false;
            _newDeath = false;

            _lastExileState = false;

            _votedOff = false;
            _voteCausesEnd = true;

            _lastPlayerAliveState = new Dictionary<string, bool>();
        }
        
        public void Init() {
            if (_useHttp) {
                Console.WriteLine("Running in HTTP mode");
            }
            
            Console.WriteLine("Initializing program...");

            FindProcess();
        }

        private void FindProcess() {
            var counter = 0;

            bool found = false;
            while (!found) {
                Console.Write("\rTrying to find Among Us process ");

                switch (counter) {
                    case 0: 
                        Console.Write("/");
                        break;
                    case 1:
                        Console.Write("-");
                        break;
                    case 2:
                        Console.Write("\\");
                        break;
                    case 3:
                        Console.Write("|");
                        break;
                }

                counter++;
                if (counter > 3) {
                    counter = 0;
                }
                
                Thread.Sleep(500);
                
                found = Mem.OpenProcess(AmongUsProcessName);
            }

            Console.WriteLine("\n");

            for (var i = 10; i > 0; i--) {
                Console.Write($"\rAmong Us Process found, starting program loop in {i}...");
                Thread.Sleep(1000);
            }

            Console.WriteLine("\rAmong Us Process found, starting program loop now...");

            Mem.OpenProcess(AmongUsProcessName);
            
            var proc = Process.GetProcessesByName(AmongUsProcessName)[0];
            ProcessMemory = new ProcessMemory(proc);
            ProcessMemory.Open(ProcessAccess.AllAccess);
            
            Start();
        }

        private void Start() {
            while (CheckProcess()) {
                CheckState();
                CheckPlayers();
                CheckMeeting();

                Thread.Sleep(100);
            }
            
            Console.WriteLine("Among Us process closed, exiting...");
        }

        private bool CheckProcess() {
            return Mem.GetProcIdFromName(AmongUsProcessName) != 0;
        }
        
        private void CheckState() {
            if (!GetAmongUsClient(out var amongUsClient)) {
                return;
            }
            
            var newState = (GameState) amongUsClient.GameState;
            
            if (!_gameState.Equals(newState)) {
                Console.WriteLine($"State changed to {newState.ToString()}");

                if (newState.Equals(GameState.EndScreen)) {
                    Console.WriteLine("Game has ended!");

                    if (_useHttp) {
                        _httpClient.SendEndRequest();
                    } else if (_isDeafened || _isMuted) {
                        ToggleMute();
                    }
                }

                if (_gameState.Equals(GameState.Lobby) && newState.Equals(GameState.InGame)) {
                    Console.WriteLine("Game has started!");

                    _isDead = false;
                    _newDeath = false;

                    List<string> playerNames = new List<string>();
                    foreach (var playerData in GetAllPlayers()) {
                        playerNames.Add(Utils.ReadString(playerData.PlayerInfo.PlayerName));
                    }

                    if (_useHttp) {
                        _httpClient.SendStartRequest(playerNames);
                    } else {
                        ToggleDeafen();
                    }
                }

                if (newState.Equals(GameState.Lobby) || newState.Equals(GameState.Menu)) {
                    _inMeeting = false;
                    _isDead = false;
                    _newDeath = false;
                    _votedOff = false;
                    _voteCausesEnd = false;
                    
                    if (_isDeafened || _isMuted) {
                        ToggleMute();
                    }
                }
                
                _gameState = newState;
            }
        }

        private void CheckPlayers() {
            if (!_gameState.Equals(GameState.InGame)) {
                return;
            }
            
            var checkedPlayers = new List<string>();
            
            foreach (var playerData in GetAllPlayers()) {
                var playerName = Utils.ReadString(playerData.PlayerInfo.PlayerName);
                var currentState = playerData.PlayerInfo.IsDead == 1;
                    
                checkedPlayers.Add(playerName);
            
                if (!_lastPlayerAliveState.ContainsKey(playerName)) {
                    _lastPlayerAliveState.Add(playerName, currentState);
                        
                    continue;
                }
            
                if (!_lastPlayerAliveState.TryGetValue(playerName, out var lastState)) {
                    continue;
                }
            
                if (currentState != lastState) {
                        
                    if (currentState) {
                        if (_useHttp) {
                            Console.WriteLine($"Player {playerName} has died");
                            
                            _httpClient.SendPlayerDeathRequest(playerName);
                        } else if (playerData.IsLocalPlayer()) {
                            Console.WriteLine("You have died");
                            
                            _isDead = true;
                            _newDeath = !_votedOff;
                            
                            if (_votedOff) {
                                _votedOff = false;
                            }
                        }
                    }
                        
                    _lastPlayerAliveState.Remove(playerName);
                    _lastPlayerAliveState.Add(playerName, currentState);
                }
            }
                
            var playerNamesNotInGame = new List<string>();
            
            foreach (var playerName in _lastPlayerAliveState.Keys) {
                if (!checkedPlayers.Contains(playerName)) {
                    playerNamesNotInGame.Add(playerName);
                }
            }
                
            foreach (var playerName in playerNamesNotInGame) {
                _lastPlayerAliveState.Remove(playerName);
            
                Console.WriteLine($"Player {playerName} has left the game");
            
                if (_useHttp) {
                    _httpClient.SendPlayerDeathRequest(playerName);
                }
            }
        }

        private void CheckMeeting() {
            // Check whether a meeting starts
            CheckMeetingStart();
            // Check whether the meeting voted to exile a player
            CheckMeetingExiledPlayer();
            // Check whether the exile controller changes and thus the meeting ends
            CheckExileController();
        }

        private void CheckMeetingStart() {
            if (!CheckValidMeeting(out var meetingHud)) {
                return;
            }

            // If we are already in a meeting or the meeting state is not a starting state
            // or if we are not in-game at the moment, skip
            if (_inMeeting || meetingHud.state != 0 && meetingHud.state != 1 ||
                !_gameState.Equals(GameState.InGame)) {
                return;
            }
            
            Console.WriteLine("Meeting Called!");

            _inMeeting = true;

            if (_useHttp) {
                _httpClient.SendMeetingCalledRequest();
            } else if (_isDead) {
                if (_newDeath) {
                    ToggleDeafen();

                    _newDeath = false;
                }

                ToggleMute();
            } else {
                // Wait a bit before undeafening alive players, to prevent them hearing dead players
                Thread.Sleep(OverlapWaitTime);

                ToggleDeafen();
            }
        }

        private void CheckMeetingExiledPlayer() {
            if (!CheckValidMeeting(out var meetingHud)) {
                return;
            }
            
            // Only continue if we are in the last state of the meeting and have an exiled player
            if (meetingHud.state != 3 || meetingHud.exiledPlayer == IntPtr.Zero || _votedOff) {
                return;
            }
            
            var localPlayer = GetLocalPLayer();
            if (localPlayer == null) {
                return;
            }

            var exiledPlayerAddress = ((int) meetingHud.exiledPlayer).ToString("X");
                
            var exiledPlayerBytes = Mem.ReadBytes(exiledPlayerAddress, Utils.SizeOf<PlayerInfo>());
            if (exiledPlayerBytes != null && exiledPlayerBytes.Length != 0) {
                var exiledPlayer = Utils.FromBytes<PlayerInfo>(exiledPlayerBytes);

                if (GetLocalPLayer().PlayerInfo.PlayerId == exiledPlayer.PlayerId) {
                    // Local player was exiled (voted-off)
                    _votedOff = true;
                } else {
                    // Check whether this vote causes the game to end
                    var players = GetAllPlayers();

                    var crewCount = 0;
                    var imposterCount = 0;
                        
                    foreach (var player in players) {
                        // Skip dead and exiled players
                        if (player.PlayerInfo.PlayerId == exiledPlayer.PlayerId 
                            || player.PlayerInfo.IsDead == 1) {
                            continue;
                        }

                        if (player.PlayerInfo.IsImpostor == 1) {
                            imposterCount++;
                        } else {
                            crewCount++;
                        }
                    }

                    if (imposterCount == 0 || imposterCount >= crewCount) {
                        _voteCausesEnd = true;
                    }
                }
            }
        }

        private void CheckExileController() {
            if (!GetExileControllerActive(out var newExileState)) {
                return;
            }

            // If the last exile state was true, and now it is false, we ended the meeting
            if (!newExileState && _lastExileState) {
                // Sanity check, are we currently in a game and in a meeting?
                if (!_inMeeting || !_gameState.Equals(GameState.InGame)) {
                    return;
                }
                
                Console.WriteLine("Meeting Ended!");

                _inMeeting = false;
                
                if (_useHttp) {
                    if (!_voteCausesEnd) {
                        // Don't send a meeting end request if game is going to end, to prevent extra requests
                        _httpClient.SendMeetingEndRequest();
                    }
                } else if (_isDead) {
                    if (_isMuted) {
                        if (!_voteCausesEnd) {
                            // Wait a bit before unmuting dead players, to prevent alive players hearing them
                            // Don't have to wait if this meeting vote causes end
                            Thread.Sleep(OverlapWaitTime);
                        }

                        ToggleMute();
                    }
                } else {
                    if (!_voteCausesEnd) {
                        if (!_votedOff) {
                            ToggleDeafen();
                        } else {
                            Console.WriteLine("Local player was voted off, not deafening");
                        }
                    }
                }
            }
            
            // Updating last exile state
            _lastExileState = newExileState;
        }

        private bool CheckValidMeeting(out MeetingHud meetingHud) {
            if (!GetMeetingHud(out meetingHud)) {
                return false;
            }

            // Check if the meeting exists and the cached pointer is non-zero
            return !meetingHud.Equals(null) && meetingHud.cachedPtr != 0;
        }

        private List<PlayerData> GetAllPlayers() {
            var datas = new List<PlayerData>();

            var allPlayersAddress = Utils.GetPointerAddress(Offset.AllPlayersPointer, Offset.AllPlayersOffsets);

            if (allPlayersAddress == null) {
                return datas;
            }

            var allPlayersArray = Mem.ReadInt(allPlayersAddress);

            var arraySize = Mem.ReadInt(allPlayersArray.ToString("X") + "+C");

            var itemStart = Mem.ReadInt(allPlayersArray.ToString("X") + "+8");
            
            for (var i = 0; i < arraySize; i++) {
                var itemAddress = itemStart + 16 + 4 * i;
                
                var playerInfoAddress = Mem.ReadInt(itemAddress.ToString("X"));

                var playerInfoBytes = Mem.ReadBytes(
                    playerInfoAddress.ToString("X"), 
                    Utils.SizeOf<PlayerInfo>()
                );

                var playerInfo = Utils.FromBytes<PlayerInfo>(playerInfoBytes);

                if (playerInfo._object == IntPtr.Zero || playerInfo.PlayerName == IntPtr.Zero) {
                    continue;
                }
                
                var playerControl = Utils.FromBytes<PlayerControl>(Program.Mem.ReadBytes(
                    ((int) playerInfo._object).ToString("X"),
                    Utils.SizeOf<PlayerControl>()
                ));
                
                datas.Add(new PlayerData(playerInfo, playerControl));
            }
            
            return datas;
        }

        private PlayerData GetLocalPLayer() {
            foreach (PlayerData playerData in GetAllPlayers()) {
                if (playerData.IsLocalPlayer()) {
                    return playerData;
                }
            }

            return null;
        }

        private static bool GetAmongUsClient(out AmongUsClient client) {
            var amongUsClientAddress = Utils.GetPointerAddress(Offset.AmongUsClientPointer, Offset.AmongUsClientOffsets);

            if (amongUsClientAddress == null) {
                client = new AmongUsClient();
                return false;
            }

            var amongUsClientBytes = Mem.ReadBytes(amongUsClientAddress, Utils.SizeOf<AmongUsClient>());

            if (amongUsClientBytes == null || amongUsClientBytes.Length == 0) {
                client = new AmongUsClient();
                return false;
            }
            
            client = Utils.FromBytes<AmongUsClient>(amongUsClientBytes);
            return true;
        }

        private static bool GetShipStatus(out ShipStatus shipStatus) {
            var shipStatusAddress = Utils.GetPointerAddress(Offset.ShipStatusPointer, Offset.ShipStatusOffsets);

            if (shipStatusAddress == null) {
                shipStatus = new ShipStatus();
                return false;
            }
            
            var shipStatusBytes = Mem.ReadBytes(shipStatusAddress, Utils.SizeOf<ShipStatus>());

            if (shipStatusBytes == null || shipStatusBytes.Length == 0) {
                shipStatus = new ShipStatus();
                return false;
            }
            
            shipStatus = Utils.FromBytes<ShipStatus>(shipStatusBytes);
            return true;
        }
        
        private static bool GetMeetingHud(out MeetingHud meetingHud) {
            var meetingHudAddress = Utils.GetPointerAddress(Offset.MeetingHudPointer, Offset.MeetingHudOffsets);

            if (meetingHudAddress == null) {
                meetingHud = new MeetingHud();
                return false;
            }
            
            var meetingHudBytes = Mem.ReadBytes(meetingHudAddress, Utils.SizeOf<MeetingHud>());

            if (meetingHudBytes == null || meetingHudBytes.Length == 0) {
                meetingHud = new MeetingHud();
                return false;
            }
            
            meetingHud = Utils.FromBytes<MeetingHud>(meetingHudBytes);
            return true;
        }
        
        private static bool GetExileControllerActive(out bool active) {
            active = false;
            
            var exileControllerAddress =
                Utils.GetPointerAddress(Offset.ExileControllerPointer, Offset.ExileControllerOffsets);

            if (exileControllerAddress == null) {
                return false;
            }

            var cachedPointerValue = Mem.ReadInt(exileControllerAddress + "+8");

            active = cachedPointerValue != 0;
            return true;
        }

        private void ToggleMute() {
            _keyboard.SendDown(Keyboard.Input.CONTROL);
            _keyboard.SendDown(Keyboard.Input.F1);
            
            _keyboard.SendUp(Keyboard.Input.F1);
            _keyboard.SendUp(Keyboard.Input.CONTROL);
            
            if (_isDeafened) {
                _isDeafened = false;
                _isMuted = false;
            } else if (_isMuted) {
                _isMuted = false;
            } else {
                _isMuted = true;
            }
        }

        private void ToggleDeafen() {
            _keyboard.SendDown(Keyboard.Input.CONTROL);
            _keyboard.SendDown(Keyboard.Input.F2);
            
            _keyboard.SendUp(Keyboard.Input.F2);
            _keyboard.SendUp(Keyboard.Input.CONTROL);
            
            _isDeafened = !_isDeafened;
        }

        private enum GameState {
            Menu = 0,
            Lobby = 1,
            InGame = 2,
            EndScreen = 3
        }
        
    }
}