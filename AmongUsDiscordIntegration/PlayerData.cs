using System;

namespace AmongUsDiscordIntegration {
    public class PlayerData {
        private readonly PlayerControl _playerControl;
        private readonly IntPtr _playerControlOffsetPtr;

        private bool _playerInfoCached;
        private string _playerInfoOffset;

        public PlayerData(PlayerControl playerControl, IntPtr offsetPtr) {
            _playerControl = playerControl;
            _playerControlOffsetPtr = offsetPtr;

            _playerInfoCached = false;
        }

        public PlayerControl PlayerControl => _playerControl;

        public PlayerInfo PlayerInfo {
            get {
                PlayerInfo playerInfo;
                
                if (!_playerInfoCached) {
                    var ptr = Methods.CallPlayerControlGetData(_playerControlOffsetPtr);
                    _playerInfoOffset = ptr.GetAddress();
                    playerInfo =
                        Utils.FromBytes<PlayerInfo>(
                            Program.Mem.ReadBytes(_playerInfoOffset, Utils.SizeOf<PlayerInfo>()));

                    _playerInfoCached = true;
                } else {
                    playerInfo = Utils.FromBytes<PlayerInfo>(
                        Program.Mem.ReadBytes(_playerInfoOffset,
                        Utils.SizeOf<PlayerInfo>())
                    );
                }

                return playerInfo;
            }
        }
        
        public bool IsLocalPlayer() {
            return _playerControl.myLight != IntPtr.Zero;
        }

        public Vector2 Position => IsLocalPlayer() ? GetMyPosition() : GetSyncPosition();

        private Vector2 GetSyncPosition() {
            try {
                const int offsetVec2Position = 60;
                const int offsetVec2SizeOf = 8;
                
                var netTransform = ((int) _playerControl.NetTransform + offsetVec2Position).ToString("X");
                var vec2Data = Program.Mem.ReadBytes($"{netTransform}", offsetVec2SizeOf);
                
                if (vec2Data != null && vec2Data.Length != 0) {
                    var vec2 = Utils.FromBytes<Vector2>(vec2Data);
                    return vec2;
                }
                
                return Vector2.Zero;
            } catch (Exception) {
                return Vector2.Zero;
            }
        }

        private Vector2 GetMyPosition() {
            try {
                const int offsetVec2Position = 80;
                const int offsetVec2SizeOf = 8;
                
                var netTransform = ((int) _playerControl.NetTransform + offsetVec2Position).ToString("X");
                var vec2Data = Program.Mem.ReadBytes($"{netTransform}", offsetVec2SizeOf);

                if (vec2Data != null && vec2Data.Length != 0) {
                    var vec2 = Utils.FromBytes<Vector2>(vec2Data);
                    return vec2;
                }

                return Vector2.Zero;
            } catch {
                return Vector2.Zero;
            }
        }
    }
}