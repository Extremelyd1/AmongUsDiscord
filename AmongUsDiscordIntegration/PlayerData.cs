using System;

namespace AmongUsDiscordIntegration {
    public class PlayerData {
        private readonly PlayerInfo _playerInfo;
        private readonly PlayerControl _playerControl;

        public PlayerData(PlayerInfo playerInfo, PlayerControl playerControl) {
            _playerInfo = playerInfo;
            _playerControl = playerControl;
        }

        public PlayerControl PlayerControl => _playerControl;

        public PlayerInfo PlayerInfo => _playerInfo;
        
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