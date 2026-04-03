using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script mẫu MonoBehaviour dùng để minh họa vòng đời cơ bản của một component trong Unity.
/// </summary>
public class DemoMCP : MonoBehaviour
{
    /// <summary>
    /// Thiết lập dữ liệu ban đầu trước khi component bước vào vòng lặp Update.
    /// </summary>
    void Start()
    {
        // Khối chính: hiện chưa có nghiệp vụ cụ thể, giữ làm điểm mở rộng cho logic khởi tạo.
    }

    /// <summary>
    /// Cập nhật logic theo từng khung hình khi game đang chạy.
    /// </summary>
    void Update()
    {
        // Khối chính: hiện chưa có nghiệp vụ cụ thể, giữ làm điểm mở rộng cho logic runtime.
    }
}
