# Unity 세팅 가이드 - Car Survival Game POC

## 📌 목차
1. [Unity 프로젝트 생성](#1-unity-프로젝트-생성)
2. [필수 패키지 설치](#2-필수-패키지-설치)
3. [태그 및 레이어 설정](#3-태그-및-레이어-설정)
4. [씬 설정](#4-씬-설정)
5. [프리팹 생성](#5-프리팹-생성)
6. [게임 매니저 설정](#6-게임-매니저-설정)
7. [카메라 설정](#7-카메라-설정)
8. [UI 설정](#8-ui-설정)
9. [임무 설정](#9-임무-설정)
10. [NavMesh 베이킹](#10-navmesh-베이킹)
11. [최종 연결](#11-최종-연결)
12. [테스트](#12-테스트)

---

## 1. Unity 프로젝트 생성

### 1-1. Unity Hub에서 프로젝트 생성
```
1. Unity Hub 실행
2. "New Project" 클릭
3. 템플릿: "3D (URP)" 또는 "3D" 선택
4. 프로젝트 이름: Epic_POC6
5. 위치: git 저장소와 동일한 경로
6. "Create Project" 클릭
```

**권장 Unity 버전**: 2021.3 LTS 이상

---

## 2. 필수 패키지 설치

### 2-1. TextMeshPro
```
Window → Package Manager → Unity Registry
검색: "TextMeshPro" → Install
TMP Essentials 임포트
```

### 2-2. AI Navigation (NavMesh)
```
Window → Package Manager → Unity Registry
검색: "AI Navigation" → Install
```

### 2-3. Arcade Vehicle Physics
```
Unity Asset Store에서 다운로드 & Import
(무료 또는 유료 에셋)
```

---

## 3. 태그 및 레이어 설정

### 3-1. 태그 생성
```
Edit → Project Settings → Tags and Layers
Tags 섹션에서 추가:
- Player
- Enemy
- Terrain
- Wall
- Projectile
```

---

## 4. 씬 설정

### 4-1. 메인 씬
```
1. Assets/Scenes/MainGame.unity 열기
2. Hierarchy에서 기본 "Main Camera" 삭제
3. "Directional Light"는 유지
```

---

## 5. 프리팹 생성

### 5-1. Land Tile (맵 타일)

**생성:**
```
Hierarchy → 3D Object → Cube
Name: "LandTile"
```

**설정:**
```
Transform:
- Position: (0, 0, 0)
- Scale: (1, 0.1, 1)
Tag: Terrain
```

**Material:**
```
Assets/Materials → Create → Material
Name: "LandMaterial"
Albedo: 초록색 (#4CAF50)
→ LandTile에 적용
```

**프리팹화:**
```
LandTile을 Assets/Prefabs/Tiles로 드래그
Hierarchy에서 삭제
```

---

### 5-2. Projectile (총알)

**생성:**
```
Hierarchy → 3D Object → Sphere
Name: "Projectile"
```

**설정:**
```
Transform:
- Scale: (0.2, 0.2, 0.2)
Tag: Projectile

Components:
- Rigidbody:
  * Use Gravity: 체크 해제
  * Is Kinematic: 체크
- Sphere Collider:
  * Is Trigger: 체크
- Projectile (스크립트)
```

**Material:**
```
Name: "ProjectileMaterial"
Albedo: 노란색 (#FFEB3B)
```

**프리팹화:**
```
Assets/Prefabs로 드래그
Hierarchy에서 삭제
```

---

### 5-3. Player

**생성:**
```
Hierarchy → 3D Object → Capsule
Name: "Player"
Position: (50, 1, 50)  // 맵 중앙
Tag: Player
```

**Components:**
```
- Character Controller:
  * Radius: 0.5
  * Height: 2
  * Center: (0, 1, 0)

- Player Controller (스크립트)
- Player Stats (스크립트)
- Player Inventory (스크립트)
- Gun (스크립트)
```

**자식 오브젝트:**
```
Player → Create Empty → "FirePoint"
Position: (0, 1, 0.5)
```

**Material:**
```
Name: "PlayerMaterial"
Albedo: 파란색 (#2196F3)
```

**프리팹화:**
```
Assets/Prefabs로 드래그
씬에는 그대로 유지
```

---

### 5-4. Enemy

**생성:**
```
Hierarchy → 3D Object → Capsule
Name: "Enemy"
Tag: Enemy
```

**Components:**
```
- Nav Mesh Agent:
  * Speed: 3
  * Stopping Distance: 8
  * Auto Braking: 체크

- Enemy AI (스크립트)
- Enemy Stats (스크립트)
- Gun (스크립트)
```

**자식 오브젝트 - FirePoint:**
```
Enemy → Create Empty → "FirePoint"
Position: (0, 1, 0.5)
```

**자식 오브젝트 - PatrolPoints:**
```
Enemy → Create Empty → "PatrolPoints"
  → Create Empty → "Point1" (Position: 5, 0, 5)
  → Create Empty → "Point2" (Position: -5, 0, 5)
  → Create Empty → "Point3" (Position: 0, 0, -5)
```

**Material:**
```
Name: "EnemyMaterial"
Albedo: 빨간색 (#F44336)
```

**프리팹화:**
```
Assets/Prefabs로 드래그
Hierarchy에서 삭제 (임무에서 사용)
```

---

### 5-5. Vehicle

**생성:**
```
Hierarchy → 3D Object → Cube
Name: "Vehicle"
Scale: (2, 1, 3)
Position: (55, 0.5, 50)  // 플레이어 근처
```

**Components:**
```
- Rigidbody:
  * Mass: 1000
  * Drag: 0.5
  * Angular Drag: 2

- Arcade Vehicle Physics 컴포넌트
- Vehicle (스크립트)
- Gun (스크립트)
```

**자식 오브젝트:**
```
Vehicle → Create Empty → "ExitPoint" (Position: 2, 0, 0)
Vehicle → Create Empty → "FirePoint" (Position: 0, 1, 1.5)
```

**Material:**
```
Name: "VehicleMaterial"
Albedo: 회색 (#9E9E9E)
```

**프리팹화:**
```
Assets/Prefabs로 드래그
씬에는 유지
```

---

### 5-6. Items (5종)

각 아이템:
```
Hierarchy → 3D Object → Sphere
Scale: (0.5, 0.5, 0.5)

Components:
- Sphere Collider (Is Trigger: 체크)
- Item (스크립트)
```

**아이템별 설정:**

| 아이템 | Material 색상 | Item Type |
|--------|---------------|-----------|
| Medical | 빨간색 #E91E63 | Medical |
| Food | 주황색 #FF9800 | Food |
| Water | 파란색 #03A9F4 | Water |
| Parts | 회색 #607D8B | Parts |
| Fuel | 노란색 #FFC107 | Fuel |

**프리팹화:**
```
각각 Assets/Prefabs/Items로 드래그
Hierarchy에서 삭제
```

---

## 6. 게임 매니저 설정

### 6-1. GameManager
```
Hierarchy → Create Empty → "GameManager"
Position: (0, 0, 0)
Add Component: Game Manager (스크립트)
```

### 6-2. MapGenerator
```
Hierarchy → Create Empty → "MapGenerator"
Add Component: Map Generator (스크립트)

Inspector 설정:
- Map Width: 100
- Map Height: 100
- Seed: 0
- Continent Scale: 20
- Continent Threshold: 0.4
- Hole Scale: 10
- Hole Weight: 0.3
- Land Tile Prefab: LandTile 드래그
- Water Tile Prefab: (선택사항)
```

**GameManager 연결:**
```
GameManager 선택
- Map Generator 필드에 MapGenerator 드래그
```

### 6-3. MissionManager
```
Hierarchy → Create Empty → "MissionManager"
Add Component: Mission Manager (스크립트)
```

### 6-4. ProjectilePool
```
Hierarchy → Create Empty → "ProjectilePool"
Add Component: Projectile Pool (스크립트)

Inspector:
- Projectile Prefab: Projectile 드래그
- Initial Pool Size: 50
```

---

## 7. 카메라 설정

```
Hierarchy → Camera → "Main Camera"
Tag: MainCamera

Transform:
- Position: (50, 15, 45)  // 플레이어 위
- Rotation: (60, 0, 0)

Add Component: Top Down Camera (스크립트)

Inspector:
- Target: Player 드래그
- Offset: (0, 15, -5)
- Camera Angle: 60
- Smooth Follow: 체크
- Smooth Speed: 10
```

---

## 8. UI 설정

### 8-1. Canvas 생성
```
Hierarchy → UI → Canvas → "MainCanvas"

Canvas:
- Render Mode: Screen Space - Overlay

Canvas Scaler:
- UI Scale Mode: Scale With Screen Size
- Reference Resolution: 1920 x 1080
```

### 8-2. UIManager 추가
```
MainCanvas 선택
Add Component: UI Manager (스크립트)
```

### 8-3. 플레이어 스탯 UI (왼쪽 아래)

**Panel 생성:**
```
MainCanvas → 우클릭 → UI → Panel
Name: "PlayerStatsPanel"

Rect Transform:
- Anchors: Bottom Left
- Position: (150, 100, 0)
- Width: 250
- Height: 200

Image (Panel):
- Color: 반투명 검정 (0, 0, 0, 150)
```

**Health Bar:**
```
PlayerStatsPanel → UI → Image → "HealthBarBG"
- Anchors: Stretch Horizontal
- Pos Y: 80
- Height: 20
- Color: 어두운 빨강

HealthBarBG → UI → Image → "HealthBar"
- Anchors: Stretch
- Image Type: Filled
- Fill Method: Horizontal
- Color: 밝은 빨강
```

**마찬가지로 생성:**
```
StaminaBar (초록색)
HungerBar (주황색)
ThirstBar (파란색)
```

**탄약 텍스트:**
```
PlayerStatsPanel → UI → TextMeshPro → "AmmoText"
- Position: (0, -60, 0)
- Font Size: 24
- Color: 흰색
- Alignment: Center
```

### 8-4. 자동차 스탯 UI (왼쪽 아래)

```
MainCanvas → UI → Panel → "VehicleStatsPanel"
Position: (150, 320, 0)
Width: 250, Height: 100

자식:
- VehicleHealthBar (빨강)
- FuelBar (노랑)

초기 상태: Inactive (비활성화)
```

### 8-5. 미니맵 UI (오른쪽 아래)

**Panel:**
```
MainCanvas → UI → Panel → "MinimapPanel"

Rect Transform:
- Anchors: Bottom Right
- Position: (-150, 150, 0)
- Width: 250
- Height: 250

Image: 어두운 배경
```

**Minimap Image:**
```
MinimapPanel → UI → Raw Image → "MinimapImage"
- Anchors: Stretch
- Margin: 10px 사방
```

**Player Icon:**
```
MinimapImage → UI → Image → "PlayerIcon"
- Width: 10, Height: 10
- Color: 노랑
- Sprite: 화살표 또는 점
```

**MinimapController 추가:**
```
MinimapPanel 선택
Add Component: Minimap Controller (스크립트)

Inspector:
- Minimap Image: MinimapImage 드래그
- Minimap Rect: MinimapImage RectTransform 드래그
- Player Icon: PlayerIcon 드래그
```

### 8-6. 임무 목록 UI (오른쪽 위)

```
MainCanvas → UI → Panel → "MissionListPanel"

Rect Transform:
- Anchors: Top Right
- Position: (-150, -100, 0)
- Width: 300
- Height: 400

자식:
MainCanvas → UI → Vertical Layout Group → "MissionListContainer"
- Child Alignment: Upper Left
- Padding: 10px
```

**임무 Entry 프리팹:**
```
MissionListContainer → UI → Panel → "MissionEntry"
Width: 280, Height: 40

자식:
- UI → Image → "Checkbox" (10x10, 왼쪽)
- UI → TextMeshPro → "MissionNameText" (가운데)

MissionEntry를 Assets/Prefabs로 드래그
MissionListContainer에서 삭제
```

### 8-7. 인벤토리 UI (Tab)

**Inventory Panel:**
```
MainCanvas → UI → Panel → "InventoryPanel"

Rect Transform:
- Anchors: Stretch
- Margin: 200px 사방

Image: 반투명 어두운 배경
초기 상태: Inactive
```

**왼쪽 - 아이템 목록:**
```
InventoryPanel → UI → Panel → "LeftPanel"
- Anchors: Left Stretch
- Width: 400

자식:
LeftPanel → UI → Vertical Layout Group → "ItemContainer"
```

**오른쪽 - 전체 맵:**
```
InventoryPanel → UI → Panel → "RightPanel"
- Anchors: Right Stretch
- Width: 나머지 공간

자식:
RightPanel → UI → Raw Image → "FullMapImage"
- Anchors: Stretch
```

### 8-8. UIManager 연결

```
MainCanvas (UIManager) Inspector:

Player Stats UI:
- Health Bar: HealthBar 드래그
- Stamina Bar: StaminaBar 드래그
- Hunger Bar: HungerBar 드래그
- Thirst Bar: ThirstBar 드래그
- Ammo Text: AmmoText 드래그

Vehicle Stats UI:
- Vehicle Stats Panel: VehicleStatsPanel 드래그
- Vehicle Health Bar: VehicleHealthBar 드래그
- Fuel Bar: FuelBar 드래그

Minimap:
- Minimap Controller: MinimapPanel (MinimapController) 드래그

Mission List:
- Mission List Container: MissionListContainer 드래그
- Mission Entry Prefab: MissionEntry 프리팹 드래그

Inventory:
- Inventory Panel: InventoryPanel 드래그
- Inventory Item Container: ItemContainer 드래그
- Full Map Image: FullMapImage 드래그
```

---

## 9. 임무 설정

### 9-1. 소탕 임무

**임무 영역 생성:**
```
Hierarchy → Create Empty → "EliminationMission1"
Position: (30, 0, 30)  // 맵 내 임의 위치

Add Component: Elimination Mission (스크립트)

Inspector:
- Mission Name: "소탕 임무 1"
- Mission Description: "모든 적 처치"
```

**적 배치:**
```
EliminationMission1 자식으로 Enemy 프리팹 드래그 (3개)
각 Enemy 위치 조정:
- Enemy1: (28, 0, 28)
- Enemy2: (32, 0, 28)
- Enemy3: (30, 0, 32)
```

**각 Enemy 설정:**
```
EnemyAI 컴포넌트:
- Detection Range: 15
- Chase Range: 20
- Attack Range: 10
- Patrol Points: 해당 Enemy의 PatrolPoints 드래그
- Gun: 해당 Enemy의 Gun 컴포넌트 드래그
- Fire Point: 해당 Enemy의 FirePoint 드래그

Gun 컴포넌트:
- Range: 50
- Bullet Speed: 30
- Damage: 10
- Fire Rate: 0.5
- Max Ammo: 999 (적은 무한 탄약)
- Fire Point: FirePoint 드래그
- Projectile Pool: ProjectilePool 드래그
- Owner Tag: "Enemy"
```

### 9-2. 운반 임무

```
Hierarchy → Create Empty → "DeliveryMission1"
Position: (70, 0, 30)

Add Component: Delivery Mission (스크립트)

자식 오브젝트:
1. Create Empty → "PickupPoint"
   Position: (68, 0, 30)

2. Create Empty → "DeliveryPoint"
   Position: (72, 0, 35)

3. 시각화 (선택):
   PickupPoint → 3D Object → Sphere (노란색, Scale 0.5)
   DeliveryPoint → 3D Object → Sphere (초록색, Scale 0.5)

DeliveryMission1 Inspector:
- Mission Name: "운반 임무 1"
- Pickup Point: PickupPoint 드래그
- Delivery Point: DeliveryPoint 드래그
- Interaction Range: 2
```

### 9-3. 조작 임무

```
Hierarchy → Create Empty → "InteractionMission1"
Position: (30, 0, 70)

Add Component: Interaction Mission (스크립트)

자식:
Create Empty → "InteractionPoint"
Position: (30, 0, 70)

시각화:
InteractionPoint → 3D Object → Cylinder (보라색)

InteractionMission1 Inspector:
- Mission Name: "조작 임무 1"
- Interaction Point: InteractionPoint 드래그
- Interaction Range: 2
- Minigame Type: MouseHold
- Required Hold Time: 3
```

---

## 10. NavMesh 베이킹

### 10-1. NavMesh 설정

**맵 생성 먼저 실행:**
```
1. Play 버튼 눌러서 게임 실행
2. 맵이 생성되는지 확인
3. 정지

→ 맵 타일들이 씬에 생성되어 있어야 함
```

**NavMesh 베이킹:**
```
1. Window → AI → Navigation
2. Bake 탭 선택
3. 설정:
   - Agent Radius: 0.5
   - Agent Height: 2
   - Max Slope: 45
   - Step Height: 0.4

4. "Bake" 버튼 클릭
```

**주의사항:**
- 맵이 절차적 생성이므로, 매번 생성 후 NavMesh를 다시 베이킹해야 함
- 또는 런타임 NavMesh 빌드 사용 (NavMeshSurface 사용)

### 10-2. 런타임 NavMesh (권장)

```
Hierarchy → Create Empty → "NavMeshSurface"
Add Component: Nav Mesh Surface

Inspector:
- Collect Objects: All
- Include Layers: Default, Terrain

MapGenerator.cs 수정 필요:
- 맵 생성 완료 후 NavMeshSurface.BuildNavMesh() 호출
```

---

## 11. 최종 연결

### 11-1. Player 연결

```
Player Inspector:

PlayerController:
- Fire Point: Player/FirePoint 드래그
- Main Camera: Main Camera 드래그

Gun:
- Fire Point: Player/FirePoint 드래그
- Projectile Pool: ProjectilePool 드래그
- Owner Tag: "Player"
- Range: 50
- Bullet Speed: 30
- Damage: 10
- Fire Rate: 0.2
- Max Ammo: 30
- Current Ammo: 30
- Reload Time: 2
```

### 11-2. Vehicle 연결

```
Vehicle Inspector:

Vehicle:
- Exit Point: Vehicle/ExitPoint 드래그
- Vehicle Gun: Vehicle의 Gun 컴포넌트 드래그
- Fire Point: Vehicle/FirePoint 드래그
- Main Camera: Main Camera 드래그

Gun:
- (Player Gun과 동일하게 설정)
- Owner Tag: "Player"
```

### 11-3. UIManager 초기화

```
GameManager에 Start() 메서드에서:

void Start() {
    InitializeGame();

    // UI 초기화
    PlayerController player = FindObjectOfType<PlayerController>();
    PlayerStats stats = player.GetComponent<PlayerStats>();
    Gun gun = player.GetComponent<Gun>();

    UIManager.Instance.Initialize(stats, player, gun);

    // Minimap 초기화
    MinimapController minimap = UIManager.Instance.GetMinimapController();
    minimap.Initialize(mapGenerator, player.transform);
}
```

---

## 12. 테스트

### 12-1. 기본 테스트 체크리스트

**맵 생성:**
```
✓ Play 버튼 눌러서 맵이 생성되는지 확인
✓ 대륙 형태 + 중간에 구멍 있는지 확인
✓ Console에 "Map generated" 메시지 확인
```

**플레이어:**
```
✓ WASD로 이동
✓ Shift로 달리기 (스태미나 소모)
✓ Space로 구르기
✓ 마우스로 회전
✓ 좌클릭으로 발사
✓ R로 재장전
```

**UI:**
```
✓ 왼쪽 아래에 체력/스태미나/허기/수분 바 표시
✓ 오른쪽 아래에 미니맵 표시
✓ 오른쪽 위에 임무 목록 표시
✓ Tab으로 인벤토리 열기/닫기
```

**자동차:**
```
✓ F키로 탑승
✓ WASD로 운전 (Arcade Vehicle Physics)
✓ E키로 하차
✓ 카메라가 자동차 따라감
✓ 자동차 스탯 UI 표시
```

**적 AI:**
```
✓ 적이 순찰
✓ 플레이어 감지 시 추격
✓ 공격 범위에서 총 발사
✓ 죽으면 사라짐
```

**임무:**
```
✓ 소탕 임무: 모든 적 처치 시 완료
✓ 운반 임무: F키로 픽업 → 배달
✓ 조작 임무: F키로 시작 → 마우스 홀드
✓ 임무 완료 시 목록에서 체크
```

**아이템:**
```
✓ F키로 아이템 획득
✓ 1~5 키로 아이템 사용
✓ 효과 적용 확인
```

### 12-2. 디버그 키

```
F1: 자동차 체력 회복
F2: 자동차 연료 회복
```

### 12-3. 문제 해결

**맵이 생성 안 됨:**
```
- MapGenerator에 LandTile 프리팹 연결 확인
- Console 에러 확인
```

**플레이어가 움직이지 않음:**
```
- Character Controller 컴포넌트 확인
- 땅 위에 있는지 확인 (Y 위치)
```

**총이 발사 안 됨:**
```
- ProjectilePool에 Projectile 프리팹 연결 확인
- Gun의 Projectile Pool 연결 확인
- Fire Point 연결 확인
```

**적이 움직이지 않음:**
```
- NavMesh 베이킹 확인
- Nav Mesh Agent 컴포넌트 확인
- Patrol Points 연결 확인
```

**UI가 안 보임:**
```
- Canvas Render Mode 확인
- UIManager 연결 확인
- EventSystem 있는지 확인 (자동 생성됨)
```

---

## 13. 다음 단계

1. **밸런싱:**
   - 스탯 값 조정
   - 적 난이도 조정
   - 아이템 드롭률 설정

2. **비주얼 개선:**
   - 3D 모델 교체
   - 파티클 이펙트 추가
   - 애니메이션 추가

3. **사운드:**
   - 발사 효과음
   - 엔진 소리
   - 배경 음악

4. **추가 기능:**
   - 탈출 구역
   - 더 많은 임무 타입
   - 세이브/로드 시스템

---

## 📚 참고 사항

### 스크립트 실행 순서
```
1. GameManager - 가장 먼저
2. MapGenerator - 맵 생성
3. Player, Enemy, Vehicle - 생성된 맵 위에
4. UI - 마지막
```

### 중요 설정
```
- Time.timeScale: 인벤토리 열 때 0, 닫을 때 1
- Physics Layers: 발사체 충돌 설정
- NavMesh: 런타임 빌드 또는 미리 베이킹
```

### 성능 최적화
```
- Static Batching: 맵 타일
- Object Pooling: 발사체
- Culling: 카메라 범위 밖 오브젝트
```

---

완료! 이제 게임을 플레이할 수 있습니다! 🎮
