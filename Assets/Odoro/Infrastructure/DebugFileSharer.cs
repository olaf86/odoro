using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Odoro
{
    public static class DebugFileSharer
    {
        public static bool IsAvailable
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static bool ShareFile(string path)
        {
            if (!IsAvailable || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

#if UNITY_IOS && !UNITY_EDITOR
            OdoroShareFile(path);
            return true;
#else
            Debug.Log($"Debug file sharing is unavailable for this platform: {path}");
            return false;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void OdoroShareFile(string path);
#endif
    }
}
