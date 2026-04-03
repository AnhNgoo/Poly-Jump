// Script CloudScript xử lý nhận điểm sự kiện từ client,
// kiểm tra khung giờ hợp lệ và cập nhật thống kê sự kiện trên server PlayFab.
handlers.SubmitEventScore = function (args, context) {
    // Khối 1: đọc cấu hình sự kiện từ Title Data để xác thực điều kiện mở sự kiện.
    var tdResp = server.GetTitleData({
        Keys: ["EventEnabled", "EventStartUtc", "EventEndUtc", "EventStart", "EventEnd"]
    });
    var titleData = (tdResp && tdResp.Data) ? tdResp.Data : {};

    // Hàm phụ: chuẩn hóa và đọc trạng thái bật/tắt sự kiện.
    function isEnabled(value) {
        if (value === null || value === undefined) return false;
        var normalized = ("" + value).toLowerCase().trim();
        return normalized === "true" || normalized === "1" || normalized === "yes";
    }

    // Hàm phụ: ưu tiên giá trị primary, fallback sang giá trị dự phòng nếu primary rỗng.
    function pickValue(primary, fallback) {
        if (primary !== null && primary !== undefined && ("" + primary).trim() !== "") {
            return ("" + primary).trim();
        }

        if (fallback !== null && fallback !== undefined && ("" + fallback).trim() !== "") {
            return ("" + fallback).trim();
        }

        return "";
    }

    // Hàm phụ: chuyển chuỗi thời gian sự kiện về UTC milliseconds.
    // Hỗ trợ các dạng: "HH:mm:ss-yyyy:MM:dd", "HH-mm-ss - yyyy-MM-dd", "yyyy:MM:dd-HH:mm:ss".
    function parseVnTimeToUtcMs(text) {
        if (!text || typeof text !== "string") return null;

        // Khối phụ: ghép năm-tháng-ngày-giờ VN và đổi sang mốc UTC.
        function toUtcMs(year, month, day, hour, minute, second) {
            if (hour < 0 || hour > 24) return null;
            if (minute < 0 || minute > 59 || second < 0 || second > 59) return null;
            if (hour === 24 && (minute !== 0 || second !== 0)) return null;

            var safeHour = hour === 24 ? 0 : hour;
            var localMs = Date.UTC(year, month - 1, day, safeHour, minute, second);
            if (hour === 24) {
                localMs += 24 * 60 * 60 * 1000;
            }

            return localMs - (7 * 60 * 60 * 1000);
        }

        var raw = text.trim();

        // Khối 2.1: parse dạng giờ-trước, ngày-sau.
        var m1 = raw.match(/^\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*$/);
        if (m1) {
            var h1 = parseInt(m1[1], 10);
            var min1 = parseInt(m1[2], 10);
            var s1 = parseInt(m1[3], 10);
            var y1 = parseInt(m1[4], 10);
            var mo1 = parseInt(m1[5], 10);
            var d1 = parseInt(m1[6], 10);

            if (isNaN(h1) || isNaN(min1) || isNaN(s1) || isNaN(y1) || isNaN(mo1) || isNaN(d1)) {
                return null;
            }

            return toUtcMs(y1, mo1, d1, h1, min1, s1);
        }

        // Khối 2.2: parse dạng ngày-trước, giờ-sau.
        var m2 = raw.match(/^\s*(\d{4})\D+(\d{1,2})\D+(\d{1,2})\s*-\s*(\d{1,2})\D+(\d{1,2})\D+(\d{1,2})\s*$/);
        if (m2) {
            var y2 = parseInt(m2[1], 10);
            var mo2 = parseInt(m2[2], 10);
            var d2 = parseInt(m2[3], 10);
            var h2 = parseInt(m2[4], 10);
            var min2 = parseInt(m2[5], 10);
            var s2 = parseInt(m2[6], 10);

            if (isNaN(h2) || isNaN(min2) || isNaN(s2) || isNaN(y2) || isNaN(mo2) || isNaN(d2)) {
                return null;
            }

            return toUtcMs(y2, mo2, d2, h2, min2, s2);
        }

        return null;
    }

    var startText = pickValue(titleData.EventStartUtc, titleData.EventStart);
    var endText = pickValue(titleData.EventEndUtc, titleData.EventEnd);
    var baseStatisticName = "LeaderBoard_Event";

    // Khối 3: xác định tên statistic sự kiện theo payload client hoặc dùng mặc định.
    var statisticName = (args && args.statisticName) ? ("" + args.statisticName).trim() : baseStatisticName;
    if (!statisticName) {
        statisticName = baseStatisticName;
    }

    // Khối 4: ép ghi vào statistic gắn suffix thời gian sự kiện để tách từng đợt.
    // Ví dụ: LeaderBoard_Event_11:30:00-2026:04:03
    if (statisticName === baseStatisticName && startText) {
        statisticName = baseStatisticName + "_" + startText;
    }

    // Khối 5: chặn cập nhật nếu sự kiện chưa mở từ cấu hình.
    if (!isEnabled(titleData.EventEnabled)) {
        return { success: false, message: "Sự kiện đang đóng" };
    }

    // Khối 6: parse thời gian và kiểm tra định dạng hợp lệ.
    var startMs = parseVnTimeToUtcMs(startText);
    var endMs = parseVnTimeToUtcMs(endText);

    if (startMs === null || endMs === null) {
        return {
            success: false,
            message: "EventStart/EventEnd sai định dạng, cần HH:mm:ss-yyyy:MM:dd"
        };
    }

    if (endMs < startMs) {
        // Hỗ trợ khung giờ qua đêm (ví dụ 22:00 -> 06:00 ngày hôm sau).
        endMs += 24 * 60 * 60 * 1000;

        if (endMs < startMs) {
            return { success: false, message: "Thời gian sự kiện không hợp lệ (End < Start)" };
        }
    }

    // Khối 7: chặn request nằm ngoài khung giờ sự kiện.
    var nowMs = Date.now();
    if (nowMs < startMs || nowMs > endMs) {
        return {
            success: false,
            message: "Ngoài thời gian sự kiện"
        };
    }

    // Khối 8: chuẩn hóa điểm đầu vào và kiểm tra hợp lệ.
    var score = parseInt(args && args.score, 10);
    if (isNaN(score) || score < 0) {
        return { success: false, message: "Điểm không hợp lệ" };
    }

    // Khối 9: xác định người chơi hiện tại từ context server.
    var playFabId = currentPlayerId;
    if (!playFabId) {
        return { success: false, message: "Không tìm thấy người chơi hiện tại" };
    }

    // Khối 10: lấy điểm cũ để trả về thông tin thay đổi cho client/admin.
    var previousScore = 0;
    try {
        var statsResp = server.GetPlayerStatistics({
            PlayFabId: playFabId,
            StatisticNames: [statisticName]
        });

        if (statsResp && statsResp.Statistics) {
            for (var i = 0; i < statsResp.Statistics.length; i++) {
                var s = statsResp.Statistics[i];
                if (s && s.StatisticName === statisticName) {
                    previousScore = parseInt(s.Value, 10) || 0;
                    break;
                }
            }
        }
    } catch (readErr) {
        // Không chặn luồng cập nhật điểm nếu đọc điểm cũ thất bại.
    }

    // Khối 11: ghi đè điểm hiện tại của người chơi lên statistic sự kiện.
    server.UpdatePlayerStatistics({
        PlayFabId: playFabId,
        Statistics: [
            {
                StatisticName: statisticName,
                Value: score
            }
        ]
    });

    // Khối 12: trả kết quả xử lý cho phía gọi CloudScript.
    return {
        success: true,
        updated: true,
        statisticName: statisticName,
        score: score,
        previousScore: previousScore,
        message: "Đã cập nhật điểm gần nhất của sự kiện"
    };
}