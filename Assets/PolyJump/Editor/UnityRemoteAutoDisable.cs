#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace PolyJump.Editor
{
    /// <summary>
    /// Tự động tắt Unity Remote một lần trong Editor để tránh lỗi ADB khi không có thiết bị kết nối.
    /// </summary>
    internal static class UnityRemoteAutoDisable
    {
        private const string OnceFlagKey = "PolyJump.UnityRemote.DisabledOnce";

        [InitializeOnLoadMethod]
        /// <summary>
        /// Thực hiện nghiệp vụ Disable Unity Remote Once theo ngữ cảnh sử dụng của script.
        /// </summary>
        private static void DisableUnityRemoteOnce()
        {
            // Khối chính: chuẩn bị dữ liệu cục bộ và điều phối các bước xử lý của hàm.
            if (EditorPrefs.GetBool(OnceFlagKey, false))
            {
                return;
            }

            // Khối điều kiện: rẽ nhánh xử lý theo dữ liệu và trạng thái hiện tại.
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
