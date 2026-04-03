# Tài Liệu Chi Tiết Toàn Bộ Script PolyJump

Tài liệu này mô tả chi tiết toàn bộ script trong thư mục PolyJump, bao gồm vai trò từng script, từng lớp và từng hàm. Nội dung được viết để đọc độc lập khi review kiến trúc, onboarding thành viên mới hoặc bảo vệ đồ án.

## Phạm vi tài liệu

- Toàn bộ script C# trong thư mục Scripts và Editor.
- CloudScript JavaScript trong thư mục CloudScript.
- Mô tả mức class và mức method theo hướng dễ hiểu, bám sát chức năng thực tế.

---

## Script: SubmitEventScore.js

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\CloudScript\SubmitEventScore.js
- Loại script: CloudScript (JavaScript) chạy phía PlayFab server.

### Tổng quan chức năng

- Handler chính nhận điểm sự kiện từ client, kiểm tra trạng thái bật/tắt sự kiện, kiểm tra khung giờ, xác thực dữ liệu điểm, sau đó ghi thống kê bằng API server-side.
- Luồng xử lý có cơ chế tách tên statistic theo suffix thời gian sự kiện để phân biệt từng đợt thi, đồng thời trả lại previousScore phục vụ audit/log phía client.

### Danh sách hàm và tác dụng chi tiết

#### handlers.SubmitEventScore

- Mục đích chính: Điểm vào trung tâm cho submit điểm sự kiện từ client, bảo vệ tính hợp lệ thời gian và dữ liệu trước khi ghi server.
- Thời điểm/cách gọi: Được client gọi thông qua ExecuteCloudScript với FunctionName tương ứng.
- Tác động hệ thống: Cập nhật LeaderBoard_Event theo phiên sự kiện, ảnh hưởng trực tiếp bảng xếp hạng sự kiện.
- Ghi chú triển khai: Đã có comment tiếng Việt theo từng khối lớn trong handler.

#### isEnabled

- Mục đích chính: Chuẩn hóa giá trị boolean từ Title Data dưới nhiều định dạng (true/1/yes).
- Thời điểm/cách gọi: Được gọi nội bộ trong handler khi xác thực EventEnabled.
- Tác động hệ thống: Quyết định cho phép hoặc chặn toàn bộ quá trình submit điểm sự kiện.

#### pickValue

- Mục đích chính: Chọn giá trị ưu tiên và fallback cho dữ liệu thời gian sự kiện.
- Thời điểm/cách gọi: Dùng khi đọc EventStartUtc/EventStart và EventEndUtc/EventEnd.
- Tác động hệ thống: Giúp hệ thống tương thích cả key mới và key cũ trong Title Data.

#### parseVnTimeToUtcMs và toUtcMs

- Mục đích chính: Parse chuỗi thời gian định dạng VN sang mốc UTC milliseconds để so sánh thời gian chính xác trên server.
- Thời điểm/cách gọi: Được gọi khi xác thực EventStart/EventEnd trước khi cập nhật điểm.
- Tác động hệ thống: Nếu parse lỗi hoặc ngoài khung giờ, request sẽ bị từ chối để đảm bảo tính công bằng sự kiện.

---

## Script: PolyJumpAudioSetupExtension.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Editor\PolyJumpAudioSetupExtension.cs
- Số lớp tìm thấy: 1
- Số hàm tìm thấy: 10

### Tổng quan chức năng

- **PolyJumpAudioSetupExtension** (dòng 12): Công cụ Editor cấu hình AudioManager theo kiểu idempotent: gọi lại nhiều lần vẫn an toàn, chỉ thêm/gán phần đang thiếu.

### Danh sách hàm và tác dụng chi tiết

#### SetupAudioManager (dòng 22, quyền truy cập: public)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildPrototypeWithAudio (dòng 77, quyền truy cập: public)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureAudioSources (dòng 87, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindSecondaryAudioSource (dòng 128, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### AssignDefaultClipsIfMissing (dòng 148, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureToggleSpriteSet (dòng 180, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureFolderPath (dòng 206, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryAssignClipIfMissing (dòng 240, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ConfigureMusicDefaults (dòng 274, quyền truy cập: private)

- Mục đích chính: Thiết lập tham số vận hành kỹ thuật cho component.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ConfigureSfxDefaults (dòng 291, quyền truy cập: private)

- Mục đích chính: Thiết lập tham số vận hành kỹ thuật cho component.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: PolyJumpPrototypeBuilder.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Editor\PolyJumpPrototypeBuilder.cs
- Số lớp tìm thấy: 1
- Số hàm tìm thấy: 38

### Tổng quan chức năng

- **PolyJumpPrototypeBuilder** (dòng 17): Công cụ Editor tự động dựng prototype scene, prefab, panel UI và các tham chiếu manager theo chuẩn của dự án PolyJump.

### Danh sách hàm và tác dụng chi tiết

#### BuildStage1Prototype (dòng 72, quyền truy cập: public)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureFolders (dòng 93, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureTag (dòng 107, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateOrUpdatePlayerPrefab (dòng 138, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateOrUpdatePlatformPrefab (dòng 181, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SaveTempAsPrefab (dòng 214, quyền truy cập: private)

- Mục đích chính: Lưu dữ liệu cấu hình hoặc trạng thái ra nơi lưu trữ.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsurePlayerPrefabEnhancements (dòng 224, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsurePlayerAnimatorController (dòng 313, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureScaleAnimationClip (dòng 351, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindOrCreateState (dòng 380, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateDefaultPlayerBounceParticle (dòng 405, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetSquareSprite (dòng 442, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetBuiltinFont (dòng 458, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetupScene (dòng 474, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureStartPanelAudioToggles (dòng 794, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateOrUpdateSideBoundaries (dòng 822, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateOrUpdateBoundary (dòng 844, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RemoveExistingScenePlayer (dòng 876, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PrepareExistingScenePlayer (dòng 890, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateOrGetPlayerSpawnPoint (dòng 906, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateOrUpdateStartGround (dòng 923, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetupMainCamera (dòng 998, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureDirectionalLight (dòng 1029, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureEventSystem (dòng 1055, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateOrGetCanvas (dòng 1071, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildUi (dòng 1107, quyền truy cập: private)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildLeaderboardPanel (dòng 1200, quyền truy cập: private)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildAuthUi (dòng 1542, quyền truy cập: private)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateAuthPanel (dòng 1607, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateTMPLabel (dòng 1644, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateTMPInputField (dòng 1684, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureTmpInputTextObjects (dòng 1731, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateTMPButton (dòng 1801, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreatePanel (dòng 1874, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateText (dòng 1910, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateButton (dòng 1953, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindOrCreate (dòng 2041, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindChildByName (dòng 2057, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: UnityRemoteAutoDisable.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Editor\UnityRemoteAutoDisable.cs
- Số lớp tìm thấy: 1
- Số hàm tìm thấy: 1

### Tổng quan chức năng

- **UnityRemoteAutoDisable** (dòng 11): Tiện ích Editor chạy khi khởi động để tắt Unity Remote một lần, tránh lỗi ADB timeout khi không có thiết bị Android.

### Danh sách hàm và tác dụng chi tiết

#### DisableUnityRemoteOnce (dòng 19, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: AudioManager.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Scripts\AudioManager.cs
- Số lớp tìm thấy: 4
- Số hàm tìm thấy: 62

### Tổng quan chức năng

- **AudioManager** (dòng 14): Lớp điều phối âm thanh tổng thể. Nó phát nhạc nền, SFX theo ngữ cảnh, quản lý nút bật/tắt âm thanh và các relay để bắt sự kiện từ UI/gameplay.
- **AudioButtonClickRelay** (dòng 1168): Lớp relay bắt sự kiện click của Button bất kỳ để phát âm thanh click tập trung qua AudioManager.
- **AudioStartToggleRelay** (dòng 1244): Lớp relay cũ cho toggle âm thanh ở menu, duy trì khả năng bật/tắt và làm mới hiển thị trạng thái ON/OFF.
- **AudioPlayerJumpRelay** (dòng 1389): Lớp relay phát âm thanh nhảy khi người chơi chạm platform, có kiểm soát điều kiện rơi/xuống và chống lặp âm quá nhanh.

### Danh sách hàm và tác dụng chi tiết

#### Awake (dòng 77, quyền truy cập: private)

- Mục đích chính: Khởi tạo tham chiếu thành phần, chuẩn hóa trạng thái nội bộ ban đầu và bảo đảm điều kiện chạy đúng trước các callback khác.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnEnable (dòng 100, quyền truy cập: private)

- Mục đích chính: Đăng ký sự kiện hoặc kích hoạt liên kết runtime khi đối tượng được bật để bắt đầu nhận callback.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Start (dòng 109, quyền truy cập: private)

- Mục đích chính: Thực hiện thiết lập sau Awake, nối các phụ thuộc runtime và chuẩn bị dữ liệu để gameplay/UI hoạt động ổn định.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Update (dòng 119, quyền truy cập: private)

- Mục đích chính: Chạy mỗi frame để xử lý logic thời gian thực, đồng bộ trạng thái theo biến động input, dữ liệu và điều kiện chơi.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnDisable (dòng 148, quyền truy cập: private)

- Mục đích chính: Gỡ sự kiện và làm sạch liên kết tạm khi đối tượng bị tắt, tránh listener trùng lặp hoặc rò rỉ tham chiếu.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnDestroy (dòng 159, quyền truy cập: private)

- Mục đích chính: Thu dọn trạng thái tĩnh/liên kết còn tồn tại trước khi đối tượng bị hủy hoàn toàn.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PlayBackgroundMusic (dòng 174, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PlayButtonClick (dòng 204, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PlayCorrectAnswer (dòng 213, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PlayWrongAnswer (dòng 222, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PlayGameOver (dòng 231, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PlayJump (dòng 240, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ToggleMusic (dòng 249, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ToggleSfx (dòng 258, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetMusicEnabled (dòng 267, quyền truy cập: public)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetSfxEnabled (dòng 316, quyền truy cập: public)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PlaySfx (dòng 334, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnSceneLoaded (dòng 348, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RefreshAllHooks (dòng 359, quyền truy cập: private)

- Mục đích chính: Làm mới view hoặc cache nhằm đồng bộ trạng thái sau thay đổi.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BindAudioToggleButtons (dòng 384, quyền truy cập: private)

- Mục đích chính: Gắn liên kết giữa thành phần và callback tương ứng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### UnbindAudioToggleButtons (dòng 433, quyền truy cập: private)

- Mục đích chính: Gỡ liên kết event/callback đã gắn trước đó.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RemoveLegacyToggleRelay (dòng 451, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnMusicTogglePressed (dòng 470, quyền truy cập: private)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnSfxTogglePressed (dòng 479, quyền truy cập: private)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RefreshToggleButtonsVisual (dòng 488, quyền truy cập: private)

- Mục đích chính: Làm mới view hoặc cache nhằm đồng bộ trạng thái sau thay đổi.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ApplyToggleButtonVisual (dòng 498, quyền truy cập: private)

- Mục đích chính: Áp dụng cấu hình/trạng thái đã tính lên đối tượng đích.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveToggleSprite (dòng 527, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetSpriteFromSet (dòng 573, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetFallbackToggleSprite (dòng 593, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateFallbackToggleSprite (dòng 642, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### DrawRect (dòng 715, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### DrawCircle (dòng 740, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### DrawThickLine (dòng 773, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindButtonByName (dòng 818, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### HookAllButtons (dòng 843, quyền truy cập: private)

- Mục đích chính: Cài hook tự động để theo dõi sự kiện hoặc callback.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BindQuizButtons (dòng 871, quyền truy cập: private)

- Mục đích chính: Gắn liên kết giữa thành phần và callback tương ứng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### UnbindQuizButtons (dòng 913, quyền truy cập: private)

- Mục đích chính: Gỡ liên kết event/callback đã gắn trước đó.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PlayQuizAnswerSfxDeferred (dòng 932, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ContainsIgnoreCase (dòng 961, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsurePlayerJumpHooks (dòng 975, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveGameOverPanel (dòng 1003, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PollGameOverPanel (dòng 1033, quyền truy cập: private)

- Mục đích chính: Kiểm tra định kỳ trạng thái để phản ứng kịp thời.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureAudioSources (dòng 1060, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ConfigureMusicSourceDefaults (dòng 1115, quyền truy cập: private)

- Mục đích chính: Thiết lập tham số vận hành kỹ thuật cho component.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ConfigureSfxSourceDefaults (dòng 1132, quyền truy cập: private)

- Mục đích chính: Thiết lập tham số vận hành kỹ thuật cho component.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ApplyAudioState (dòng 1149, quyền truy cập: private)

- Mục đích chính: Áp dụng cấu hình/trạng thái đã tính lên đối tượng đích.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Awake (dòng 1176, quyền truy cập: private)

- Mục đích chính: Khởi tạo tham chiếu thành phần, chuẩn hóa trạng thái nội bộ ban đầu và bảo đảm điều kiện chạy đúng trước các callback khác.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnEnable (dòng 1185, quyền truy cập: private)

- Mục đích chính: Đăng ký sự kiện hoặc kích hoạt liên kết runtime khi đối tượng được bật để bắt đầu nhận callback.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnDisable (dòng 1206, quyền truy cập: private)

- Mục đích chính: Gỡ sự kiện và làm sạch liên kết tạm khi đối tượng bị tắt, tránh listener trùng lặp hoặc rò rỉ tham chiếu.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetManager (dòng 1218, quyền truy cập: public)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnButtonClicked (dòng 1227, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Awake (dòng 1259, quyền truy cập: private)

- Mục đích chính: Khởi tạo tham chiếu thành phần, chuẩn hóa trạng thái nội bộ ban đầu và bảo đảm điều kiện chạy đúng trước các callback khác.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnEnable (dòng 1270, quyền truy cập: private)

- Mục đích chính: Đăng ký sự kiện hoặc kích hoạt liên kết runtime khi đối tượng được bật để bắt đầu nhận callback.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnDisable (dòng 1291, quyền truy cập: private)

- Mục đích chính: Gỡ sự kiện và làm sạch liên kết tạm khi đối tượng bị tắt, tránh listener trùng lặp hoặc rò rỉ tham chiếu.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Update (dòng 1303, quyền truy cập: private)

- Mục đích chính: Chạy mỗi frame để xử lý logic thời gian thực, đồng bộ trạng thái theo biến động input, dữ liệu và điều kiện chơi.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnTogglePressed (dòng 1318, quyền truy cập: private)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RefreshVisual (dòng 1344, quyền truy cập: private)

- Mục đích chính: Làm mới view hoặc cache nhằm đồng bộ trạng thái sau thay đổi.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Awake (dòng 1403, quyền truy cập: private)

- Mục đích chính: Khởi tạo tham chiếu thành phần, chuẩn hóa trạng thái nội bộ ban đầu và bảo đảm điều kiện chạy đúng trước các callback khác.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FixedUpdate (dòng 1421, quyền truy cập: private)

- Mục đích chính: Chạy theo nhịp vật lý cố định để cập nhật chuyển động/va chạm có tính ổn định cao, tránh lệ thuộc frame rate.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetManager (dòng 1439, quyền truy cập: public)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnTriggerEnter2D (dòng 1448, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### IsDescendingOrAlmostStill (dòng 1490, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: AudioToggleSpriteSet.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Scripts\AudioToggleSpriteSet.cs
- Số lớp tìm thấy: 1
- Số hàm tìm thấy: 0

### Tổng quan chức năng

- **AudioToggleSpriteSet** (dòng 9): ScriptableObject lưu bộ sprite ON/OFF cho Music và SFX, giúp UI toggle âm thanh hiển thị nhất quán với trạng thái thực tế.

### Danh sách hàm và tác dụng chi tiết

- Không phát hiện hàm theo pattern chuẩn.

---

## Script: GameManager.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Scripts\GameManager.cs
- Số lớp tìm thấy: 1
- Số hàm tìm thấy: 31

### Tổng quan chức năng

- **GameManager** (dòng 20): Lớp điều phối gameplay trung tâm. Nó chịu trách nhiệm vòng đời trận chơi, trạng thái Menu/Playing/Quiz/GameOver, đếm thời gian, tính điểm hiện tại và đồng bộ UI chính.

### Danh sách hàm và tác dụng chi tiết

#### ApplyGlobalFrameRateOnBoot (dòng 29, quyền truy cập: private)

- Mục đích chính: Áp dụng cấu hình/trạng thái đã tính lên đối tượng đích.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResetStatics (dòng 39, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Awake (dòng 98, quyền truy cập: private)

- Mục đích chính: Khởi tạo tham chiếu thành phần, chuẩn hóa trạng thái nội bộ ban đầu và bảo đảm điều kiện chạy đúng trước các callback khác.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnApplicationFocus (dòng 122, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ApplyFrameRateSettings (dòng 134, quyền truy cập: private)

- Mục đích chính: Áp dụng cấu hình/trạng thái đã tính lên đối tượng đích.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Start (dòng 144, quyền truy cập: private)

- Mục đích chính: Thực hiện thiết lập sau Awake, nối các phụ thuộc runtime và chuẩn bị dữ liệu để gameplay/UI hoạt động ổn định.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Update (dòng 195, quyền truy cập: private)

- Mục đích chính: Chạy mỗi frame để xử lý logic thời gian thực, đồng bộ trạng thái theo biến động input, dữ liệu và điều kiện chơi.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### WireButtons (dòng 235, quyền truy cập: private)

- Mục đích chính: Nối các event/listener giữa UI và logic xử lý.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnPlayPressed (dòng 255, quyền truy cập: public)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnReplayPressed (dòng 269, quyền truy cập: public)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### StartGameplay (dòng 280, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RequestQuiz (dòng 327, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RegisterPendingQuizPlatform (dòng 361, quyền truy cập: public)

- Mục đích chính: Đăng ký dữ liệu người dùng hoặc tham gia một luồng nghiệp vụ mới.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveQuiz (dòng 381, quyền truy cập: public)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ConvertPendingQuizPlatformToNormal (dòng 431, quyền truy cập: private)

- Mục đích chính: Chuyển đổi dữ liệu/đối tượng từ dạng này sang dạng khác.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### AddTime (dòng 456, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SubtractTime (dòng 466, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SpawnOrActivatePlayer (dòng 476, quyền truy cập: private)

- Mục đích chính: Sinh đối tượng gameplay mới trong scene.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetPlayerSpawnPosition (dòng 510, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TrackBestHeight (dòng 524, quyền truy cập: private)

- Mục đích chính: Theo dõi biến động dữ liệu qua thời gian để phục vụ logic.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### UpdateCameraFollow (dòng 538, quyền truy cập: private)

- Mục đích chính: Cập nhật dữ liệu/hiển thị để phản ánh trạng thái mới nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CheckFallOut (dòng 571, quyền truy cập: private)

- Mục đích chính: Kiểm tra điều kiện và kích hoạt xử lý tương ứng nếu đạt ngưỡng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GameOver (dòng 590, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RefreshUI (dòng 656, quyền truy cập: private)

- Mục đích chính: Làm mới view hoặc cache nhằm đồng bộ trạng thái sau thay đổi.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RefreshHudText (dòng 694, quyền truy cập: private)

- Mục đích chính: Làm mới view hoặc cache nhằm đồng bộ trạng thái sau thay đổi.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FormatTime (dòng 712, quyền truy cập: private)

- Mục đích chính: Định dạng dữ liệu hiển thị để người dùng dễ đọc.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureUiReferences (dòng 724, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetState (dòng 841, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnDestroy (dòng 850, quyền truy cập: private)

- Mục đích chính: Thu dọn trạng thái tĩnh/liên kết còn tồn tại trước khi đối tượng bị hủy hoàn toàn.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryStartFromPlayButtonInput (dòng 862, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### IsPointerOnPlayButton (dòng 907, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: LeaderboardUiManager.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Scripts\LeaderboardUiManager.cs
- Số lớp tìm thấy: 2
- Số hàm tìm thấy: 68

### Tổng quan chức năng

- **LeaderboardUiManager** (dòng 15): Lớp dựng và vận hành UI bảng xếp hạng. Nó tải top người chơi, quản lý tab thường/sự kiện, đồng bộ trạng thái đăng nhập và cache để tối ưu gọi API.
- **LeaderboardCache** (dòng 36): Mô hình cache cho từng tab leaderboard, lưu top entries, trạng thái fetch, dữ liệu người chơi hiện tại và timestamp làm mới.

### Danh sách hàm và tác dụng chi tiết

#### Clear (dòng 51, quyền truy cập: public)

- Mục đích chính: Xóa dữ liệu tạm hoặc reset bộ nhớ đệm.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Bootstrap (dòng 136, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Awake (dòng 159, quyền truy cập: private)

- Mục đích chính: Khởi tạo tham chiếu thành phần, chuẩn hóa trạng thái nội bộ ban đầu và bảo đảm điều kiện chạy đúng trước các callback khác.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnEnable (dòng 178, quyền truy cập: private)

- Mục đích chính: Đăng ký sự kiện hoặc kích hoạt liên kết runtime khi đối tượng được bật để bắt đầu nhận callback.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Update (dòng 191, quyền truy cập: private)

- Mục đích chính: Chạy mỗi frame để xử lý logic thời gian thực, đồng bộ trạng thái theo biến động input, dữ liệu và điều kiện chơi.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveRuntimeReferences (dòng 245, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildOrBindUi (dòng 291, quyền truy cập: private)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildOrBindOpenLeaderboardButton (dòng 312, quyền truy cập: private)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildOrBindLeaderboardPanel (dòng 368, quyền truy cập: private)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureLeaderboardChildren (dòng 408, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### WireEvents (dòng 593, quyền truy cập: private)

- Mục đích chính: Nối các event/listener giữa UI và logic xử lý.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnOpenLeaderboardPressed (dòng 634, quyền truy cập: private)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnBackPressed (dòng 681, quyền truy cập: private)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnTabNormalPressed (dòng 718, quyền truy cập: private)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnTabRacePressed (dòng 736, quyền truy cập: private)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnRefreshPressed (dòng 754, quyền truy cập: private)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetActiveTab (dòng 780, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### UpdateEventInfoVisibility (dòng 810, quyền truy cập: private)

- Mục đích chính: Cập nhật dữ liệu/hiển thị để phản ánh trạng thái mới nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnLoginDetected (dòng 831, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnLogoutDetected (dòng 847, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ShouldAutoRefresh (dòng 869, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FetchNormalLeaderboard (dòng 895, quyền truy cập: private)

- Mục đích chính: Gọi tải dữ liệu từ backend/API hoặc nguồn dữ liệu ngoài.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FetchCurrentPlayerPosition (dòng 965, quyền truy cập: private)

- Mục đích chính: Gọi tải dữ liệu từ backend/API hoặc nguồn dữ liệu ngoài.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FinalizeNormalCacheWithoutRank (dòng 1030, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RefreshCurrentTabUi (dòng 1049, quyền truy cập: private)

- Mục đích chính: Làm mới view hoặc cache nhằm đồng bộ trạng thái sau thay đổi.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FetchEventLeaderboard (dòng 1071, quyền truy cập: private)

- Mục đích chính: Gọi tải dữ liệu từ backend/API hoặc nguồn dữ liệu ngoài.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FetchEventPlayerPosition (dòng 1180, quyền truy cập: private)

- Mục đích chính: Gọi tải dữ liệu từ backend/API hoặc nguồn dữ liệu ngoài.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FinalizeEventCacheWithoutRank (dòng 1245, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RefreshTabUi (dòng 1264, quyền truy cập: private)

- Mục đích chính: Làm mới view hoặc cache nhằm đồng bộ trạng thái sau thay đổi.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetCurrentPlayerInfo (dòng 1303, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateLeaderboardRow (dòng 1327, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ClearRows (dòng 1361, quyền truy cập: private)

- Mục đích chính: Xóa dữ liệu tạm hoặc reset bộ nhớ đệm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetRowText (dòng 1387, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateDefaultRowTemplate (dòng 1413, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### UpdateEmptyText (dòng 1444, quyền truy cập: private)

- Mục đích chính: Cập nhật dữ liệu/hiển thị để phản ánh trạng thái mới nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### UpdateRefreshButtonVisual (dòng 1468, quyền truy cập: private)

- Mục đích chính: Cập nhật dữ liệu/hiển thị để phản ánh trạng thái mới nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SyncAuthVisibility (dòng 1502, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnDestroy (dòng 1517, quyền truy cập: private)

- Mục đích chính: Thu dọn trạng thái tĩnh/liên kết còn tồn tại trước khi đối tượng bị hủy hoàn toàn.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveNormalStatisticName (dòng 1529, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveEventStatisticName (dòng 1543, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetEventStatisticSuffix (dòng 1574, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryNormalizeEventSuffixFromRaw (dòng 1594, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryParseEventSuffixParts (dòng 1645, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryRefreshEventInfo (dòng 1711, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnEventConfigUpdated (dòng 1809, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FlushPendingEventInfoCallbacks (dòng 1849, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### IsEventOpenNow (dòng 1875, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetEventWindowState (dòng 1884, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildEventUnavailableMessage (dòng 1929, quyền truy cập: private)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetEventStartDisplayText (dòng 1945, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ApplyEventInfoToUi (dòng 1965, quyền truy cập: private)

- Mục đích chính: Áp dụng cấu hình/trạng thái đã tính lên đối tượng đích.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetTitleDataValue (dòng 1991, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ParseTitleDataBool (dòng 2005, quyền truy cập: private)

- Mục đích chính: Phân tích chuỗi/dữ liệu thô thành định dạng có cấu trúc để xử lý tiếp.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveEventTimeFromTitleData (dòng 2026, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ParseEventUtcTitleData (dòng 2064, quyền truy cập: private)

- Mục đích chính: Phân tích chuỗi/dữ liệu thô thành định dạng có cấu trúc để xử lý tiếp.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryParseVnEventTextToUtc (dòng 2106, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryBuildUtcFromParts (dòng 2160, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FormatEventTimeForDisplay (dòng 2225, quyền truy cập: private)

- Mục đích chính: Định dạng dữ liệu hiển thị để người dùng dễ đọc.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetButtonText (dòng 2235, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveCurrentUserName (dòng 2254, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### NormalizeDisplayName (dòng 2273, quyền truy cập: private)

- Mục đích chính: Chuẩn hóa dữ liệu đầu vào về định dạng thống nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindOrCreateChild (dòng 2299, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureButton (dòng 2327, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateButton (dòng 2344, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureLabel (dòng 2391, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureColumnText (dòng 2432, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindOrCreateChild (dòng 2494, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetUiFont (dòng 2524, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: LevelSpawner.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Scripts\LevelSpawner.cs
- Số lớp tìm thấy: 1
- Số hàm tìm thấy: 15

### Tổng quan chức năng

- **LevelSpawner** (dòng 9): Lớp sinh địa hình theo trục dọc. Nó tạo platform liên tục phía trên người chơi, xóa platform cũ ngoài màn hình, và quản lý ground khởi điểm.

### Danh sách hàm và tác dụng chi tiết

#### Start (dòng 49, quyền truy cập: private)

- Mục đích chính: Thực hiện thiết lập sau Awake, nối các phụ thuộc runtime và chuẩn bị dữ liệu để gameplay/UI hoạt động ổn định.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Update (dòng 76, quyền truy cập: private)

- Mục đích chính: Chạy mỗi frame để xử lý logic thời gian thực, đồng bộ trạng thái theo biến động input, dữ liệu và điều kiện chơi.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetPaused (dòng 91, quyền truy cập: public)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResetLevelAroundPlayer (dòng 100, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SpawnAhead (dòng 154, quyền truy cập: private)

- Mục đích chính: Sinh đối tượng gameplay mới trong scene.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SpawnSinglePlatform (dòng 173, quyền truy cập: private)

- Mục đích chính: Sinh đối tượng gameplay mới trong scene.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SpawnGuaranteedFirstPlatform (dòng 204, quyền truy cập: private)

- Mục đích chính: Sinh đối tượng gameplay mới trong scene.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SpawnStartGround (dòng 224, quyền truy cập: private)

- Mục đích chính: Sinh đối tượng gameplay mới trong scene.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveSceneGround (dòng 247, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureGroundTagIfMissing (dòng 277, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ConfigureGroundForBounce (dòng 295, quyền truy cập: private)

- Mục đích chính: Thiết lập tham số vận hành kỹ thuật cho component.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetSpawnX (dòng 315, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CleanupOldPlatforms (dòng 342, quyền truy cập: private)

- Mục đích chính: Thu dọn đối tượng không còn cần thiết để tối ưu tài nguyên.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetScreenBottomY (dòng 389, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### IsBelowScreen (dòng 409, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: PlayerController.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Scripts\PlayerController.cs
- Số lớp tìm thấy: 1
- Số hàm tìm thấy: 17

### Tổng quan chức năng

- **PlayerController** (dòng 10): Lớp điều khiển nhân vật. Nó đọc input bàn phím/chạm, áp vận tốc ngang, xử lý nảy khi chạm platform, và điều khiển hoạt ảnh/particle khi bật nhảy.

### Danh sách hàm và tác dụng chi tiết

#### Awake (dòng 66, quyền truy cập: private)

- Mục đích chính: Khởi tạo tham chiếu thành phần, chuẩn hóa trạng thái nội bộ ban đầu và bảo đảm điều kiện chạy đúng trước các callback khác.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Start (dòng 90, quyền truy cập: private)

- Mục đích chính: Thực hiện thiết lập sau Awake, nối các phụ thuộc runtime và chuẩn bị dữ liệu để gameplay/UI hoạt động ổn định.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Update (dòng 102, quyền truy cập: private)

- Mục đích chính: Chạy mỗi frame để xử lý logic thời gian thực, đồng bộ trạng thái theo biến động input, dữ liệu và điều kiện chơi.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FixedUpdate (dòng 112, quyền truy cập: private)

- Mục đích chính: Chạy theo nhịp vật lý cố định để cập nhật chuyển động/va chạm có tính ổn định cao, tránh lệ thuộc frame rate.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CaptureInput (dòng 137, quyền truy cập: private)

- Mục đích chính: Thu thập dữ liệu từ ngữ cảnh runtime hoặc input.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ReadTouchDragInput (dòng 183, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnTriggerEnter2D (dòng 259, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetInputEnabled (dòng 300, quyền truy cập: public)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetGameplayPaused (dòng 317, quyền truy cập: public)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResetForNewRun (dòng 351, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ForceNormalAnimation (dòng 376, quyền truy cập: public)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PlayBounceEffects (dòng 400, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EmitBounceParticle (dòng 412, quyền truy cập: private)

- Mục đích chính: Phát tín hiệu hoặc hiệu ứng phục vụ phản hồi gameplay/UI.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureBounceParticle (dòng 434, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### UpdateAnimationState (dòng 505, quyền truy cập: private)

- Mục đích chính: Cập nhật dữ liệu/hiển thị để phản ánh trạng thái mới nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CrossFadeTo (dòng 532, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RefreshAnimatorStateCache (dòng 547, quyền truy cập: private)

- Mục đích chính: Làm mới view hoặc cache nhằm đồng bộ trạng thái sau thay đổi.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: PlayFabAuthManager.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Scripts\PlayFabAuthManager.cs
- Số lớp tìm thấy: 1
- Số hàm tìm thấy: 96

### Tổng quan chức năng

- **PlayFabAuthManager** (dòng 19): Lớp tích hợp PlayFab cho xác thực và dữ liệu người chơi. Nó xử lý đăng ký/đăng nhập/đăng xuất, cập nhật hồ sơ, gửi điểm leaderboard thường và event.

### Danh sách hàm và tác dụng chi tiết

#### ResetRuntimeSession (dòng 98, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### PreserveSessionForNextSceneLoad (dòng 112, quyền truy cập: public)

- Mục đích chính: Giữ lại trạng thái/phiên để tái sử dụng qua lần tải scene.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ConsumePreserveSessionFlag (dòng 125, quyền truy cập: private)

- Mục đích chính: Đọc và dùng một cờ/trạng thái theo cơ chế dùng một lần.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnEnable (dòng 136, quyền truy cập: private)

- Mục đích chính: Đăng ký sự kiện hoặc kích hoạt liên kết runtime khi đối tượng được bật để bắt đầu nhận callback.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Awake (dòng 156, quyền truy cập: private)

- Mục đích chính: Khởi tạo tham chiếu thành phần, chuẩn hóa trạng thái nội bộ ban đầu và bảo đảm điều kiện chạy đúng trước các callback khác.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### Start (dòng 180, quyền truy cập: private)

- Mục đích chính: Thực hiện thiết lập sau Awake, nối các phụ thuộc runtime và chuẩn bị dữ liệu để gameplay/UI hoạt động ổn định.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### LateUpdate (dòng 229, quyền truy cập: private)

- Mục đích chính: Xử lý hậu kỳ sau Update để tinh chỉnh hiển thị hoặc đồng bộ theo kết quả đã tính ở bước trước.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnValidate (dòng 243, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RegisterUser (dòng 263, quyền truy cập: public)

- Mục đích chính: Đăng ký dữ liệu người dùng hoặc tham gia một luồng nghiệp vụ mới.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### LoginUser (dòng 326, quyền truy cập: public)

- Mục đích chính: Xử lý quy trình xác thực đăng nhập.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetCachedLeaderboardHighscore (dòng 371, quyền truy cập: public)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureLeaderboardHighscoreCached (dòng 380, quyền truy cập: public)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SubmitScore (dòng 443, quyền truy cập: public)

- Mục đích chính: Gửi dữ liệu hoặc yêu cầu lên backend/service.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SubmitScore (dòng 452, quyền truy cập: public)

- Mục đích chính: Gửi dữ liệu hoặc yêu cầu lên backend/service.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SubmitEventScoreWhenEventOpen (dòng 490, quyền truy cập: private)

- Mục đích chính: Gửi dữ liệu hoặc yêu cầu lên backend/service.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureEventWindowStatus (dòng 515, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SubmitEventScoreViaCloudScript (dòng 603, quyền truy cập: private)

- Mục đích chính: Gửi dữ liệu hoặc yêu cầu lên backend/service.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SubmitEventScoreDirect (dòng 659, quyền truy cập: private)

- Mục đích chính: Gửi dữ liệu hoặc yêu cầu lên backend/service.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveEventStatisticNameForWindow (dòng 675, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetEventStatisticSuffix (dòng 695, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryNormalizeEventSuffixFromRaw (dòng 722, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryParseEventSuffixParts (dòng 773, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ShouldFallbackEventSubmit (dòng 839, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildCloudScriptErrorDetails (dòng 864, quyền truy cập: private)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryReadCloudScriptBoolean (dòng 919, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryReadCloudScriptString (dòng 999, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### IsEventWindowOpenNow (dòng 1030, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetTitleDataValue (dòng 1075, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ParseTitleDataBool (dòng 1089, quyền truy cập: private)

- Mục đích chính: Phân tích chuỗi/dữ liệu thô thành định dạng có cấu trúc để xử lý tiếp.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveEventTimeFromTitleData (dòng 1110, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ParseEventUtcTitleData (dòng 1148, quyền truy cập: private)

- Mục đích chính: Phân tích chuỗi/dữ liệu thô thành định dạng có cấu trúc để xử lý tiếp.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryParseVnEventTextToUtc (dòng 1190, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryBuildUtcFromParts (dòng 1236, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FlushPendingEventWindowCallbacks (dòng 1301, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### InvalidateEventWindowCache (dòng 1327, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SubmitStatisticValue (dòng 1343, quyền truy cập: private)

- Mục đích chính: Gửi dữ liệu hoặc yêu cầu lên backend/service.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SubmitStatisticValue (dòng 1352, quyền truy cập: private)

- Mục đích chính: Gửi dữ liệu hoặc yêu cầu lên backend/service.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnRegisterButtonPressed (dòng 1399, quyền truy cập: public)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnLoginButtonPressed (dòng 1412, quyền truy cập: public)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SaveExtraInfo (dòng 1423, quyền truy cập: private)

- Mục đích chính: Lưu dữ liệu cấu hình hoặc trạng thái ra nơi lưu trữ.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ValidateRegisterInput (dòng 1474, quyền truy cập: private)

- Mục đích chính: Xác thực tính hợp lệ dữ liệu đầu vào theo quy tắc.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ValidateLoginInput (dòng 1524, quyền truy cập: private)

- Mục đích chính: Xác thực tính hợp lệ dữ liệu đầu vào theo quy tắc.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### HandleRegisterError (dòng 1553, quyền truy cập: private)

- Mục đích chính: Xử lý trường hợp hoặc lỗi cụ thể theo nhánh nghiệp vụ.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### HandleLoginError (dòng 1589, quyền truy cập: private)

- Mục đích chính: Xử lý trường hợp hoặc lỗi cụ thể theo nhánh nghiệp vụ.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnError (dòng 1620, quyền truy cập: private)

- Mục đích chính: Hàm callback theo vòng đời/sự kiện hệ thống, tự động được Unity hoặc luồng đăng ký sự kiện gọi đến.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### MarkAuthenticatedAndOpenStartMenu (dòng 1635, quyền truy cập: private)

- Mục đích chính: Đánh dấu trạng thái để dùng cho các bước xử lý tiếp theo.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### MarkAuthenticatedAndOpenStartMenu (dòng 1648, quyền truy cập: private)

- Mục đích chính: Đánh dấu trạng thái để dùng cho các bước xử lý tiếp theo.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### NormalizeUserNamePlayerData (dòng 1661, quyền truy cập: private)

- Mục đích chính: Chuẩn hóa dữ liệu đầu vào về định dạng thống nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnLogoutPressed (dòng 1691, quyền truy cập: public)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ExtractLeaderboardHighscore (dòng 1712, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### UpdateLeaderboardHighscoreCache (dòng 1742, quyền truy cập: private)

- Mục đích chính: Cập nhật dữ liệu/hiển thị để phản ánh trạng thái mới nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### InvalidateLeaderboardHighscoreCache (dòng 1752, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FlushPendingHighscoreCallbacks (dòng 1764, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SaveAuthContextForReplay (dòng 1789, quyền truy cập: private)

- Mục đích chính: Lưu dữ liệu cấu hình hoặc trạng thái ra nơi lưu trữ.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CaptureCurrentStaticAuthContext (dòng 1811, quyền truy cập: private)

- Mục đích chính: Thu thập dữ liệu từ ngữ cảnh runtime hoặc input.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ConsumeReplayPreserveFlag (dòng 1832, quyền truy cập: private)

- Mục đích chính: Đọc và dùng một cờ/trạng thái theo cơ chế dùng một lần.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TryRestoreAuthContextFromReplayCache (dòng 1866, quyền truy cập: private)

- Mục đích chính: Thử xử lý một tác vụ có thể thất bại và trả kết quả kiểm tra thành công.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ClearReplaySessionCache (dòng 1901, quyền truy cập: private)

- Mục đích chính: Xóa dữ liệu tạm hoặc reset bộ nhớ đệm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến gameplay, điểm số, thời gian hoặc phản hồi âm thanh/hiệu ứng của người chơi.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SaveStringOrDelete (dòng 1918, quyền truy cập: private)

- Mục đích chính: Lưu dữ liệu cấu hình hoặc trạng thái ra nơi lưu trữ.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RefreshAuthGateUi (dòng 1933, quyền truy cập: private)

- Mục đích chính: Làm mới view hoặc cache nhằm đồng bộ trạng thái sau thay đổi.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ApplyEditorDefaultAuthState (dòng 1978, quyền truy cập: private)

- Mục đích chính: Áp dụng cấu hình/trạng thái đã tính lên đối tượng đích.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureTitleIdConfigured (dòng 2002, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### NormalizeEmail (dòng 2029, quyền truy cập: private)

- Mục đích chính: Chuẩn hóa dữ liệu đầu vào về định dạng thống nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### TrimInput (dòng 2040, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BuildUsernameFromName (dòng 2049, quyền truy cập: private)

- Mục đích chính: Xây dựng cấu trúc hoàn chỉnh từ nhiều thành phần con theo một chuẩn cài đặt.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### IsValidGmail (dòng 2104, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### IsValidPhoneNumber (dòng 2113, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetStatus (dòng 2122, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ClearStatusAfterDelay (dòng 2153, quyền truy cập: private)

- Mục đích chính: Xóa dữ liệu tạm hoặc reset bộ nhớ đệm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### AutoBindUiReferences (dòng 2169, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### IsLeaderboardPanelVisible (dòng 2273, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### WireUiEvents (dòng 2287, quyền truy cập: private)

- Mục đích chính: Nối các event/listener giữa UI và logic xử lý.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnOpenRegisterPressed (dòng 2328, quyền truy cập: public)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnBackToLoginPressed (dòng 2337, quyền truy cập: public)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ShowLoginTab (dòng 2346, quyền truy cập: private)

- Mục đích chính: Hiển thị panel/thành phần hoặc nội dung cần người dùng thấy.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ShowRegisterTab (dòng 2366, quyền truy cập: private)

- Mục đích chính: Hiển thị panel/thành phần hoặc nội dung cần người dùng thấy.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureCenteredAuthLayout (dòng 2386, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsurePanelRectDefaultsWhenMissing (dòng 2397, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureTabButtons (dòng 2423, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateTabButton (dòng 2459, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### LocalizeUiTexts (dòng 2497, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureUiInteractionSetup (dòng 2527, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureStartMenuWidgets (dòng 2542, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### UpdateStartMenuWidgetLayout (dòng 2615, quyền truy cập: private)

- Mục đích chính: Cập nhật dữ liệu/hiển thị để phản ánh trạng thái mới nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureLogoutButtonLabel (dòng 2653, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateStartUserText (dòng 2691, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### CreateStartLogoutButton (dòng 2716, quyền truy cập: private)

- Mục đích chính: Tạo mới thực thể/đối tượng UI hoặc dữ liệu khi chưa tồn tại.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động trực tiếp đến dữ liệu backend/phiên đăng nhập hoặc kết quả đồng bộ client-server.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### LoadAndShowCurrentUserName (dòng 2755, quyền truy cập: private)

- Mục đích chính: Nạp dữ liệu/tài nguyên vào bộ nhớ để sử dụng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetStartUserText (dòng 2793, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### EnsureInputEditable (dòng 2808, quyền truy cập: private)

- Mục đích chính: Đảm bảo tiền điều kiện hoặc tham chiếu cần thiết đã sẵn sàng trước khi dùng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetTmpTextByName (dòng 2823, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetInputPlaceholder (dòng 2837, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetButtonLabel (dòng 2856, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindTmpInput (dòng 2883, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindTmpText (dòng 2893, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### FindButton (dòng 2903, quyền truy cập: private)

- Mục đích chính: Tìm kiếm đối tượng/thành phần theo tên, path hoặc điều kiện.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: QuizManager.cs

- Đường dẫn: D:\ProjectUnity\GameAI\Assets\PolyJump\Scripts\QuizManager.cs
- Số lớp tìm thấy: 3
- Số hàm tìm thấy: 14

### Tổng quan chức năng

- **QuizQuestion** (dòng 12): Mô hình dữ liệu một câu hỏi quiz gồm nội dung câu hỏi, danh sách đáp án và chỉ số đáp án đúng.
- **QuizQuestionCollection** (dòng 23): Mô hình bọc danh sách câu hỏi quiz để parse JsonUtility từ dữ liệu JSON mảng.
- **QuizManager** (dòng 31): Lớp quản lý quiz trong trận. Nó tải ngân hàng câu hỏi từ Resources, chọn câu hỏi không lặp trong chu kỳ, hiển thị đáp án và trả thưởng/phạt thời gian.

### Danh sách hàm và tác dụng chi tiết

#### Awake (dòng 69, quyền truy cập: private)

- Mục đích chính: Khởi tạo tham chiếu thành phần, chuẩn hóa trạng thái nội bộ ban đầu và bảo đảm điều kiện chạy đúng trước các callback khác.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### NormalizeFeedbackVietnameseText (dòng 89, quyền truy cập: private)

- Mục đích chính: Chuẩn hóa dữ liệu đầu vào về định dạng thống nhất.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### LoadQuestions (dòng 119, quyền truy cập: private)

- Mục đích chính: Nạp dữ liệu/tài nguyên vào bộ nhớ để sử dụng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### BindAnswerButtons (dòng 149, quyền truy cập: private)

- Mục đích chính: Gắn liên kết giữa thành phần và callback tương ứng.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ShowRandomQuestion (dòng 175, quyền truy cập: public)

- Mục đích chính: Hiển thị panel/thành phần hoặc nội dung cần người dùng thấy.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### HideQuizPanel (dòng 253, quyền truy cập: public)

- Mục đích chính: Ẩn panel/thành phần khỏi giao diện hiện thời.
- Thời điểm/cách gọi: Được script khác hoặc UI event trong scene gọi trực tiếp theo nhu cầu nghiệp vụ.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnAnswerButtonPressed (dòng 276, quyền truy cập: public)

- Mục đích chính: Hàm xử lý sự kiện bấm nút từ UI, đóng vai trò cầu nối giữa thao tác người dùng và nghiệp vụ hệ thống.
- Thời điểm/cách gọi: Được gọi khi người dùng thao tác nút UI đã được bind listener.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ResolveAfterDelay (dòng 308, quyền truy cập: private)

- Mục đích chính: Xác định giá trị, trạng thái hoặc đối tượng phù hợp từ nhiều nguồn đầu vào.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### ShowAnswerFeedback (dòng 326, quyền truy cập: private)

- Mục đích chính: Hiển thị panel/thành phần hoặc nội dung cần người dùng thấy.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RestoreQuestionTextAlignment (dòng 358, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### SetAnswersInteractable (dòng 372, quyền truy cập: private)

- Mục đích chính: Gán giá trị cấu hình hoặc trạng thái cụ thể cho hệ thống.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### GetNextQuestion (dòng 397, quyền truy cập: private)

- Mục đích chính: Truy xuất giá trị hiện có theo ngữ cảnh hàm.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### RebuildQuestionOrder (dòng 426, quyền truy cập: private)

- Mục đích chính: Thực hiện một tác vụ nghiệp vụ chuyên biệt trong luồng hoạt động của script.
- Thời điểm/cách gọi: Được gọi nội bộ trong script như một bước con của quy trình xử lý lớn hơn.
- Tác động hệ thống: Tác động rõ rệt lên giao diện, trạng thái panel, hoặc cấu trúc thành phần đang hiển thị.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

#### OnDisable (dòng 474, quyền truy cập: private)

- Mục đích chính: Gỡ sự kiện và làm sạch liên kết tạm khi đối tượng bị tắt, tránh listener trùng lặp hoặc rò rỉ tham chiếu.
- Thời điểm/cách gọi: Được Unity tự động gọi theo vòng đời MonoBehaviour.
- Tác động hệ thống: Tác động đến trạng thái nội bộ, làm nền cho các bước xử lý kế tiếp trong cùng luồng nghiệp vụ.
- Ghi chú triển khai: Hàm này đã được bổ sung comment trong code để mô tả khối xử lý chính, nhánh điều kiện và vòng lặp quan trọng (nếu có).

---

## Script: DemoMCP.cs

- Đường dẫn: Assets/DemoMCP.cs
- Số lớp tìm thấy: 1
- Số hàm tìm thấy: 2

### Tổng quan chức năng

- **DemoMCP**: Script mẫu MonoBehaviour rất cơ bản, chủ yếu dùng làm điểm khởi đầu để chèn logic thử nghiệm hoặc minh họa vòng đời component trong Unity.
- Script này hiện không chứa nghiệp vụ gameplay, nhưng có đầy đủ khung `Start` và `Update` để mở rộng nhanh khi cần.

### Danh sách hàm và tác dụng chi tiết

#### Start

- Mục đích chính: Khởi tạo dữ liệu ban đầu trước khi component đi vào vòng lặp runtime.
- Thời điểm/cách gọi: Được Unity tự động gọi một lần khi component active sau Awake.
- Tác động hệ thống: Hiện tại chưa tác động nghiệp vụ; được giữ làm điểm mở rộng logic khởi tạo sau này.

#### Update

- Mục đích chính: Nơi xử lý các tác vụ cần chạy theo từng frame.
- Thời điểm/cách gọi: Được Unity gọi liên tục mỗi frame khi component đang active.
- Tác động hệ thống: Hiện tại chưa tác động nghiệp vụ; được giữ làm điểm mở rộng logic realtime trong tương lai.

---

## Quan hệ giữa các script chính

- GameManager là trung tâm điều phối, gọi LevelSpawner, PlayerController, QuizManager, PlayFabAuthManager.
- PlayFabAuthManager phụ trách xác thực và gửi điểm, đồng thời là nguồn dữ liệu cho LeaderboardUiManager.
- LeaderboardUiManager vận hành UI bảng xếp hạng và gọi API PlayFab để đọc dữ liệu top/rank.
- AudioManager hoạt động xuyên suốt, liên kết qua các relay để bắt sự kiện từ gameplay và UI.
- CloudScript SubmitEventScore.js là lớp xác thực phía server cho luồng gửi điểm sự kiện.

## Ghi chú sử dụng tài liệu

- Tài liệu này đi cùng comment trong mã nguồn: comment trong code ưu tiên ngắn gọn, còn tài liệu này ưu tiên đầy đủ ngữ cảnh.
- Khi bổ sung hàm mới, nên cập nhật cả comment trong file script và mục tương ứng trong tài liệu này để giữ đồng bộ.
