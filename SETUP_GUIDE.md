# Hướng Dẫn Setup Scene Game - PolyJump

Tài liệu này hướng dẫn chi tiết cách thiết lập GameObjects và UI để scene game chạy đúng với hệ thống State Pattern vừa tạo.

---

## 1. Cấu Trúc Hierarchy Scene

```
Scene: GameScene
│
├── Managers
│   ├── EventManager           (GameObject + EventManager)
│   ├── GameManager            (GameObject + GameManager)
│   ├── QuizManager            (GameObject + QuizManager)
│   ├── UIManager              (GameObject + UIManager)
│   ├── ScoreManager           (GameObject + ScoreManager)
│   ├── DataManager            (GameObject + DataManager)
│   └── PersistentData         (GameObject + PersistentData)
│
├── Background
│   └── Background             (Sprite Renderer)
│
├── Gameplay
│   ├── Player                 (Sprite Renderer + PlayerController + PlayerAnimHandler)
│   ├── PlatformSpawner        (GameObject + PlatformSpawner)
│   └── Camera                 (Main Camera + CameraFollow)
│
└── UI
   └── Canvas
      ├── MainMenu            (MainMenuUI)
      ├── SettingsMenu        (SettingMenuUI)
      ├── PauseMenu           (PauseMenuUI)
      ├── MapSelection        (MapSelectionUI)
      ├── MajorSelection      (MajorSelectionUI)
      ├── GameplayHUD         (GameplayHUDUI)
      ├── QuizPanel           (QuizUI)
      ├── GameOverPanel       (GameOverUI)
      └── TouchControls
         ├── LeftTouchArea   (UI Button)
         └── RightTouchArea  (UI Button)
```

---

## 2. Thiết Lập Chi Tiết Từng GameObject

### 2.1 Managers

#### EventManager

- **GameObject** mới, đặt tên `EventManager`
- Thêm component: `EventManager` (script có sẵn)
- Đây là singleton dùng chung toàn game

#### GameManager

- **GameObject** mới, đặt tên `GameManager`
- Thêm component: `GameManager` (script có sẵn)
- Kéo các reference cần thiết trong Inspector

#### QuizManager

- **GameObject** mới, đặt tên `QuizManager`
- Thêm component: `QuizManager` (script có sẵn)
- Kéo các reference cần thiết trong Inspector

#### UIManager

- **GameObject** mới, đặt tên `UIManager`
- Thêm component: `UIManager` (script có sẵn)
- Kéo reference `Canvas` vào field `Canvas`

#### ScoreManager

- **GameObject** mới, đặt tên `ScoreManager`
- Thêm component: `ScoreManager` (script có sẵn)

#### DataManager

- **GameObject** mới, đặt tên `DataManager`
- Thêm component: `DataManager` (script có sẵn)

#### PersistentData

- **GameObject** mới, đặt tên `PersistentData`
- Thêm component: `PersistentData` (script có sẵn)

---

### 2.2 Player

#### Tạo Player GameObject

1. Tạo **GameObject** mới, đặt tên `Player`
2. Đặt vị trí ban đầu: `(0, -2, 0)`
3. Gán tag **Player** (để `CameraFollow` tự tìm)

#### Thêm Collider

1. Thêm **BoxCollider2D**
   - Size: `(0.8, 0.8)`
   - Center: `(0, 0)`

2. Thêm **Rigidbody2D**
   - Body Type: **Dynamic**
   - Gravity Scale: `1`
   - Collision Detection: **Continuous**
   - Freeze Rotation Z: **Tích chọn**

#### Thêm Sprite Renderer

1. Thêm **SpriteRenderer**
   - Kéo sprite nhân vật vào **Sprite**
   - **Flip X**: Tùy chiều mặc định

#### Thêm Animator

1. Thêm **Animator**
   - Kéo **Controller**: Tạo mới hoặc kéo Animator Controller
   - **Apply Root Motion**: Bỏ tick

2. **TẠO ANIMATOR CONTROLLER** cho Player:
   - Right-click trong Project > Create > Animation Controller
   - Đặt tên: `PlayerAC`
   - Mở Animator Window
   - Tạo 2 State: `Fall` và `Jump`
   - Mỗi state kéo sprite phù hợp vào
   - **Entry** → Fall (mặc định)
   - Không cần tạo Trigger/Transition nếu dùng CrossFade
   - Đảm bảo state name đúng chính tả: `Fall`, `Jump`

#### Thêm PlayerController Script

1. Thêm **PlayerController** (script đã cập nhật)
2. **THÊM THÊM** component **PlayerAnimHandler** vào **cùng GameObject** với PlayerController
3. Kéo **Animator** vào field `Animator` của `PlayerAnimHandler`
4. Kéo **SpriteRenderer** vào field `Sprite Renderer` của `PlayerAnimHandler`
5. Trong `PlayerController` Inspector, kéo **chính GameObject này** vào field `Anim Handler`

| Field                  | Giá trị                  |
| ---------------------- | ------------------------ |
| **Move Speed**         | `5`                      |
| **Screen Left Bound**  | `-4.5`                   |
| **Screen Right Bound** | `4.5`                    |
| **Anim Handler**       | Kéo chính GameObject này |

> **LƯU Ý QUAN TRỌNG:** `PlayerAnimHandler` là **component riêng biệt**. Bạn phải thêm nó vào GameObject của Player, sau đó gán Animator + SpriteRenderer vào component đó, rồi kéo chính GameObject đó vào `PlayerController.Anim Handler`.

---

### 2.3 PlatformSpawner

#### Tạo Platform Prefab

1. Tạo **GameObject**, đặt tên `Platform_Prefab`
2. Thêm **BoxCollider2D** (Size phù hợp, ví dụ `3 x 0.3`)
3. Thêm **SpriteRenderer** với sprite nền platform
4. Thêm tag: `Platform`
5. **Kéo vào Project** để tạo Prefab
6. Xóa khỏi Scene

> `PlatformComponent` sẽ được `PlatformSpawner` tự add vào platform khi spawn (không bắt buộc add thủ công).

#### Tạo PlatformSpawner

1. Tạo **GameObject** mới, đặt tên `PlatformSpawner`
2. Thêm script `PlatformSpawner`
3. Kéo **Platform Prefab** vào field `Platform Prefab`
4. Cấu hình các thông số sinh platform

---

### 2.4 Camera

#### Main Camera

- Chọn **Main Camera** có sẵn
- Background: Màu phù hợp
- Projection: **Orthographic**
- Size: `5` (hoặc tùy chỉnh)
- Có thể thêm **Cinemachine** để camera theo player mượt hơn

#### CameraFollow

1. Thêm component `CameraFollow` vào **Main Camera**
2. Đảm bảo Player có tag **Player** để `CameraFollow` tự bắt target

---

## 3. Thiết Lập UI (Canvas)

### 3.1 Canvas Chính

1. Right-click trong Hierarchy > UI > Canvas
2. **Render Mode**: `Screen Space - Overlay`
3. **Canvas Scaler**:
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1080 x 1920`
   - Match: `0.5`

### 3.2 MainMenu (MainMenuUI)

Right-click trên Canvas > Create Empty, đặt tên `MainMenu`

#### ButtonPanel

1. Tạo child `ButtonPanel`
2. Tạo 3 Button con:
   - `PlayButton`
   - `SettingsButton`
   - `QuitButton`

> `MainMenuUI` sẽ tự tìm các button theo path `ButtonPanel/...`

### 3.3 MapSelection (MapSelectionUI)

Right-click trên Canvas > Create Empty, đặt tên `MapSelection`

#### ButtonPanel

1. Tạo child `ButtonPanel`
2. Tạo child `FacultyButtons` (container để spawn)
3. Tạo `BackButton`

#### Faculty Button Prefab

1. Tạo prefab `FacultyButton` (Button + Text/TMP là child)
2. Kéo prefab này vào field `Faculty Button Prefab` của `MapSelectionUI`

> Đây là màn hình chọn **ngành/khoa**. Khi chọn sẽ chuyển qua MajorSelection để chọn **chuyên ngành**.

> `MapSelectionUI` đọc dữ liệu từ [Assets/StreamingAssets/FacultyData.json](Assets/StreamingAssets/FacultyData.json) và tự spawn button theo danh sách faculties.

### 3.4 MajorSelection (MajorSelectionUI)

Right-click trên Canvas > Create Empty, đặt tên `MajorSelection`

#### TitleText

1. Tạo `TitleText` (TextMeshProUGUI) để hiển thị tên ngành/khoa hiện tại

#### ButtonPanel

1. Tạo child `ButtonPanel`
2. Tạo child `MajorButtons` (đây là nơi spawn button chuyên ngành)
3. Tạo `BackButton` (nên đặt trong `ButtonPanel` nhưng ngoài `MajorButtons`)

#### Major Button Prefab

1. Tạo 1 prefab `MajorButton` (Button + Text/TMP)
2. Text/TMP nên là **child** của Button (ví dụ `Label`)
3. Kéo prefab này vào field `Major Button Prefab` của `MajorSelectionUI`

#### Cấu hình danh sách chuyên ngành bằng JSON

Tạo file [Assets/StreamingAssets/FacultyData.json](Assets/StreamingAssets/FacultyData.json) với cấu trúc sau:

```
{
   "faculties": [
      {
         "id": "IT",
         "displayName": "Cong nghe thong tin",
         "majors": [
            { "id": "GameProgramming", "displayName": "Game Programming" },
            { "id": "WebProgramming", "displayName": "Web Programming" }
         ]
      },
      {
         "id": "Marketing",
         "displayName": "Marketing",
         "majors": [
            { "id": "DigitalMarketing", "displayName": "Digital Marketing" }
         ]
      }
   ]
}
```

> `MajorSelectionUI` đọc dữ liệu từ JSON, tạo button prefab tương ứng theo ngành/khoa đã chọn, và cập nhật TitleText.

> Lưu ý: `majorId` trong [Assets/StreamingAssets/QuestionsData.json](Assets/StreamingAssets/QuestionsData.json) phải khớp với `id` trong `FacultyData.json` để câu hỏi đúng chuyên ngành.

### 3.5 GameplayHUD (GameplayHUDUI)

Right-click trên Canvas > Create Empty, đặt tên `GameplayHUD`

#### HUDCanvas

1. Tạo child `HUDCanvas`
2. Tạo 2 TextMeshProUGUI con:
   - `JumpScoreText`
   - `KnowledgeScoreText`
3. Tạo 1 Button con:
   - `PauseButton`

### 3.6 QuizPanel (QuizUI)

Right-click trên Canvas > Create Empty, đặt tên `QuizPanel`
**Ban đầu SetActive = false**

#### ContentPanel

1. Tạo child `ContentPanel`
2. Tạo các con sau:
   - `QuestionText` (TextMeshProUGUI)
   - `CountdownPanel` (GameObject) để bật/tắt timer
   - `CountdownText` (TextMeshProUGUI) nằm trong `CountdownPanel`

> Khi mở câu hỏi, `CountdownPanel` sẽ hiển thị timer 30 giây. Hết thời gian sẽ tự đóng quiz và quay lại game.

#### Answer Buttons (Prefab)

1. Tạo child `AnswerButtons` trong `ContentPanel` (container để spawn)
2. Tạo prefab `AnswerButton` (Button + TextMeshProUGUI làm child)
3. Gán prefab vào field `Answer Button Prefab` của `QuizUI`

> `QuizUI` sẽ tự spawn số lượng button theo số đáp án (2, 4, ...).

### 3.7 GameOverPanel (GameOverUI)

Right-click trên Canvas > Create Empty, đặt tên `GameOverPanel`
**Ban đầu SetActive = false**

#### ContentPanel

1. Tạo child `ContentPanel`
2. Tạo các con:
   - `JumpScoreText` (TextMeshProUGUI)
   - `KnowledgeScoreText` (TextMeshProUGUI)
   - `PercentageText` (TextMeshProUGUI)
   - `RestartButton` (Button)
   - `MainMenuButton` (Button)

### 3.8 SettingsMenu (SettingMenuUI)

Right-click trên Canvas > Create Empty, đặt tên `SettingsMenu`
**Ban đầu SetActive = false**

#### ContentPanel

1. Tạo child `ContentPanel`
2. Tạo các con:
   - `MusicSlider` (Slider)
   - `VFXSlider` (Slider)
   - `BackButton` (Button)

> `SettingMenuUI` dùng 2 slider để chỉnh music/vfx (logic sẽ thêm sau).

### 3.9 PauseMenu (PauseMenuUI)

Right-click trên Canvas > Create Empty, đặt tên `PauseMenu`
**Ban đầu SetActive = false**

#### ButtonPanel

1. Tạo child `ButtonPanel`
2. Tạo các con:
   - `MainMenuButton` (Button)
   - `ResumeButton` (Button)
   - `SettingsButton` (Button)

### 3.10 TouchControls - Điều Khiển Cảm Ứng

Right-click trên Canvas > Create Empty, đặt tên `TouchControls`

#### LeftTouchArea

1. Right-click > TouchControls > UI > Button (Image)
2. Đặt góc dưới bên trái
3. Size: `200 x 300`
4. Alpha: `0` (trong suốt)
5. Thêm script xử lý touch hoặc EventTrigger:
   - `OnPointerDown` → `PlayerController.OnPointerDownLeft()`
   - `OnPointerUp` → `PlayerController.OnPointerUpLeft()`

#### RightTouchArea

1. Tương tự LeftTouchArea
2. Đặt góc dưới bên phải
3. Events:
   - `OnPointerDown` → `PlayerController.OnPointerDownRight()`
   - `OnPointerUp` → `PlayerController.OnPointerUpRight()`

---

## 4. Kết Nối References

Sau khi tạo xong tất cả GameObjects, cần kết nối references:

### UIManager

```
Canvas                → Canvas (GameObject)
```

### GameManager

```
Player Prefab         → Player prefab (PlayerController)
Spawn Point           → Transform (điểm spawn)
Gameplay Container    → (tuỳ chọn) GameObject
```

### PlayerController

```
Anim Handler          → PlayerAnimHandler (component)
Left Touch Area       → LeftTouchArea (RectTransform)
Right Touch Area      → RightTouchArea (RectTransform)
```

### PlayerAnimHandler

```
Animator             → Player Animator
Sprite Renderer      → Player SpriteRenderer
```

### PlatformSpawner

```
Platform Prefab       → Platform_Prefab
```

### CameraFollow

```
Target                → Player (hoặc để trống để auto tìm theo tag Player)
```

### QuizManager

```
Quiz UI               → QuizPanel (QuizUI)
```

---

## 5. Thiết Lập Physics (Project Settings)

Vào **Edit > Project Settings > Physics 2D**:

| Setting          | Giá trị khuyến nghị     |
| ---------------- | ----------------------- |
| Gravity Y        | `-30`                   |
| Default Material | Tạo Physics Material 2D |

### Tạo Physics Material 2D cho Player

1. Right-click trong Project > Create > Physics Material 2D
2. Đặt tên: `PlayerMaterial`
3. Friction: `0`
4. Bounciness: `0`
5. Kéo vào **Rigidbody2D** của Player

---

## 6. Thiết Lập Tags

### Platform Tag

1. Chọn Platform Prefab
2. Inspector > Tag > Platform
3. Nếu chưa có, tạo mới Tag "Platform"

---

## 7. Thiết Lập Layer (Tùy Chọn)

Nếu cần, tạo Layer mới:

1. **Edit > Project Settings > Tags and Layers**
2. Thêm Layer: `Platform`, `Player`

---

## 8. Kiểm Tra Game Chạy

1. Nhấn **Play** (Ctrl+P)
2. Player rơi xuống → thấy animation **Fall**
3. Player nhảy lên → thấy animation **Jump**
4. Di chuyển trái/phải → sprite lật hướng
5. Chạm platform → `PlatformPassed` event được gọi
6. Kiểm tra UI hiển thị đúng

---

## 9. Troubleshooting Thường Gặp

### Player không nhảy lên

- Kiểm tra Rigidbody2D có Gravity Scale > 0
- Kiểm tra Platform có Collider2D
- Kiểm tra Collision detection mode

### Animation không chạy

- Kiểm tra Animator Controller đã gán
- Kiểm tra Trigger parameter có đúng tên (Fall, Jump)
- Kiểm tra Transition không có Exit Time

### Sprite không lật

- Kiểm tra `PlayerAnimHandler` có kéo SpriteRenderer vào
- Kiểm tra `_animHandler.SetFacingDirection()` được gọi

### Sự kiện touch không hoạt động

- Kiểm tra Canvas có Raycast Target trên Button
- Kiểm tra EventSystem có trong Scene
- Thêm `EventTrigger` component nếu OnClick không hoạt động

---

## 10. File Script Đã Tạo

```
Assets/_Data/Scripts/Player/
├── PlayerController.cs           (Đã cập nhật)
├── Animation/
│   └── PlayerAnimHandler.cs      (Mới)
└── States/
    ├── IPlayerState.cs           (Mới)
    ├── PlayerStateMachine.cs    (Mới)
    └── StateBehaviors/
        ├── FallState.cs         (Mới)
        └── JumpState.cs         (Mới)
```

---

Chúc bạn setup thành công! Nếu gặp vấn đề gì, hãy cho tôi biết.
