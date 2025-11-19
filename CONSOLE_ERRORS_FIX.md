# Console 에러 해결 가이드

콘솔에 나타난 에러들을 해결하는 방법입니다.

---

## ✅ 정상 작동 확인

맵 생성은 성공했습니다:
```
✓ Tiles spawned
✓ Combined 5275 tiles into CombinedLandMap
✓ Combined 4725 tiles into CombinedWaterWalls
✓ Map generated with seed: 9950
```

---

## 🔴 심각한 에러 (즉시 수정 필요)

### 1. PlayerController NullReferenceException

**에러 메시지**:
```
NullReferenceException: Object reference not set to an instance of an object
PlayerController.HandleRotation () (at Assets/Scripts/Player/PlayerController.cs:146)
```

**원인**: Main Camera가 없거나 Tag가 "MainCamera"가 아님

**해결 방법**:

#### 방법 1: Main Camera 확인 (권장)

1. **Hierarchy에서 Main Camera 찾기**
   - Main Camera GameObject가 있는지 확인
   - 없다면: Hierarchy 우클릭 > Camera 생성

2. **Main Camera의 Tag 확인**
   ```
   Main Camera 선택 > Inspector 상단
   └─ Tag: MainCamera ← 반드시 "MainCamera"여야 함
   ```

3. **Tag가 "Untagged"라면**:
   - Tag 드롭다운 클릭
   - "MainCamera" 선택

#### 방법 2: TopDownCamera 사용

이미 TopDownCamera가 있다면:

1. **TopDownCamera GameObject 선택**
2. **Inspector 상단 Tag를 "MainCamera"로 변경**

#### 방법 3: PlayerController에 수동 할당

1. **Player GameObject 선택**
2. **Inspector > Player Controller (Script)**
   ```
   References
   ├─ Fire Point: [총구 Transform]
   └─ Main Camera: [Scene의 Camera 드래그] ← 여기에 수동으로 드래그
   ```

#### 확인

Play 모드 진입 후 마우스를 움직였을 때 플레이어가 회전하면 성공!

---

## 🟡 경고 (맵은 작동하지만 개선 필요)

### 2. Mesh Combining 에러

**에러 메시지**:
```
Cannot combine mesh that does not allow access: Primitive_Floor
```

**원인**: 프리팹의 Mesh가 Read/Write Enabled가 아님

**해결 방법**:

#### Primitive 대신 Custom Mesh 사용 (권장)

Unity의 기본 Primitive (Cube, Sphere 등)는 Read/Write가 불가능합니다.

**해결책 A: FBX Mesh 임포트**
1. Blender나 3D 소프트웨어에서 Plane/Cube 모델 제작
2. FBX로 Export
3. Unity에 Import
4. Import Settings:
   ```
   Model
   └─ Read/Write: ✓ 체크
   ```
5. Apply

**해결책 B: Unity Plane 사용**
1. Hierarchy에서 3D Object > Plane 생성
2. Plane의 Mesh는 기본적으로 Read/Write 가능
3. 프리팹으로 저장
4. Land/Water Tile Prefab으로 사용

**해결책 C: 에러 무시**
- Mesh Combining은 부분적으로 성공함
- 일부 타일만 합쳐지지 않고 나머지는 정상 작동
- 성능에 약간 영향 있지만 플레이 가능

#### 현재 상태 확인

Console에 이렇게 나왔다면 일부는 성공:
```
✓ Combined 5275 tiles into CombinedLandMap
✓ Combined 4725 tiles into CombinedWaterWalls
```

대부분 합쳐졌으므로 당장은 문제없습니다.

---

### 3. 한글 폰트 문제

**에러 메시지**:
```
The character with Unicode value \uC6B4 was not found in the [LiberationSans SDF]
```

**원인**: LiberationSans 폰트에 한글이 없음

**결과**: UI에 "운반 임무", "소탕 임무" 등이 □□□로 표시됨

**해결 방법**:

#### 방법 1: 한글 폰트 추가 (권장)

1. **한글 폰트 다운로드**
   - 무료: Noto Sans KR (Google Fonts)
   - 무료: Nanum Gothic
   - 다운로드: https://fonts.google.com/noto/specimen/Noto+Sans+KR

2. **Unity로 Import**
   - .ttf 파일을 Assets/Fonts 폴더로 드래그

3. **TextMeshPro Font Asset 생성**
   - Window > TextMeshPro > Font Asset Creator
   - Source Font File: [Noto Sans KR]
   - Character Set: Unicode Range (Hex)
   - Character Sequence:
     ```
     0x0020-0x007E  (영어/숫자/기호)
     0xAC00-0xD7A3  (한글 완성형 전체)
     ```
   - Sampling Point Size: Auto Sizing
   - Atlas Resolution: 4096x4096
   - Generate Font Atlas 클릭

4. **UI Text에 적용**
   - Mission Name Text (TMP) 선택
   - Font Asset: [NotoSansKR SDF]

#### 방법 2: 영어로 변경

한글이 필요없다면:

MissionBase.cs 수정:
```csharp
// 기존
missionName = "소탕 임무 1";

// 변경
missionName = "Elimination Mission 1";
```

---

## 🔵 마이너 경고 (무시 가능)

### 4. DontDestroyOnLoad 경고

**에러 메시지**:
```
DontDestroyOnLoad only works for root GameObjects or components on root GameObjects.
ProjectilePool:Awake ()
GameManager:Awake ()
```

**원인**: ProjectilePool이나 GameManager가 다른 GameObject의 자식으로 있음

**해결 방법**:

1. **Hierarchy에서 ProjectilePool 찾기**
2. **다른 GameObject의 자식이라면 밖으로 드래그**
3. **Root 레벨에 배치**

또는:

**무시** - 경고일 뿐이고 게임은 정상 작동합니다.

---

### 5. Mission Zone 경고

**에러 메시지**:
```
No mission zone prefabs assigned!
```

**원인**: Mission Zone Prefabs 리스트가 비어있음

**결과**: 미션 존이 배치되지 않지만 맵은 정상 생성

**해결 방법**:

1. **미션 존 프리팹 생성** (MAJOR_IMPROVEMENTS_SETUP_GUIDE.md 참고)
2. **MapGenerator Inspector**:
   ```
   Mission Zones
   └─ Mission Zone Prefabs
       ├─ Element 0: [EliminationZone]
       ├─ Element 1: [DeliveryZone]
       └─ Element 2: [InteractionZone]
   ```

또는:

**무시** - 미션 없이 맵만 생성하고 싶다면 그대로 두세요.

---

## 우선순위 정리

### 즉시 수정 (게임 플레이 불가)

1. ✅ **Main Camera Tag 설정** → PlayerController 작동
   - Main Camera의 Tag를 "MainCamera"로 설정

### 선택적 수정 (게임 플레이 가능하지만 개선 필요)

2. 🔹 **한글 폰트 추가** → UI 제대로 표시
   - Noto Sans KR 폰트 Import + TextMeshPro Font Asset 생성

3. 🔹 **Mesh Combining 개선** → 성능 최적화
   - Plane 사용 또는 FBX Mesh Import (Read/Write 켜기)

4. 🔹 **Mission Zone 프리팹 추가** → 미션 시스템 작동
   - 미션 존 프리팹 생성 및 MapGenerator에 할당

### 무시 가능

5. ⚪ DontDestroyOnLoad 경고 → 게임에 영향 없음

---

## 체크리스트

```
[ ] Main Camera의 Tag = "MainCamera"
[ ] Play 모드에서 마우스 움직임에 플레이어 회전
[ ] 한글 폰트 추가 (또는 영어로 변경)
[ ] Mission Zone Prefabs 추가 (선택)
```

---

## 해결 후 예상 Console

Main Camera 문제만 해결하면:

```
✓ Map generated with seed: 9950
✓ Total land tiles: 5275
✓ Game initialized

(경고만 있고 NullReferenceException 없음)
```

완벽하게 모두 해결하면:

```
✓ Map generated with seed: 9950
✓ Total land tiles: 5275
✓ Mission zones placed: 3
✓ Game initialized

(아무 경고/에러 없음)
```
