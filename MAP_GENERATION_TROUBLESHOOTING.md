# MapGenerator 맵 생성 문제 해결 가이드

맵이 생성되지 않을 때 확인해야 할 사항들을 순서대로 안내합니다.

---

## 1단계: Inspector 설정 확인

### MapGenerator GameObject 선택

**Hierarchy에서 MapGenerator 선택 > Inspector 확인**

#### 필수 설정 체크리스트

```
MapGenerator (Script)

Map Settings
├─ Map Width: 100 (0이 아닌 값)
├─ Map Height: 100 (0이 아닌 값)
└─ Random Seed: 0 또는 임의값

Noise Settings - Continent
├─ Continent Scale: 20
└─ Continent Threshold: 0.4

Noise Settings - Holes
├─ Hole Scale: 10
└─ Hole Weight: 0.3

Prefabs ⚠️ 중요!
├─ Land Tile Prefab: [반드시 할당!] ← 여기가 비어있으면 맵 안 생김
└─ Water Tile Prefab: [반드시 할당!] ← 여기가 비어있으면 맵 안 생김

Mission Zones (선택사항 - 비어있어도 타일은 생성됨)
├─ Mission Zone Prefabs: [리스트]
└─ Mission Zone Spacing: 20

Optimization
├─ Optimize Mesh: ✓ (체크)
└─ Water Wall Height: 5

References
└─ Map Parent: None (비워두면 자동 생성)
```

### 가장 흔한 문제: Prefab 미할당

**증상**: Play 모드 진입해도 아무것도 생성되지 않음

**원인**:
- `Land Tile Prefab` 필드가 None (비어있음)
- `Water Tile Prefab` 필드가 None (비어있음)

**해결**:
1. Project 창에서 LandTile 프리팹 생성
2. Project 창에서 WaterTile 프리팹 생성
3. MapGenerator Inspector에 드래그하여 할당

---

## 2단계: Console 메시지 확인

### Console 창 열기
**Window > General > Console** (또는 Ctrl+Shift+C)

### 정상 작동 시 보이는 메시지
```
Tiles spawned
Map generated with seed: 12345
Total land tiles: 4523
Mission zones placed: 0 (또는 1, 2, 3...)
```

### 에러 메시지별 해결 방법

#### ❌ "No mission zone prefabs assigned!"
```
원인: Mission Zone Prefabs 리스트가 비어있음
심각도: 낮음 (타일은 생성됨, 미션 존만 없음)
해결: Mission Zone 프리팹을 만들어서 리스트에 추가
```

#### ❌ "NullReferenceException: Object reference not set to an instance of an object"
```
원인: Land Tile Prefab 또는 Water Tile Prefab이 None
심각도: 높음 (맵이 전혀 생성되지 않음)
해결:
  1. LandTile, WaterTile 프리팹 생성
  2. MapGenerator Inspector에 할당
```

#### ❌ "The type or namespace name 'NavMeshSurface' could not be found"
```
원인: AI Navigation 패키지 미설치
심각도: 중간 (맵은 생성되지만 NavMesh 없음)
해결:
  1. Window > Package Manager
  2. AI Navigation 패키지 설치
  3. Unity 에디터 재시작
```

#### ❌ "Could not find valid position for mission zone"
```
원인: 미션 존이 너무 크거나, 맵이 너무 작음
심각도: 낮음 (타일은 생성됨, 특정 미션 존만 배치 실패)
해결:
  1. MissionZoneInfo의 Size를 줄임 (예: 15x15 → 10x10)
  2. 또는 맵 크기를 늘림 (예: 100x100 → 150x150)
```

---

## 3단계: Scene View에서 시각적 확인

### Hierarchy 확인

Play 모드 진입 후 Hierarchy에서 찾아야 할 것들:

```
Hierarchy
├─ MapGenerator
├─ Map (자동 생성됨)
│   ├─ LandTile_0_0
│   ├─ LandTile_0_1
│   ├─ ...
│   ├─ WaterWall_0_0
│   └─ ...
├─ CombinedLandMap (Optimize Mesh 켜면 생성)
└─ CombinedWaterWalls (Optimize Mesh 켜면 생성)
```

**맵이 생성되지 않았을 때**:
- `Map` GameObject가 없거나 비어있음
- 자식 오브젝트가 0개

**맵이 생성되었을 때**:
- `Map` GameObject 아래 LandTile_X_Y, WaterWall_X_Y가 수천 개 생성됨
- Optimize Mesh가 켜져 있으면 곧바로 `CombinedLandMap`과 `CombinedWaterWalls`로 합쳐지고 개별 타일은 삭제됨

### Scene View 확인

1. **Scene 탭 클릭**
2. **Map GameObject 더블클릭** (자동으로 카메라 이동)
3. **맵이 보이는지 확인**

**만약 아무것도 보이지 않는다면**:
- Prefab의 MeshRenderer가 비활성화되어 있을 수 있음
- Prefab에 Material이 할당되지 않았을 수 있음

---

## 4단계: Prefab 확인

### Land Tile Prefab 점검

**Project 창에서 LandTile 프리팹 선택 > Inspector**

```
LandTile (Prefab)
├─ Mesh Filter
│   └─ Mesh: Cube ✓
├─ Mesh Renderer
│   ├─ Enabled: ✓ 체크되어 있어야 함
│   └─ Materials: [녹색/갈색 Material] ✓
├─ Box Collider ✓
└─ Tag: Terrain ✓
```

**문제가 될 수 있는 것들**:
- ❌ Mesh Filter의 Mesh가 None
- ❌ Mesh Renderer가 비활성화됨
- ❌ Materials가 None (회색으로 표시됨)
- ❌ Tag가 Untagged

### Water Tile Prefab 점검

**Project 창에서 WaterTile 프리팹 선택 > Inspector**

```
WaterTile (Prefab)
├─ Mesh Filter
│   └─ Mesh: Cube ✓
├─ Mesh Renderer
│   ├─ Enabled: ✓ (MapGenerator가 나중에 비활성화함)
│   └─ Materials: [파란색 Material] ✓
├─ Box Collider: 없어도 됨 (MapGenerator가 추가함)
└─ Tag: Wall ✓
```

---

## 5단계: 빠른 테스트 - 최소 설정

### 최소한의 설정으로 테스트

Prefab이 없다면 임시로 기본 Cube를 사용해서 테스트할 수 있습니다.

#### 방법 1: 기본 Cube 사용

1. **Hierarchy에서 Cube 2개 생성**
   - 이름: `TempLandTile`, `TempWaterTile`

2. **TempLandTile 설정**
   - Material: 녹색
   - Tag: `Terrain`
   - Project 창으로 드래그해서 Prefab 생성

3. **TempWaterTile 설정**
   - Material: 파란색
   - Tag: `Wall`
   - Project 창으로 드래그해서 Prefab 생성

4. **MapGenerator에 할당**
   - Land Tile Prefab: TempLandTile
   - Water Tile Prefab: TempWaterTile

5. **Mission Zones는 비워둠**
   - 일단 타일 생성 테스트만 진행

6. **Play 모드 진입**

**예상 결과**:
- Console에 "Tiles spawned" 메시지
- Console에 "Map generated with seed: ..." 메시지
- Hierarchy에 Map GameObject 생성
- Scene View에 100x100 타일 격자 표시

---

## 6단계: 디버그 모드 활성화

### 물 타일 보이게 하기

기본적으로 물 타일은 투명(Renderer 비활성화)입니다. 디버깅을 위해 보이게 만들 수 있습니다.

**MapGenerator.cs 수정**:

265번 라인:
```csharp
// 기존 (물 안 보임)
renderer.enabled = false;

// 디버그용 (물 보임)
renderer.enabled = true;
```

이렇게 하면 파란색 물 타일이 보입니다.

### 추가 디버그 로그

MapGenerator.cs의 SpawnTiles() 함수에 로그 추가:

```csharp
private void SpawnTiles()
{
    Debug.Log($"Starting to spawn tiles. Map size: {mapWidth}x{mapHeight}");

    if (landTilePrefab == null)
        Debug.LogError("Land Tile Prefab is NULL!");

    if (waterTilePrefab == null)
        Debug.LogError("Water Tile Prefab is NULL!");

    // ... 나머지 코드
}
```

---

## 7단계: 성능 문제인 경우

### 증상: 맵 생성 중 Unity가 멈춤

**원인**: 100x100 = 10,000개 타일을 생성하는데 시간이 오래 걸림

**확인 방법**:
- Unity가 "응답 없음"처럼 보이지만 사실은 생성 중
- 1-2분 기다리면 완료될 수 있음

**해결 방법**:
1. **맵 크기 줄이기 (테스트용)**
   ```
   Map Width: 50
   Map Height: 50
   ```
   → 2,500개 타일로 훨씬 빠름

2. **Optimize Mesh 켜기**
   ```
   Optimize Mesh: ✓
   ```
   → Mesh Combining으로 성능 향상

3. **Profiler 확인**
   - Window > Analysis > Profiler
   - CPU Usage 탭에서 어디서 시간이 걸리는지 확인

---

## 문제별 빠른 해결 방법 요약

| 증상 | 가능한 원인 | 해결 방법 |
|------|------------|----------|
| 아무것도 생성 안 됨 | Prefab 미할당 | Land/Water Tile Prefab 할당 |
| Console에 경고만 있음 | Mission Zone 없음 | 무시해도 됨 (타일은 생성됨) |
| 맵이 보이지 않음 | Material/Renderer 문제 | Prefab의 Material 확인 |
| Unity가 멈춤 | 맵이 너무 큼 | 맵 크기를 50x50으로 줄임 |
| NavMesh 에러 | 패키지 미설치 | AI Navigation 패키지 설치 |
| 미션 존 배치 실패 | Zone이 너무 큼 | MissionZoneInfo Size 줄임 |

---

## 체크리스트: 맵 생성 전 필수 확인사항

```
[ ] MapGenerator GameObject가 Scene에 존재
[ ] Land Tile Prefab 할당됨 (None이 아님)
[ ] Water Tile Prefab 할당됨 (None이 아님)
[ ] Land Tile Prefab의 Tag = "Terrain"
[ ] Water Tile Prefab의 Tag = "Wall"
[ ] Map Width > 0 (예: 100)
[ ] Map Height > 0 (예: 100)
[ ] Land Tile Prefab에 MeshRenderer + Material 있음
[ ] Water Tile Prefab에 MeshRenderer + Material 있음
[ ] Console 창 열어서 에러 확인
```

---

## 여전히 작동하지 않는다면

다음 정보를 확인해주세요:

1. **Console 전체 에러 메시지** (스크린샷 또는 복사)
2. **MapGenerator Inspector 스크린샷**
3. **Hierarchy 구조** (Play 모드 후)
4. **Unity 버전** (Help > About Unity)
5. **AI Navigation 패키지 설치 여부**

이 정보가 있으면 정확한 문제를 진단할 수 있습니다.
