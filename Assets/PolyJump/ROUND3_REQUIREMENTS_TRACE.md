# Truy Vết Yêu Cầu Vòng 3 (Requirement-to-Code Trace)

Tài liệu này đối chiếu 9 yêu cầu vòng 3 với code hiện tại trong dự án PolyJump, chỉ rõ:

- Code đang nằm ở đâu.
- Luồng hoạt động hiện tại.
- Mức độ đáp ứng.
- Khoảng thiếu cần bổ sung để đạt đúng đề bài.

## Tổng quan nhanh mức đáp ứng

| #   | Yêu cầu                                                        | Mức đáp ứng hiện tại                                |
| --- | -------------------------------------------------------------- | --------------------------------------------------- |
| 1   | Tùy chọn luật chơi 3/5 phút hoặc về đích nhanh nhất (<=5 phút) | Một phần                                            |
| 2   | Chơi nhiều lần nhưng chỉ ghi nhận thành tích lần cuối          | Đạt (cho chế độ sự kiện)                            |
| 3   | Kết quả mỗi lần chơi đều ghi nhận và gửi server                | Một phần                                            |
| 4   | Quản trị mở khung giờ chơi (ví dụ 7h-9h)                       | Đạt (cho event)                                     |
| 5   | Hết giờ mở: xem top 10/20 + thông tin Họ tên/SĐT/Email         | Một phần                                            |
| 6   | Có giải pháp chống spam score                                  | Một phần                                            |
| 7   | Tổ chức dự án rõ ràng, giải thích được                         | Đạt                                                 |
| 8   | Đồ họa đẹp, phù hợp quy định FPT và nhà nước                   | Một phần (kỹ thuật có, pháp lý cần checklist riêng) |
| 9   | Cấu trúc client-server rõ ràng, có API lưu score               | Đạt (dùng PlayFab API + CloudScript)                |

---

## 1) Tùy chọn luật chơi: 3 hoặc 5 phút; hoặc về đích sớm nhất (<=5 phút)

### Mức đáp ứng: Một phần

### Code liên quan

- Bộ đếm thời gian cấu hình tại [Assets/PolyJump/Scripts/GameManager.cs](Assets/PolyJump/Scripts/GameManager.cs#L63).
- Khởi tạo thời gian khi bắt đầu run tại [Assets/PolyJump/Scripts/GameManager.cs](Assets/PolyJump/Scripts/GameManager.cs#L235).
- Trừ thời gian liên tục khi chơi tại [Assets/PolyJump/Scripts/GameManager.cs](Assets/PolyJump/Scripts/GameManager.cs#L166).
- Hết thời gian thì GameOver tại [Assets/PolyJump/Scripts/GameManager.cs](Assets/PolyJump/Scripts/GameManager.cs#L172).

### Cách hoạt động hiện tại

1. Game dùng một biến duy nhất `startTimeSeconds` (mặc định 180 giây).
2. Khi vào run, `_remainingTime` lấy từ `startTimeSeconds`.
3. Trong state Playing/Quiz, thời gian giảm theo `Time.unscaledDeltaTime`.
4. Về 0 thì kết thúc run.

### Khoảng thiếu so với yêu cầu

- Chưa có menu cho người chơi chọn rõ 3 phút hoặc 5 phút.
- Chưa có mode “về đích nhanh nhất trong tối đa 5 phút”.
- Chưa có khái niệm “đích hoàn thành” (goal/finish line) trong gameplay hiện tại.

### Đề xuất triển khai

1. Thêm enum `MatchRuleMode` trong `GameManager` (TimeAttack3Min, TimeAttack5Min, Speedrun5MinCap).
2. Thêm UI chọn mode tại `Panel_Start` và truyền mode trước `StartGameplay()`.
3. Với mode speedrun: thêm điều kiện hoàn thành (ví dụ đạt độ cao mục tiêu hoặc số mốc platform), tính thành tích theo thời gian hoàn thành.
4. Chuẩn hóa payload gửi server kèm `mode`, `attemptId`, `durationSeconds`.

---

## 2) Mỗi người chơi chơi nhiều lần nhưng chỉ ghi nhận thành tích lần cuối

### Mức đáp ứng: Đạt (cho chế độ sự kiện)

### Code liên quan

- Điểm run gửi khi GameOver tại [Assets/PolyJump/Scripts/GameManager.cs](Assets/PolyJump/Scripts/GameManager.cs#L509).
- Trigger gửi điểm event khi event mở tại [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L414).
- Gửi điểm event qua CloudScript tại [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L513).
- Server ghi đè trực tiếp giá trị statistic event bằng điểm vừa gửi tại [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js#L188).

### Cách hoạt động hiện tại

1. Sau mỗi run, client gọi `SubmitScore(finalScore)`.
2. Trong cùng luồng đó, nếu event đang mở thì hàm submit event luôn được gọi và gửi điểm run hiện tại lên server.
3. CloudScript dùng `UpdatePlayerStatistics` với `Value = score` để ghi đè trực tiếp điểm event bằng lần gửi mới nhất.
4. Vì vậy leaderboard event đang vận hành đúng quy tắc “ghi nhận thành tích lần cuối”.

### Lưu ý phạm vi

- Logic leaderboard thường hiện vẫn là highscore (chỉ cập nhật khi điểm cao hơn), nhưng đây là bảng riêng và không mâu thuẫn với yêu cầu số 2 nếu yêu cầu số 2 áp cho chế độ sự kiện.

### Kết luận cho yêu cầu số 2

1. Với cách hiểu áp cho chế độ sự kiện: yêu cầu đã được đáp ứng.
2. Không bắt buộc đổi leaderboard thường sang “last attempt” nếu mục tiêu chấm điểm là bảng sự kiện.

---

## 3) Kết quả mỗi lần chơi phải ghi nhận và gửi về server

### Mức đáp ứng: Một phần

### Code liên quan

- Điểm được gửi ở cuối run: [Assets/PolyJump/Scripts/GameManager.cs](Assets/PolyJump/Scripts/GameManager.cs#L509).
- API update statistic thường: [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L1128), [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L1139).
- Event score qua CloudScript: [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L513), [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L517).
- Server update event score: [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js#L156).

### Cách hoạt động hiện tại

1. Mỗi run gọi submit một lần ở GameOver.
2. Score thường: có thể bị bỏ qua nếu không vượt highscore.
3. Score event: khi event mở thì gửi qua CloudScript; fail thì fallback update trực tiếp.

### Khoảng thiếu so với yêu cầu

- Chưa ghi nhận đầy đủ mọi lần chơi (do skip score thường khi không vượt highscore).
- Chưa có bảng lịch sử attempt server-side tách biệt (attempt log).

### Đề xuất triển khai

1. Tạo API/CloudScript `SubmitRunResult` để ghi log mọi run (attemptId, score, duration, mode, timestamp).
2. Giữ leaderboard là một phần hiển thị; tách hẳn kho dữ liệu lịch sử.
3. Thêm retry queue phía client khi mạng chập chờn.

---

## 4) Quản trị mở thời gian chơi theo khung giờ phù hợp (ví dụ 7:00-9:00)

### Mức đáp ứng: Đạt (đối với event window)

### Code liên quan

- Key cấu hình event ở client: [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L80), [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L81), [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L82).
- Đọc Title Data: [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L470).
- Kiểm tra event open/close: [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L868).
- CloudScript cũng kiểm tra lại cửa sổ giờ: [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js#L2), [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js#L95), [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js#L118).

### Cách hoạt động hiện tại

1. Admin set `EventEnabled`, `EventStart/EventEnd` (hoặc UTC variant) trên PlayFab Title Data.
2. Client fetch Title Data và tự kiểm tra event open.
3. CloudScript kiểm tra lại trên server trước khi chấp nhận điểm event.

### Nhận xét

- Đây là thiết kế đúng hướng client + server validation.
- Có parse đa định dạng thời gian, phù hợp vận hành thực tế.

---

## 5) Hết thời gian mở: xem top 10/20 + thông tin Họ tên, SĐT, Email

### Mức đáp ứng: Một phần

### Code liên quan

- Cấu hình số lượng top: [Assets/PolyJump/Scripts/LeaderboardUiManager.cs](Assets/PolyJump/Scripts/LeaderboardUiManager.cs#L64).
- Query top leaderboard: [Assets/PolyJump/Scripts/LeaderboardUiManager.cs](Assets/PolyJump/Scripts/LeaderboardUiManager.cs#L774), [Assets/PolyJump/Scripts/LeaderboardUiManager.cs](Assets/PolyJump/Scripts/LeaderboardUiManager.cs#L965).
- Cơ chế event hết giờ/đóng: [Assets/PolyJump/Scripts/LeaderboardUiManager.cs](Assets/PolyJump/Scripts/LeaderboardUiManager.cs#L1669).
- Dữ liệu profile người chơi lưu trên PlayFab: [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L1191), [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L1196), [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L1201).

### Cách hoạt động hiện tại

1. Hệ thống lấy top N theo `maxLeaderboardEntries` (mặc định 10, có thể set 20 trong Inspector).
2. UI leaderboard hiển thị chủ yếu tên hiển thị + điểm + hạng.
3. Profile có lưu Name/Phone/Email nhưng chưa join vào UI top list để xuất danh sách trao quà đầy đủ thông tin liên hệ.

### Khoảng thiếu so với yêu cầu

- Chưa có màn hình/admin export top N kèm Họ tên/SĐT/Email trong một danh sách hoàn chỉnh.

### Đề xuất triển khai

1. Sau khi lấy top N, gọi API profile theo danh sách PlayFabId để lấy User Data (`UserName`, `Phone`, `Email`).
2. Tạo admin panel hoặc endpoint export CSV cho trao quà.
3. Chỉ mở chức năng này cho tài khoản quản trị.

---

## 6) Có giải pháp chống spam score

### Mức đáp ứng: Một phần

### Code liên quan

- Chỉ update score thường khi vượt highscore: [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L390).
- Cooldown nút refresh leaderboard: [Assets/PolyJump/Scripts/LeaderboardUiManager.cs](Assets/PolyJump/Scripts/LeaderboardUiManager.cs#L63), [Assets/PolyJump/Scripts/LeaderboardUiManager.cs](Assets/PolyJump/Scripts/LeaderboardUiManager.cs#L644).
- Chỉ nhận score event khi trong cửa sổ hợp lệ: [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L421), [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js#L118).
- Validate score không âm ở server: [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js#L127).

### Cách hoạt động hiện tại

- Có anti-spam mức cơ bản: giảm số lần ghi điểm thường, giới hạn refresh, kiểm tra thời gian event, chặn score âm.

### Khoảng thiếu so với yêu cầu anti-cheat/anti-spam mạnh

- Chưa có rate-limit theo account/IP/thiết bị.
- Chưa có chữ ký payload hoặc token chống gửi request giả mạo.
- Chưa có phát hiện hành vi bất thường theo vận tốc tăng điểm/thời lượng run.

### Đề xuất triển khai

1. Đưa toàn bộ xác thực score vào CloudScript (không cho client update statistic trực tiếp với bảng thi).
2. Thêm attempt token một lần (one-time token) phát khi Start run.
3. Thêm rate-limit: ví dụ tối đa X lần submit/phút/tài khoản.
4. Log anomaly để duyệt thủ công hoặc tự động cấm điểm bất thường.

---

## 7) Tổ chức dự án theo cấu trúc rõ ràng và có thể giải thích được

### Mức đáp ứng: Đạt

### Code/cấu trúc liên quan

- Các manager theo trách nhiệm rõ ràng:
  - [Assets/PolyJump/Scripts/GameManager.cs](Assets/PolyJump/Scripts/GameManager.cs#L17)
  - [Assets/PolyJump/Scripts/PlayerController.cs](Assets/PolyJump/Scripts/PlayerController.cs#L7)
  - [Assets/PolyJump/Scripts/LevelSpawner.cs](Assets/PolyJump/Scripts/LevelSpawner.cs#L6)
  - [Assets/PolyJump/Scripts/QuizManager.cs](Assets/PolyJump/Scripts/QuizManager.cs#L22)
  - [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L16)
  - [Assets/PolyJump/Scripts/LeaderboardUiManager.cs](Assets/PolyJump/Scripts/LeaderboardUiManager.cs#L12)
  - [Assets/PolyJump/Scripts/AudioManager.cs](Assets/PolyJump/Scripts/AudioManager.cs#L11)
- Dữ liệu quiz tách riêng: [Assets/PolyJump/Resources/QuizData.json](Assets/PolyJump/Resources/QuizData.json)
- CloudScript server tách riêng: [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js)

### Cách hoạt động hiện tại

- Project tách rõ gameplay, auth, leaderboard, quiz, audio, editor tooling.
- Dễ giải thích theo kiến trúc thành phần (component-based + manager orchestration).

### Đề xuất tăng tính “bảo vệ đồ án” khi thuyết trình

1. Thêm sơ đồ luồng sequence trong tài liệu kỹ thuật.
2. Thêm file README kiến trúc ngắn ở thư mục PolyJump.

---

## 8) Đồ họa đẹp, phù hợp quy định FPT Polytechnic và quy định nhà nước

### Mức đáp ứng: Một phần

### Code/asset liên quan

- Palette nhận diện FPT trong builder:
  - [Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs](Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs#L24)
  - [Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs](Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs#L25)
- Tạo UI theo palette cam/xanh:
  - [Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs](Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs#L928)
  - [Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs](Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs#L929)
  - [Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs](Assets/PolyJump/Editor/PolyJumpPrototypeBuilder.cs#L930)
- Bộ asset UI/chibi/background nằm tại thư mục `Assets/PolyJump/UI`.

### Cách hoạt động hiện tại

- Art style đồng nhất học đường, mascot ong, tông màu nhận diện phù hợp.
- Có nhiều sprite/button/background theo chủ đề FPoly.

### Khoảng thiếu so với yêu cầu “quy định nhà nước”

- Chưa có checklist pháp lý/kiểm duyệt nội dung tích hợp trong pipeline.
- Chưa có tài liệu bản quyền tài sản đồ họa trong repo (ngoài một số gói có license file riêng).

### Đề xuất triển khai

1. Tạo checklist kiểm duyệt nội dung (không phản cảm, không vi phạm thuần phong mỹ tục).
2. Tạo file bản quyền asset (nguồn, license, phạm vi sử dụng).
3. Chốt guideline visual chính thức (màu, icon, typography) cho toàn bộ scene.

---

## 9) Cấu trúc client-server rõ ràng và có API lưu score

### Mức đáp ứng: Đạt

### Code liên quan (luồng API)

- Client gọi gửi điểm sau mỗi run: [Assets/PolyJump/Scripts/GameManager.cs](Assets/PolyJump/Scripts/GameManager.cs#L509).
- API lưu score thường (PlayFab UpdatePlayerStatistics):
  - [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L1128)
  - [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L1139)
- API event qua CloudScript:
  - Request tạo ở [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L515)
  - FunctionName ở [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L517)
  - Payload ở [Assets/PolyJump/Scripts/PlayFabAuthManager.cs](Assets/PolyJump/Scripts/PlayFabAuthManager.cs#L518)
- Server xử lý và ghi score event:
  - CloudScript handler: [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js#L1)
  - Update server-side statistic: [Assets/PolyJump/CloudScript/SubmitEventScore.js](Assets/PolyJump/CloudScript/SubmitEventScore.js#L156)

### Cách hoạt động hiện tại

1. Client-side chịu trách nhiệm gameplay, UI, submit trigger.
2. PlayFab làm backend dịch vụ (auth, leaderboard, title data).
3. Event score được xác thực thêm qua CloudScript server trước khi chấp nhận.

### Nhận xét

- Kiến trúc client-server đã rõ.
- API lưu score đã có.
- Nếu muốn chuẩn enterprise hơn: chuyển bảng thi chính sang server-authoritative hoàn toàn.

---

## Kết luận và ưu tiên nâng cấp để đạt 100% vòng 3

Ưu tiên 1 (bắt buộc):

1. Bổ sung luật chơi chọn mode 3/5 phút + mode về đích <= 5 phút.
2. Đổi quy tắc ghi nhận thành tích sang “lần cuối cùng” cho bảng chính.
3. Ghi log mọi lần chơi (attempt log) lên server.

Ưu tiên 2 (vận hành giải thưởng):

1. Tạo danh sách top 10/20 kèm Họ tên/SĐT/Email cho quản trị.
2. Export CSV hoặc trang admin riêng để trao quà.

Ưu tiên 3 (chống spam nâng cao):

1. Rate-limit + token hóa attempt.
2. Đưa xác thực score chính vào CloudScript/server.

Sau khi hoàn thành 3 nhóm ưu tiên trên, dự án sẽ bám sát đề vòng 3 cả về kỹ thuật lẫn vận hành.
