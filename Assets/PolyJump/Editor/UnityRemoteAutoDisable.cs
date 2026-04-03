#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace PolyJump.Editor
{
    internal static class UnityRemoteAutoDisable
    {
        private const string OnceFlagKey = "PolyJump.UnityRemote.DisabledOnce";

        [InitializeOnLoadMethod]
        private static void DisableUnityRemoteOnce()
        {
            if (EditorPrefs.GetBool(OnceFlagKey, false))
            {
                return;
            }

            if (!string.Equals(EditorSettings.unityRemoteDevice, "None", StringComparison.OrdinalIgnoreCase))
            {
                EditorSettings.unityRemoteDevice = "None";
                Debug.Log("[PolyJump] Unity Remote device set to 'None' to avoid adb timeout when no device/emulator is connected.");
            }

            EditorPrefs.SetBool(OnceFlagKey, true);
        }
    }
}
#endif
