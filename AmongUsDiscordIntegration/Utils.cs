using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AmongUsDiscordIntegration {
    public static class Utils {
        private static readonly Dictionary<(Type, string), int> OffsetMap = new Dictionary<(Type, string), int>();

        public static T FromBytes<T>(byte[] bytes) {
            var gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var data = (T) Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(T));
            gcHandle.Free();
            return data;
        }

        public static int SizeOf<T>() {
            return Marshal.SizeOf(typeof(T));
        }


        public static string GetAddress(this long value) {
            return value.ToString("X");
        }

        public static string GetAddress(this int value) {
            return value.ToString("X");
        }

        public static string GetAddress(this uint value) {
            return value.ToString("X");
        }

        public static string GetAddress(this IntPtr value) {
            return value.ToInt32().GetAddress();
        }

        public static string GetAddress(this UIntPtr value) {
            return value.ToUInt32().GetAddress();
        }

        public static IntPtr Sum(this IntPtr ptr, IntPtr ptr2) {
            return (IntPtr) (ptr.ToInt32() + ptr2.ToInt32());
        }

        public static IntPtr Sum(this IntPtr ptr, UIntPtr ptr2) {
            return (IntPtr) (ptr.ToInt32() + (int) ptr2.ToUInt32());
        }

        public static IntPtr Sum(this UIntPtr ptr, IntPtr ptr2) {
            return (IntPtr) (ptr.ToUInt32() + ptr2.ToInt32());
        }

        public static IntPtr Sum(this int ptr, IntPtr ptr2) {
            return (IntPtr) (ptr + ptr2.ToInt32());
        }

        public static IntPtr Sum(this IntPtr ptr, int ptr2) {
            return (IntPtr) (ptr.ToInt32() + ptr2);
        }

        public static IntPtr GetMemberPointer(IntPtr basePtr, Type type, string fieldName) {
            var offset = GetOffset(type, fieldName);
            return basePtr.Sum(offset);
        }

        public static int GetOffset(Type type, string fieldName) {
            if (OffsetMap.ContainsKey((type, fieldName))) {
                return OffsetMap[(type, fieldName)];
            }

            var field = type.GetField(fieldName);
            var attributes = field.GetCustomAttributes(true);
            foreach (var attr in attributes) {
                if (attr is FieldOffsetAttribute attribute) {
                    OffsetMap.Add((type, fieldName), attribute.Value);
                    return attribute.Value;
                }
            }

            return -1;
        }

        public static string ReadString(IntPtr offset) {
            // string pointer + 8 = length
            var length = Program.Mem.ReadInt(offset.Sum(8).GetAddress());

            // unit of string is 2byte.
            var formatLength = length * 2;

            // string pointer + 12 = value
            var strByte = Program.Mem.ReadBytes(offset.Sum(12).GetAddress(), formatLength);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strByte.Length; i += 2) {
                // english = 1byte
                if (strByte[i + 1] == 0) {
                    sb.Append((char) strByte[i]);
                } else {
                    // korean & unicode = 2byte
                    sb.Append(Encoding.Unicode.GetString(new[] {strByte[i], strByte[i + 1]}));
                }
            }

            return sb.ToString();
        }

        public static string GetPointerAddress(string baseAddress, string[] offsets) {
            var currentReadBytes = Program.Mem.ReadBytes(baseAddress, 4);

            if (currentReadBytes == null || currentReadBytes.Length == 0) {
                return null;
            }

            for (var i = 0; i < offsets.Length; i++) {
                var offsetAddress = "";

                foreach (var readByte in currentReadBytes) {
                    offsetAddress = readByte.ToString("X2") + offsetAddress;
                }

                if (i == offsets.Length - 1) {
                    var longAddress = 
                        Convert.ToInt64(offsetAddress,16) + Convert.ToInt64(offsets[i],16);
                    
                    return longAddress.ToString("X");
                }

                offsetAddress = offsetAddress + "+" + offsets[i];
                
                currentReadBytes = Program.Mem.ReadBytes(offsetAddress, 4);

                if (currentReadBytes == null || currentReadBytes.Length == 0) {
                    return null;
                }
            }

            return "";
        }
    }
}