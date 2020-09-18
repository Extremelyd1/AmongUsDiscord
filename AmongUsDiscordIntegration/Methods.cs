using System;
using System.Linq;

namespace AmongUsDiscordIntegration {
    public static class Methods {
        #region PlayerControl.GetData

        private static IntPtr _playerControlGetDataPtr = IntPtr.Zero;

        #endregion

        private static void InitPlayerControlGetData() {
            if (_playerControlGetDataPtr == IntPtr.Zero) {
                var aobScan = Program.Mem.AoBScan(Offset.PlayerControlGetData);
                aobScan.Wait();
                if (aobScan.Result.Count() == 1) {
                    _playerControlGetDataPtr = (IntPtr) aobScan.Result.First();
                }
            }
        }

        public static int CallPlayerControlGetData(IntPtr playerInfoPtr) {
            if (_playerControlGetDataPtr == IntPtr.Zero) {
                return -1;
            }
            
            var ptr = _playerControlGetDataPtr;
            var playerInfoAddress = Program.ProcessMemory.CallFunction(ptr, playerInfoPtr);
            return playerInfoAddress;

        }

        public static void Init() {
            InitPlayerControlGetData();
        }
    }
}