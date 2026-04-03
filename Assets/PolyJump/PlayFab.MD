BƯỚC 1: THIẾT LẬP PLAYFAB (QUẢN LÝ TÀI KHOẢN)

1. Bật quyền cho phép Client tạo tài khoản:
   o Vào Settings (Bánh răng) > Title Settings > Client Profile.
   o Tích chọn: Allow client to post player statistics.
   o Tích chọn: Allow client to change display name.
2. Thiết lập Leaderboard:
   o Vào Leaderboards > New Leaderboard.
   o Statistic name: HighScore_Normal (Max).

---

BƯỚC 2: CÀI ĐẶT UNITY SDK
Mày chỉ cần duy nhất PlayFab SDK. Nếu đã cài rồi thì giữ nguyên. Không cần bộ Google Sign-In nữa, mày có thể xóa nó đi cho nhẹ project.

---

BƯỚC 3: VIẾT CODE LOGIC (ĐĂNG KÝ & ĐĂNG NHẬP)
Mày tạo script PlayFabAccountManager.cs. Điểm mấu chốt ở đây là: PlayFab dùng Email làm khóa chính, còn Họ tên/SĐT mình sẽ lưu vào User Data.
C#
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using System.Collections.Generic;

public class PlayFabAccountManager : MonoBehaviour
{
// 1. ĐĂNG KÝ TÀI KHOẢN MỚI
public void RegisterUser(string email, string password, string fullName, string phone)
{
var request = new RegisterPlayFabUserRequest {
Email = email,
Password = password,
Username = email, // Dùng email làm username luôn cho tiện
DisplayName = fullName,
RequireBothUsernameAndEmail = false
};

        PlayFabClientAPI.RegisterPlayFabUser(request, result => {
            Debug.Log("Đăng ký thành công!");
            // Sau khi đăng ký xong, lưu SĐT vào User Data
            SaveExtraInfo(phone);
        }, OnError);
    }

    // 2. ĐĂNG NHẬP (Dùng Email)
    public void LoginUser(string email, string password)
    {
        var request = new LoginWithEmailAddressRequest {
            Email = email,
            Password = password
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, result => {
            Debug.Log("Đăng nhập thành công!");
            // Chuyển Scene vào Game
        }, OnError);
    }

    // 3. LƯU THÔNG TIN BỔ SUNG (SĐT)
    void SaveExtraInfo(string phone)
    {
        var request = new UpdateUserDataRequest {
            Data = new Dictionary<string, string> { { "Phone", phone } }
        };
        PlayFabClientAPI.UpdateUserData(request, result => Debug.Log("Lưu SĐT thành công"), OnError);
    }

    void OnError(PlayFabError error) { Debug.LogError(error.GenerateErrorReport()); }

}

---

BƯỚC 4: THIẾT KẾ UI TRONG GAME
Mày cần tạo 2 cái Panel (Bảng) trong Canvas:

1. Register Panel:
   o 4 Input Field: Name_Input, Email_Input, Phone_Input, Password_Input.
   o 1 Button "Đăng Ký": Gọi hàm RegisterUser.
2. Login Panel:
   o 2 Input Field: Email_Input, Password_Input.
   o 1 Button "Đăng Nhập": Gọi hàm LoginUser.
   Mẹo: PlayFab yêu cầu mật khẩu phải từ 6-100 ký tự. Mày nên viết một đoạn code nhỏ để check nếu người chơi nhập thiếu thì báo lỗi ngay trên UI.

---

BƯỚC 5: LƯU ĐIỂM CAO NHẤT (LEADERBOARD)
Hàm này không đổi, mày gọi nó mỗi khi người chơi "Game Over".
C#
public void SubmitHighScore(int score)
{
var request = new UpdatePlayerStatisticsRequest {
Statistics = new List<StatisticUpdate> {
new StatisticUpdate { StatisticName = "HighScore_Normal", Value = score }
}
};
PlayFabClientAPI.UpdatePlayerStatistics(request, result => Debug.Log("Đã cập nhật bảng xếp hạng"), null);
}

---

🛡️ CHIẾN THUẬT PHỎNG VẤN (TỐI ƯU CHO PHƯƠNG ÁN NÀY)
• Giám khảo hỏi: "Tại sao em không dùng các bên thứ 3 như Facebook hay Google cho nhanh?"
• Trả lời: "Dạ, em muốn xây dựng một hệ thống tài khoản độc lập để làm chủ hoàn toàn dữ liệu người dùng. Việc tự thiết kế Form đăng ký giúp em thu thập được các thông tin đặc thù như Số điện thoại ngay từ đầu, phục vụ cho việc định danh và liên lạc với người chơi trong các sự kiện offline của dự án mà không cần xin thêm quyền từ Google."
• Điểm nhấn kỹ thuật: "Hệ thống của em lưu trữ mật khẩu dưới dạng Mã hóa (Hashing) trên Server của PlayFab, đảm bảo an toàn tuyệt đối cho người dùng ngay cả khi Database bị truy cập trái phép."
Khoa lưu ý: Vì mày muốn đăng nhập bằng SĐT hoặc Email, nhưng PlayFab mặc định chỉ có LoginWithEmailAddress hoặc LoginWithPlayFabAccount (Username).
• Cách làm đơn giản nhất: Luôn yêu cầu người chơi đăng nhập bằng Email.
• Cách làm nâng cao: Nếu muốn gõ SĐT vẫn đăng nhập được, mày có thể dùng SĐT để làm Username khi đăng ký. Lúc đó dùng hàm LoginWithPlayFabAccount.
