# 주요 변경 사항 Unity 세팅 가이드

이 가이드는 6가지 주요 개선 사항을 Unity 에디터에서 설정하는 방법을 설명합니다.

---

## 1. ProjectilePool.cs - 싱글톤 패턴

### 설정 방법
1. **Hierarchy에서 빈 GameObject 생성**
   - 우클릭 > Create Empty
   - 이름: `ProjectilePool`

2. **ProjectilePool 컴포넌트 추가**
   - ProjectilePool GameObject 선택
   - Inspector > Add Component > ProjectilePool

3. **Inspector 설정**
   ```
   Projectile Pool (Script)
   ├─ Projectile Prefab: [Bullet 프리팹 드래그]
   ├─ Initial Pool Size: 50
   └─ Max Pool Size: 200
   ```

4. **싱글톤이므로 Scene에 하나만 배치**
   - DontDestroyOnLoad로 자동 관리됨
   - 중복 생성시 자동으로 파괴됨

### 확인 사항
- ✅ Scene에 ProjectilePool GameObject가 딱 1개만 존재
- ✅ Projectile Prefab이 할당되어 있음

---

## 2. Gun.cs - 자동 할당

### 설정 방법
**별도 설정 불필요!** Gun 스크립트가 자동으로 ProjectilePool 싱글톤을 찾습니다.

### 기존 코드 (수동 할당 필요했음)
```csharp
[SerializeField] private ProjectilePool projectilePool; // Inspector에서 할당 필요
```

### 변경된 코드 (자동 할당)
```csharp
private void Start()
{
    if (projectilePool == null)
    {
        projectilePool = ProjectilePool.Instance; // 자동으로 찾음
    }
}
```

### 확인 사항
- ✅ Player와 Enemy의 Gun 컴포넌트에서 ProjectilePool 필드가 비어있어도 작동
- ✅ 기존에 수동으로 할당했다면 그대로 유지됨 (우선순위)

---

## 3. MapGenerator.cs - 완전 재작성 (440줄)

### 설정 방법
1. **Hierarchy에서 빈 GameObject 생성**
   - 이름: `MapGenerator`

2. **MapGenerator 컴포넌트 추가**

3. **Inspector 설정 (많음!)**

#### 3-1. Map Settings
```
Map Settings
├─ Map Width: 100
├─ Map Height: 100
├─ Tile Size: 1
└─ Random Seed: 12345 (원하는 시드값)
```

#### 3-2. Noise Settings (Multi-Layer Perlin Noise)
```
Noise Settings
├─ Base Scale: 20
├─ Base Threshold: 0.3
├─ Octaves: 3
├─ Persistence: 0.5
├─ Lacunarity: 2
├─ Hole Scale: 10
├─ Hole Threshold: 0.6
└─ Hole Intensity: 0.5
```

#### 3-3. Prefabs
```
Prefabs
├─ Land Tile Prefab: [육지 타일 프리팹 - Cube with Terrain tag]
└─ Water Tile Prefab: [물 타일 프리팹 - Cube with Wall tag]
```

**중요: 타일 프리팹 태그 설정**
- Land Tile Prefab → Tag: `Terrain`
- Water Tile Prefab → Tag: `Wall`

#### 3-4. Mission Zones (새로운 기능!)
```
Mission Zones
└─ Mission Zone Prefabs: [List]
    ├─ Element 0: [EliminationZone 프리팹]
    ├─ Element 1: [DeliveryZone 프리팹]
    └─ Element 2: [InteractionZone 프리팹]
```

**미션 존 프리팹 생성 방법**
1. Project 창에서 빈 프리팹 생성
2. 프리팹에 `MissionZoneInfo` 컴포넌트 추가
3. Inspector에서 크기 설정:
   ```
   Mission Zone Info (Script)
   └─ Size: X=15, Y=15 (타일 단위)
   ```
4. 미션 관련 오브젝트들 배치 (적, 아이템, 목표물 등)
5. MapGenerator의 Mission Zone Prefabs 리스트에 추가

#### 3-5. Materials
```
Materials
├─ Land Material: [녹색 Material]
└─ Water Material: [파란색 Material]
```

#### 3-6. Spawn Settings
```
Spawn Settings
└─ Player Spawn Offset: X=5, Y=0, Z=5
```

### 핵심 기능 이해

#### Mesh Combining (성능 최적화)
- **문제**: 100x100 맵 = 10,000개 타일 GameObject → 심각한 성능 저하
- **해결**: 모든 타일을 2개의 Combined Mesh로 합침
  - `CombinedLandMap` (모든 육지 타일)
  - `CombinedWaterWalls` (모든 물 타일 투명 벽)
- **결과**: Draw Call 99% 감소, FPS 대폭 향상

#### Water Transparent Walls (물 경계 충돌)
- 물 타일에 5m 높이 투명 BoxCollider 생성
- Renderer는 비활성화 (보이지 않음)
- 플레이어가 물에 빠지는 것을 방지

#### Mission Zone Placement (미션 존 배치)
- 프리팹 기반 미션 존 자동 배치
- `CarveLandForMissionZone()` - 노이즈 생성 시 미션 존 영역 확보
- 미션 존마다 NavMesh 자동 베이킹

### 확인 사항
- ✅ Mission Zone Prefabs에 최소 1개 이상의 프리팹 추가
- ✅ 각 프리팹에 MissionZoneInfo 컴포넌트 존재
- ✅ Land/Water Tile의 Tag 설정 완료

---

## 4. MissionZoneInfo.cs - 신규 생성

### 설정 방법
1. **미션 존 프리팹 열기**
   - Project 창에서 미션 존 프리팹 더블클릭

2. **MissionZoneInfo 컴포넌트 추가**
   - Inspector > Add Component > Mission Zone Info

3. **크기 설정**
   ```
   Mission Zone Info (Script)
   └─ Size: X=15, Y=15 (타일 단위)
   ```
   - 크기는 미션 존의 가로/세로 타일 개수
   - Elimination Zone: 15x15 권장
   - Delivery Zone: 20x20 권장
   - Interaction Zone: 10x10 권장

4. **Scene View에서 시각화 확인**
   - Gizmos 활성화 시 파란색 박스로 크기 표시됨

### 미션 존 프리팹 구조 예시

#### Elimination Zone Prefab
```
EliminationZone (GameObject)
├─ MissionZoneInfo (크기: 15x15)
├─ EliminationMission (Script)
├─ Enemies (빈 GameObject)
│   ├─ Enemy1
│   ├─ Enemy2
│   └─ Enemy3
└─ Cover Objects (엄폐물)
    ├─ Wall1
    └─ Wall2
```

#### Delivery Zone Prefab
```
DeliveryZone (GameObject)
├─ MissionZoneInfo (크기: 20x20)
├─ DeliveryMission (Script)
├─ PickupPoint
└─ DeliveryPoint
```

### 확인 사항
- ✅ 모든 미션 존 프리팹에 MissionZoneInfo 컴포넌트 추가
- ✅ Size 값이 실제 미션 존 크기와 일치
- ✅ Scene View에서 Gizmo로 크기 확인

---

## 5. UIManager.cs - Slider로 변경

### 설정 방법

#### 기존 방식 (Image.fillAmount)
```
Canvas
└─ StatsPanel
    └─ HealthBar (Image)
        └─ Fill (Image) ← fillAmount 사용
```

#### 새로운 방식 (Slider)
```
Canvas
└─ StatsPanel
    └─ HealthSlider (Slider)
```

### UI 재설정 단계

#### 1단계: 기존 UI 제거
- Canvas에서 기존 Image 기반 stat bar 삭제

#### 2단계: Slider 생성
1. **Canvas 우클릭 > UI > Slider**
2. **이름 변경**:
   - `HealthSlider`
   - `StaminaSlider`
   - `HungerSlider`
   - `ThirstSlider`

3. **Slider 위치 조정** (Bottom Left)
   ```
   위치 배치:
   HealthSlider   (왼쪽 하단)
   StaminaSlider  (HealthSlider 아래)
   HungerSlider   (StaminaSlider 아래)
   ThirstSlider   (HungerSlider 아래)
   ```

#### 3단계: Slider 설정
각 Slider 선택 > Inspector:
```
Slider (Script)
├─ Min Value: 0
├─ Max Value: 1
├─ Whole Numbers: ✗ (체크 해제)
└─ Value: 1
```

#### 4단계: Slider 외형 설정
**배경 색상**:
- Slider > Background (Image)
  - Color: 어두운 회색 (R:0.2, G:0.2, B:0.2)

**Fill 색상**:
- Slider > Fill Area > Fill (Image)
  - HealthSlider: 빨간색 (R:1, G:0, B:0)
  - StaminaSlider: 노란색 (R:1, G:1, B:0)
  - HungerSlider: 주황색 (R:1, G:0.6, B:0)
  - ThirstSlider: 파란색 (R:0, G:0.5, B:1)

**Handle 제거** (선택사항):
- Slider > Handle Slide Area 삭제
  - 게임에서는 Handle이 필요 없으므로 삭제 권장

#### 5단계: UIManager 연결
UIManager GameObject 선택 > Inspector:
```
UI Manager (Script)
Player Stats UI - Bottom Left
├─ Health Bar: [HealthSlider 드래그]
├─ Stamina Bar: [StaminaSlider 드래그]
├─ Hunger Bar: [HungerSlider 드래그]
└─ Thirst Bar: [ThirstSlider 드래그]
```

**차량 UI도 동일하게 설정**:
```
Vehicle Stats UI - Bottom Right
├─ Vehicle Health Bar: [VehicleHealthSlider]
└─ Vehicle Fuel Bar: [VehicleFuelSlider]
```

### 확인 사항
- ✅ 모든 Stat Bar가 Image가 아닌 Slider 컴포넌트
- ✅ Min Value = 0, Max Value = 1
- ✅ UIManager의 모든 필드에 Slider 할당 완료
- ✅ 실행 시 stat 변화에 따라 Slider가 움직이는지 확인

---

## 6. Vehicle.cs - Arcade Vehicle Physics 에셋 통합

### 설정 방법

#### 1단계: Arcade Vehicle Physics 에셋 임포트
1. **에셋 스토어에서 다운로드**
   - 에셋 이름: "Arcade Vehicle Physics" 또는 유사한 차량 물리 에셋
   - 무료/유료 차량 컨트롤러 에셋

2. **Package Manager에서 Import**
   - Window > Package Manager > My Assets
   - Arcade Vehicle Physics 찾기 > Import

#### 2단계: 차량 프리팹에 컨트롤러 추가

**차량 프리팹 구조**:
```
Vehicle (GameObject)
├─ Vehicle (Script) ← 우리가 만든 스크립트
├─ ArcadeVehicleController (Script) ← 에셋의 컨트롤러
├─ Rigidbody
├─ Collider
└─ Visual (3D 모델)
    └─ Car Model
```

#### 3단계: Vehicle.cs 설정

Vehicle GameObject 선택 > Inspector:
```
Vehicle (Script)
...
Arcade Vehicle Physics
└─ Arcade Vehicle Controller: [ArcadeVehicleController 컴포넌트 드래그]
```

**중요**:
- `Arcade Vehicle Controller` 필드에 **같은 GameObject의 ArcadeVehicleController 컴포넌트**를 드래그
- 에셋마다 컨트롤러 스크립트 이름이 다를 수 있음 (VehicleController, CarController 등)

#### 4단계: 동작 원리 이해

```csharp
// Vehicle.cs가 자동으로 Arcade Physics 컨트롤러를 켜고 끔

// 플레이어가 탑승하지 않았을 때
arcadeVehicleController.enabled = false; // 비활성화

// 플레이어가 탑승했을 때
arcadeVehicleController.enabled = true;  // 활성화

// 연료가 없을 때
arcadeVehicleController.enabled = false; // 비활성화
```

**장점**:
- Vehicle.cs와 Arcade Physics의 입력 충돌 방지
- 연료 시스템과 완벽한 통합
- 탑승/하차 시 자동으로 컨트롤러 전환

#### 5단계: 입력 설정 확인

**Arcade Vehicle Physics 에셋의 Input Manager 설정**:
- 수평 입력: `Horizontal` (A/D 또는 Left/Right)
- 수직 입력: `Vertical` (W/S 또는 Up/Down)

**Vehicle.cs는 다음만 처리**:
- `Space` 키: 차량 브레이크
- `F` 키: 차량 하차
- `Mouse 0` (좌클릭): 차량 사격
- 연료 소모 계산

### 에셋 없이 테스트하려면
Arcade Vehicle Physics 에셋이 없으면:
1. `Arcade Vehicle Controller` 필드를 **비워둠** (None)
2. Vehicle.cs의 기본 이동 코드로 작동 (Rigidbody 기반)
3. 나중에 에셋 추가 시 연결만 하면 됨

### 확인 사항
- ✅ 차량 프리팹에 Arcade Vehicle Physics 컨트롤러 컴포넌트 추가
- ✅ Vehicle.cs의 Arcade Vehicle Controller 필드에 컴포넌트 할당
- ✅ 플레이 모드에서 F키로 탑승 시 차량 제어 가능
- ✅ 차량 하차 시 컨트롤러 비활성화 확인

---

## 전체 설정 체크리스트

### Scene 설정
- [ ] ProjectilePool GameObject 생성 및 설정
- [ ] MapGenerator GameObject 생성 및 설정
- [ ] UIManager에 Slider UI 연결

### Prefabs 설정
- [ ] Bullet 프리팹 생성
- [ ] Land/Water Tile 프리팹 생성 (Tag 설정!)
- [ ] 미션 존 프리팹 생성 (MissionZoneInfo 추가)
- [ ] 차량 프리팹에 Arcade Vehicle Controller 추가

### UI 설정
- [ ] Image 기반 Stat Bar → Slider로 교체
- [ ] 모든 Slider Min=0, Max=1 설정
- [ ] UIManager에 Slider 연결

### Tags 설정
- [ ] `Terrain` 태그 생성 (Land Tile용)
- [ ] `Wall` 태그 생성 (Water Tile용)
- [ ] `Player` 태그 확인
- [ ] `Enemy` 태그 확인

### 실행 테스트
- [ ] Play 모드 진입 시 맵 생성 확인
- [ ] 미션 존이 맵에 배치되는지 확인
- [ ] Mesh Combining 후 2개의 Combined GameObject만 존재하는지 확인
- [ ] 플레이어 Stat 변화 시 Slider 업데이트 확인
- [ ] 차량 탑승/하차 시 컨트롤러 전환 확인
- [ ] 총 발사 시 ProjectilePool에서 총알 가져오기 확인

---

## 문제 해결

### Q: MapGenerator가 맵을 생성하지 않아요
A:
- Mission Zone Prefabs 리스트가 비어있지 않은지 확인
- Land/Water Tile Prefab이 할당되어 있는지 확인
- Console에 에러 메시지 확인

### Q: 미션 존이 배치되지 않아요
A:
- 미션 존 프리팹에 MissionZoneInfo 컴포넌트가 있는지 확인
- MissionZoneInfo의 Size가 너무 크지 않은지 확인 (맵 크기의 1/3 이하 권장)

### Q: Slider가 업데이트되지 않아요
A:
- UIManager의 Slider 필드에 Image가 아닌 Slider가 할당되어 있는지 확인
- PlayerStats의 이벤트가 UIManager에 구독되어 있는지 확인 (자동 설정됨)

### Q: 차량 컨트롤러가 작동하지 않아요
A:
- Arcade Vehicle Controller 필드에 컴포넌트가 할당되어 있는지 확인
- 탑승 후 차량의 ArcadeVehicleController.enabled가 true인지 확인
- 연료가 있는지 확인 (연료 없으면 컨트롤러 비활성화됨)

### Q: NavMeshSurface 에러가 발생해요
A:
- Window > Package Manager에서 AI Navigation 패키지 설치
- 에디터 재시작
- Unity 6.0에서는 AI Navigation 2.0.9 이상 필요

---

## 추가 참고 자료

- 전체 Unity 설정: `UNITY_SETUP_GUIDE.md`
- 원본 게임 디자인: 이전 대화 참고
- 스크립트 API: 각 스크립트 파일의 주석 참고
