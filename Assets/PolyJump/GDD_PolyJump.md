# GAME DESIGN DOCUMENT (GDD)

## 1. Tổng Quan Dự Án (Project Overview)

Tên dự án: PolyJump: Hành Trình Ong Vàng

Thể loại: 2D Endless Platformer kết hợp Educational Quiz Game

Nền tảng mục tiêu:

- Hiện tại: Unity trên PC (Editor, standalone/web tương thích)
- Mục tiêu mở rộng: Android portrait

Đối tượng người chơi:

- Sinh viên FPT Polytechnic
- Học sinh lớp 12 đang hướng nghiệp
- Người chơi casual muốn game nhẹ, nhanh, có tính giáo dục

Tầm nhìn sản phẩm:

- Một game platform nhịp nhanh, chu kỳ ngắn, dễ chơi lại
- Lồng ghép câu hỏi hướng nghiệp FPoly để vừa chơi vừa nhớ thông tin
- Tạo động lực cạnh tranh qua bảng xếp hạng PlayFab

Cốt truyện sơ lược:

- Nhân vật ong vàng bắt đầu tại khuôn viên FPoly.
- Mỗi lần nhảy là một bước vượt qua chướng ngại và thử thách kiến thức.
- Người chơi thu thập điểm cao nhất, vượt top bảng xếp hạng và tham gia các sự kiện đua top theo thời gian.

Giá trị cốt lõi:

- Fast and replayable.
- Learn by play.
- Competitive but friendly.

## 2. Lối Chơi Chính (Core Gameplay)

### 2.1 Core Loop

Chu kỳ chơi 1 vòng:

1. Vào Menu, bấm Play.
2. Nhân vật nhảy liên tục trên các platform, di chuyển ngang để cân bằng.
3. Chạy đua với bộ đếm thời gian.
4. Chạm Quiz Platform để trả lời câu hỏi.
5. Trả lời đúng thì cộng thời gian, sai thì trừ thời gian.
6. Tiếp tục leo cao để tăng điểm.
7. Hết giờ hoặc rơi xuống dưới camera thì Game Over.
8. Gửi điểm lên leaderboard, xem xếp hạng, Replay.

### 2.2 Trạng Thái Hệ Thống

Máy trạng thái gameplay:

1. Menu
2. Playing
3. Quiz
4. GameOver

Luồng chuyển:

- Menu -> Playing khi bấm Play.
- Playing -> Quiz khi chạm Quiz Platform.
- Quiz -> Playing sau feedback và resolve time delta.
- Playing -> GameOver khi hết giờ hoặc fall-out.
- GameOver -> Menu/Playing khi Replay.

### 2.3 Cơ Chế Điều Khiển

PC:

- A/D hoặc mũi tên trái/phải để di chuyển ngang.
- Nhảy được xử lý theo physics và trigger platform.

Mobile:

- Input kéo ngang theo touch delta.
- Có dead-zone, response exponent, acceleration/deceleration để điều khiển mượt.
- Khi ngừng kéo tay, nhân vật giảm tốc và dừng theo smoothing.

### 2.4 Hệ Thống Platform

Normal Platform (cam):

- Chức năng: nền nhảy cơ bản.
- Tỷ lệ spawn cao nhất.

Quiz Platform (xanh/nhận diện quiz):

- Chức năng: mở quiz panel khi chạm.
- Sau khi quiz resolve, platform quiz được convert thành normal để tránh trigger lặp.

Spawner:

- Sinh platform theo khoảng cách Y trong khoảng min/max.
- Giới hạn X trong horizontal range.
- Duy trì số lượng platform phía trước player và dọn dẹp platform cũ phía sau.

### 2.5 Điểm Số Và Điều Kiện Kết Thúc

Điểm số:

- Lấy theo độ cao Y cao nhất đạt được trong run.

Thời gian:

- Bộ đếm lùi trong suốt gameplay.
- Dùng quiz để thay đổi bộ đếm.

Kết thúc run:

1. Timer = 0
2. Nhân vật rơi xuống ngưỡng fall-out

## 3. Tính Năng Trực Tuyến (Online Features - PlayFab)

### 3.1 Đăng Ký, Đăng Nhập, Đăng Xuất

Đăng ký:

- Validate tên, email, số điện thoại, mật khẩu.
- Tạo tài khoản PlayFab với Username + DisplayName.
- Lưu User Data profile cần thiết.

Đăng nhập:

- Login bằng email/password.
- Hỗ trợ fallback trong một số trường hợp account mapping.

Đăng xuất:

- Xóa credentials local và trả về auth gate.

Session replay:

- Có cơ chế giữ session khi reload scene để replay nhanh.

### 3.2 Leaderboard Thường

Tên statistic:

- LeaderBoard_Normal

Quy tắc submit:

1. Lấy highscore đã cache.
2. Chỉ submit khi điểm run mới lớn hơn highscore.
3. Nếu thấp hơn thì bỏ qua, tránh spam API.

Hiển thị:

- Top list và thông tin player rank trong leaderboard panel.

### 3.3 Live-Ops Event

Cấu hình sự kiện qua Title Data:

- EventEnabled
- EventStart hoặc EventStartUtc
- EventEnd hoặc EventEndUtc

Hành vi:

- Nếu event đang mở, score event được submit thêm.
- Nếu event đóng, score event không submit.
- UI Event tab phân tách với tab leaderboard thường.

### 3.4 CloudScript Event Submit

CloudScript hiện có:

- Xác thực cửa sổ thời gian event.
- Parse đa định dạng thời gian, xử lý cả cửa sổ qua ngày.
- Update statistic event theo namespace của event.
- Trả payload kết quả để client xử lý.

Cơ chế an toàn:

- Nếu CloudScript fail hoặc null result, client có fallback submit trực tiếp để hạn chế mất điểm.

### 3.5 Archiving (Thiết kế để mở rộng)

Mục tiêu:

- Chụp snapshot kết quả người thắng mỗi event sau khi event kết thúc.

Thiết kế khuyến nghị:

1. CloudScript cron đọc top N của statistic event.
2. Ghi kết quả vào Internal Data với key theo version event.
3. Tạo endpoint để admin/doc kết quả lịch sử.

Trạng thái hiện tại:

- Chưa có module archive đầy đủ trong code client.
- Đã có nền tảng event + title data để bổ sung nhanh.

## 4. Giao Diện Và Mỹ Thuật (Art & UI/UX)

### 4.1 Định Hướng Nghệ Thuật

Phong cách:

- Flat design + cartoon học đường.
- Tông màu cam/xanh theo nhận diện FPoly.
- Nhân vật ong (Bee mascot) và bộ icon minh họa ngành học.

Background và environment:

- Scene khuôn viên, tòa nhà, hành lang học đường.
- Layer background sáng, thân thiện, dễ đọc UI text.

### 4.2 Hệ Thống Màn Hình

Các panel chính:

1. Panel_Start
2. Panel_Auth (Login/Register)
3. Panel_HUD
4. Panel_Quiz
5. Panel_GameOver
6. Panel_Leaderboard

Nguyên tắc UI runtime:

- Không ghi đè layout custom đã đặt trong scene.
- Chỉ auto tạo bộ phận nếu object/component bị thiếu.

HUD:

- Hiện Score và Time real-time.

Quiz UI:

- Hiện câu hỏi và danh sách đáp án.
- Hỗ trợ câu 2 lựa chọn và 4 lựa chọn.
- Feedback màu + thông điệp lớn, delay ngắn.

### 4.3 Audio UX

Có BGM và SFX riêng.

Có nút toggle music/sfx trên menu.

Có sprite set on/off để hiểu trạng thái nhanh.

## 5. Hệ Thống Dữ Liệu (Data Structure)

### 5.1 Cấu Trúc Ngân Hàng Câu Hỏi

File:

- Assets/PolyJump/Resources/QuizData.json

Schema mỗi phần tử:

- q: string
- a: string array (2 hoặc 4 phương án)
- correct: int (index đúng)

Quy tắc hiện hành:

- Các câu 4 đáp án đã được cân bằng index đúng 0/1/2/3.
- Các câu 2 đáp án chỉ có thể là 0/1.
- Mỗi lần hiển thị quiz: không lặp câu hỏi cho đến khi dùng hết pool, sau đó mới reshuffle.

### 5.2 PlayFab Title Data

Keys chính:

- EventEnabled
- EventStart
- EventEnd
- EventStartUtc
- EventEndUtc
- EventRewards (có thể thêm)

### 5.3 Player Data Và Statistic

Player Data:

- UserName
- Email
- Phone

Thống nhất key:

- Dùng 1 key canonical UserName.
- Loại bỏ key Username cũ để tránh trùng lặp.

Statistics:

- LeaderBoard_Normal
- LeaderBoard_Event_* (event scoped)

## 6. Kiến Trúc Hệ Thống Và Luồng Runtime

### 6.1 Runtime Managers

GameManager:

- Điều phối state machine, timer, score, camera follow, game over.

PlayerController:

- Xử lý input PC/mobile, movement smoothing, trigger platform.

LevelSpawner:

- Sinh nền liên tục và cleanup.

QuizManager:

- Load bank câu hỏi, render UI, chấm điểm đúng/sai theo time delta, no-repeat cycle.

PlayFabAuthManager:

- Auth flow, profile data, submit score, event check, cloudscript/fallback.

LeaderboardUiManager:

- UI top rank, refresh cooldown, tab thường/sự kiện.

AudioManager:

- Quản lý BGM/SFX, bind toggle buttons, sprite state.

### 6.2 Performance Và Runtime Guards

FPS:

- Cố định 60 FPS toàn game.
- Tắt vSync để tránh drift frame cap.

Guards quan trọng:

1. Check login trước gọi leaderboard/event API.
2. Guard state khi đang Quiz/GameOver để tránh input sai.
3. Guard coroutine feedback quiz để tránh double submit.
4. Guard session preserve one-shot khi replay.

Editor stability:

- Có workaround tự động tắt Unity Remote để tránh adb timeout khi không có device.

## 7. Cân Bằng Game (Balancing)

Mục tiêu cân bằng:

- Vòng chơi 2-5 phút để dễ replay.
- Quiz xuất hiện đủ để tạo học tập, không quá dày.

Thông số hiện tại:

- Start time: 180s
- Correct delta: +5s
- Wrong delta: -5s
- Feedback delay: 1s

Hướng tuning:

1. Nếu game quá dễ: giảm correct delta hoặc tăng quiz ratio.
2. Nếu game quá khó: tăng min gap cho platform để dễ canh tay.
3. Nếu mobile drift: điều chỉnh touch dead-zone và acceleration.

## 8. Lộ Trình Triển Khai (Timeline)

Giai đoạn 1: Cơ chế nhảy và vật lý

- Trạng thái: Đã xong cơ bản
- Đầu việc: movement, platform collision, camera follow, spawn system

Giai đoạn 2: PlayFab leaderboard + CloudScript

- Trạng thái: Đã xong core
- Đầu việc: auth, leaderboard thường, event submit cloudscript + fallback

Giai đoạn 3: Ngân hàng 100 câu + hệ thống quiz

- Trạng thái: Đã xong bản vận hành
- Đầu việc: quiz data, reward/penalty, no-repeat question cycle, balance index đúng

Giai đoạn 4: Web admin + test tổng hợp

- Trạng thái: Đang hoàn thiện
- Đầu việc: title data workflow, event operation guide, regression test mobile/UI

## 9. Rủi Ro Và Kế Hoạch Giảm Thiểu

Rủi ro 1: Event data sai format

- Giảm thiểu: parse đa format + log cảnh báo + fallback skip event submit

Rủi ro 2: CloudScript fail khi deploy/version

- Giảm thiểu: client fallback submit và thông báo rõ trong log

Rủi ro 3: UI bị snap ghi đè setup scene

- Giảm thiểu: chính sách missing-only setup runtime cho auth/start widgets

Rủi ro 4: Dữ liệu profile trùng key

- Giảm thiểu: canonical key UserName + remove legacy key

Rủi ro 5: Trùng câu hỏi gây nhàm chán

- Giảm thiểu: shuffle queue theo vòng, hết pool mới lặp

## 10. KPI Để Đánh Giá

KPI gameplay:

1. Average run time
2. Average score
3. Quiz accuracy rate
4. Replay rate

KPI online:

1. Login success rate
2. Leaderboard submit success rate
3. Event participation rate

KPI content:

1. Coverage câu hỏi theo nhóm ngành
2. Tỷ lệ thoát game tại quiz panel

## 11. Phụ Lục Vận Hành

Checklist mỗi release:

1. Kiểm tra QuizData.json parse OK.
2. Kiểm tra no-repeat quiz trong 1 vòng dữ liệu.
3. Kiểm tra submit score thường/sự kiện trên PlayFab.
4. Kiểm tra toggle audio và panel flow.
5. Kiểm tra FPS lock 60 trên target build.

Checklist live-ops:

1. Set EventEnabled.
2. Set EventStart/EventEnd.
3. Test cloudscript submit 1 account.
4. Theo dõi tab leaderboard event.
5. Đóng event và chạy quy trình archive.

---

Bản GDD này được viết dựa trên tính năng hiện có trong project và đã thêm phần mở rộng cần thiết để chuẩn hóa vận hành production.
