using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace AmongUsDiscordIntegration {
    public class Program {
        private const string AmongUsProcessName = "Among Us";
        private const string Ip = "192.168.2.200";
        private const string Port = "7919";

        private const int MeetingEndWaitTime = 7000;

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

        private bool _votedOff;

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

            _gameState = GameState.MENU;
            _inMeeting = false;
            _isDead = false;
            _newDeath = false;

            _votedOff = false;

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

            Methods.Init();
            
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

                if (newState.Equals(GameState.END_SCREEN)) {
                    Console.WriteLine("Game has ended!");

                    if (_useHttp) {
                        _httpClient.SendEndRequest();
                    } else if (_isDeafened || _isMuted) {
                        ToggleMute();
                    }
                }

                if (_gameState.Equals(GameState.LOBBY) && newState.Equals(GameState.IN_GAME)) {
                    Console.WriteLine("Game has started!");

                    _isDead = false;
                    _newDeath = false;

                    List<string> playerNames = new List<string>();
                    foreach (var playerData in GetAllPlayers()) {
                        playerNames.Add(Utils.ReadString(playerData.PlayerInfo.Value.PlayerName));
                    }

                    if (_useHttp) {
                        _httpClient.SendStartRequest(playerNames);
                    } else {
                        ToggleDeafen();
                    }
                }

                if (newState.Equals(GameState.LOBBY) || newState.Equals(GameState.MENU)) {
                    _inMeeting = false;
                    _isDead = false;
                    _newDeath = false;
                    _votedOff = false;
                    
                    if (_isDeafened || _isMuted) {
                        ToggleMute();
                    }
                }
                
                _gameState = newState;
            }
        }

        private void CheckPlayers() {
            if (!_gameState.Equals(GameState.IN_GAME)) {
                return;
            }
            
            var checkedPlayers = new List<string>();
            
            foreach (var playerData in GetAllPlayers()) {
                var playerName = Utils.ReadString(playerData.PlayerInfo.Value.PlayerName);
                var currentState = playerData.PlayerInfo.Value.IsDead == 1;
                    
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
                        } else if (playerData.IsLocalPlayer) {
                            Console.WriteLine($"Player {playerName} has died");
                            
                            _isDead = true;
                            _newDeath = true;

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
            if (!GetMeetingHud(out var meetingHud) || meetingHud.state == 4) {
                if (_inMeeting && _gameState.Equals(GameState.IN_GAME)) {
                    Console.WriteLine("Meeting Ended!");

                    _inMeeting = false;

                    // Wait a bit before muting to account for the vote result text
                    Thread.Sleep(MeetingEndWaitTime);
                    
                    if (_useHttp) {
                        _httpClient.SendMeetingEndRequest();
                    } else if (_isDead) {
                        if (_isMuted) {
                            ToggleMute();
                        }
                    } else {
                        if (!_votedOff) {
                            ToggleDeafen();
                        } else {
                            Console.WriteLine("Local player was voted off, not deafening");
                        }
                    }
                }
                
                return;
            }

            if (!_inMeeting  && meetingHud.state == 0 && _gameState.Equals(GameState.IN_GAME)) {
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
                    ToggleDeafen();
                }
            }

            if (meetingHud.state == 3 && meetingHud.exiledPlayer != IntPtr.Zero && !_votedOff) {
                var exiledPlayer = ((int) meetingHud.exiledPlayer).ToString("X");

                var playerInfoBytes = Mem.ReadBytes(exiledPlayer, Utils.SizeOf<PlayerInfo>());
                if (playerInfoBytes != null && playerInfoBytes.Length != 0) {
                    var playerInfo = Utils.FromBytes<PlayerInfo>(playerInfoBytes);

                    if (GetLocalPLayer().Instance.PlayerId == playerInfo.PlayerId) {
                        // Local player was exiled (voted-off)
                        _votedOff = true;
                    }
                }
            }
        }

        private List<PlayerData> GetAllPlayers() {
            var datas = new List<PlayerData>();

            // Find player pointer
            var playerAoB = Mem.ReadBytes(Offset.PlayerControl_Pointer, Utils.SizeOf<PlayerControl>());
            
            // Create AOB pattern
            var aobData = "";
            // Read 4 bytes for AOB pattern
            for (var i = 0; i < 4; i++) {
                aobData += playerAoB[i].ToString("X2") + " ";
            }
        
            aobData += "?? ?? ?? ??";

            //Console.WriteLine("AOB scan string: " + aobData);
            
            // Get result 
            var result = Mem.AoBScan(aobData, true);
            result.Wait();

            var results = result.Result;
            foreach (var x in results) {
                var bytes = Mem.ReadBytes(x.GetAddress(), Utils.SizeOf<PlayerControl>());
                var playerControl = Utils.FromBytes<PlayerControl>(bytes);
                
                // Filter garbage instance datas
                if (playerControl.SpawnFlags == 257 && playerControl.NetId < uint.MaxValue - 10000) {
                    datas.Add(new PlayerData {
                        Instance = playerControl,
                        offset_str = x.GetAddress(),
                        offset_ptr = new IntPtr((int) x)
                    });
                }
            }
            
            return datas;
        }

        private PlayerData GetLocalPLayer() {
            foreach (PlayerData playerData in GetAllPlayers()) {
                if (playerData.IsLocalPlayer) {
                    return playerData;
                }
            }

            return null;
        }

        private bool GetAmongUsClient(out AmongUsClient client) {
            var amongUsClientAddress = Utils.GetPointerAddress(Offset.AmongUsClient_Pointer, Offset.AmongUsClient_Offsets);

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

        private bool GetShipStatus(out ShipStatus shipStatus) {
            var shipStatusAddress = Utils.GetPointerAddress(Offset.ShipStatus_Pointer, Offset.ShipStatus_Offsets);

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
        
        private bool GetMeetingHud(out MeetingHud meetingHud) {
            var meetingHudAddress = Utils.GetPointerAddress(Offset.MeetingHud_Pointer, Offset.MeetingHud_Offsets);

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
            MENU = 0,
            LOBBY = 1,
            IN_GAME = 2,
            END_SCREEN = 3
        }
        
    }
}