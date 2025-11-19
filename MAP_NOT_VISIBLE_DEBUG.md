# 맵이 보이지 않는 문제 해결 가이드

오브젝트는 생성되지만 Scene/Game View에서 보이지 않는 경우

---

## 1단계: Hierarchy 확인

### Play 모드 진입 후 Hierarchy 확인

```
Hierarchy
├─ Map (이게 있나요?)
│   ├─ LandTile_0_0 (자식이 있나요?)
│   ├─ LandTile_0_1
│   └─ ...
├─ CombinedLandMap (이게 있나요?)
└─ CombinedWaterWalls (이게 있나요?)
```

### 확인 사항

#### Case 1: Map GameObject가 비어있음
```
Hierarchy
└─ Map (자식 0개)
```

**원인**: Land Tile Prefab 또는 Water Tile Prefab이 할당되지 않음

**해결**:
1. Play 모드 종료
2. MapGenerator 선택
3. Inspector 확인:
   ```
   Prefabs
   ├─ Land Tile Prefab: None ← 여기가 비어있음!
   └─ Water Tile Prefab: None ← 여기가 비어있음!
   ```
4. LandTile, WaterTile 프리팹을 드래그해서 할당

#### Case 2: Map GameObject에 자식은 많지만 CombinedLandMap이 없음
```
Hierarchy
└─ Map
    ├─ LandTile_0_0 (수천 개)
    └─ ...
```

**원인**: Optimize Mesh가 꺼져있거나, Mesh Combining 실패

**해결**: 아래 2단계로 진행

#### Case 3: CombinedLandMap만 있고 개별 타일이 없음
```
Hierarchy
├─ Map (비어있음)
├─ CombinedLandMap
└─ CombinedWaterWalls
```

**정상입니다!** Mesh Combining이 성공했습니다.
→ 2단계로 진행

---

## 2단계: CombinedLandMap 확인

### Hierarchy에서 CombinedLandMap 선택

#### Inspector 확인

```
CombinedLandMap
├─ Transform
│   └─ Position: (0, 0, 0) ← 확인
├─ Mesh Filter
│   └─ Mesh: Combined Mesh ← 있나요?
├─ Mesh Renderer
│   ├─ Enabled: ✓ ← 체크되어 있나요?
│   └─ Materials
│       └─ Element 0: None ← 여기가 None이면 안 보임!
└─ Mesh Collider (있어야 함)
```

### 문제별 해결

#### 문제 A: Mesh Renderer가 비활성화됨
```
Mesh Renderer
└─ Enabled: ✗ (체크 해제됨)
```

**해결**: 체크박스 클릭해서 활성화

#### 문제 B: Materials가 None
```
Mesh Renderer
└─ Materials
    └─ Element 0: None (Missing Material)
```

**원인**: Land Tile Prefab에 Material이 없었음

**해결**:
1. Play 모드 종료
2. Project 창에서 Land Tile Prefab 선택
3. Inspector:
   ```
   Mesh Renderer
   └─ Materials
       └─ Element 0: [녹색 Material 할당]
   ```
4. 다시 Play 모드 진입

#### 문제 C: Mesh가 None
```
Mesh Filter
└─ Mesh: None
```

**원인**: Mesh Combining 실패

**해결**:
1. Console 창에서 에러 확인
2. "Cannot combine mesh that does not allow access" 에러가 있다면:
   - Cube 대신 Plane 사용
   - 또는 FBX Mesh Import (Read/Write 켜기)

---

## 3단계: Scene View 확인

### Scene View로 전환

Game View가 아닌 **Scene View** 탭 클릭

### Map GameObject 찾기

1. **Hierarchy에서 Map GameObject 클릭**
2. **키보드 'F' 키 누르기** (Focus)
   - Scene View 카메라가 Map으로 이동

또는:

1. **Hierarchy에서 CombinedLandMap 클릭**
2. **키보드 'F' 키 누르기**

### 확인 사항

- **맵이 보이나요?**
  - 예: Game View 카메라 위치 문제 → 4단계로
  - 아니오: Renderer/Material 문제 → 위 2단계 재확인

---

## 4단계: 카메라 위치 확인

### Scene View에서 맵이 보이지만 Game View에서 안 보이는 경우

**원인**: 카메라가 맵을 보고 있지 않음

### Main Camera 위치 확인

1. **Hierarchy에서 Main Camera (또는 TopDownCamera) 선택**
2. **Inspector > Transform 확인**
   ```
   Transform
   ├─ Position: (50, 30, 50) ← 맵 중심을 향해야 함
   ├─ Rotation: (60, 0, 0) ← 아래를 봐야 함
   └─ Scale: (1, 1, 1)
   ```

### 권장 카메라 위치 (100x100 맵 기준)

```
Main Camera
├─ Position
│   ├─ X: 50 (맵 가로 중심)
│   ├─ Y: 30-50 (높이)
│   └─ Z: 50 (맵 세로 중심)
└─ Rotation
    ├─ X: 45-60 (위에서 아래로)
    ├─ Y: 0
    └─ Z: 0
```

### 빠른 테스트

1. **Scene View에서 맵이 잘 보이는 각도로 조정**
2. **Scene View 카메라를 선택한 상태에서**
3. **GameObject > Align View to Selected (Ctrl+Shift+F)**
4. **Main Camera를 현재 Scene View 위치로 이동**

---

## 5단계: Prefab 구조 확인

### Land Tile Prefab이 제대로 만들어졌는지 확인

**Project 창에서 Land Tile Prefab 선택 > Inspector**

#### 필수 컴포넌트 체크

```
LandTile (Prefab)
├─ Transform
│   └─ Scale: (1, 1, 1) ✓
├─ Mesh Filter ✓
│   └─ Mesh: Cube (또는 Plane) ✓
├─ Mesh Renderer ✓
│   ├─ Enabled: ✓
│   └─ Materials
│       └─ Element 0: [녹색/갈색 Material] ✓ (None이 아님!)
├─ Box Collider ✓
└─ Tag: Terrain ✓
```

#### 문제가 있다면

**Mesh Renderer가 없음**:
- Add Component > Mesh Renderer

**Materials가 None**:
- Project 우클릭 > Create > Material
- 이름: LandMaterial
- Albedo Color: 녹색 또는 갈색
- LandTile Prefab의 Mesh Renderer > Materials에 드래그

**Mesh Filter가 없음**:
- Add Component > Mesh Filter
- Mesh: Cube 할당

---

## 6단계: 디버그 모드 - 물 타일 보이게 하기

### 물 타일도 보이게 설정

MapGenerator.cs는 기본적으로 물 타일의 Renderer를 비활성화합니다 (투명 벽).

**테스트를 위해 보이게 만들기**:

1. **Assets/Scripts/Map/MapGenerator.cs 열기**
2. **265번 라인 찾기**:
   ```csharp
   // 기존
   renderer.enabled = false;

   // 디버그용으로 변경
   renderer.enabled = true;
   ```
3. **저장 후 Play 모드 재진입**

이제 파란색 물 타일도 보여야 합니다.

---

## 7단계: 최소 테스트 - 단일 Cube 생성

### MapGenerator 없이 수동으로 확인

1. **Play 모드 종료**
2. **Hierarchy 우클릭 > 3D Object > Cube**
3. **Transform**:
   ```
   Position: (50, 0, 50)
   Scale: (1, 1, 1)
   ```
4. **Material**: 녹색 할당
5. **Play 모드 진입**

**Cube가 보이나요?**
- 예: MapGenerator의 Prefab 설정 문제
- 아니오: 카메라 위치 또는 렌더링 설정 문제

---

## 문제별 빠른 해결 표

| 증상 | 원인 | 해결 |
|------|------|------|
| Hierarchy에 Map이 비어있음 | Prefab 미할당 | Land/Water Tile Prefab 할당 |
| CombinedLandMap의 Material이 None | Prefab에 Material 없음 | Prefab에 Material 추가 |
| Scene View에서 보이지만 Game View에서 안 보임 | 카메라 위치 | 카메라를 (50, 30, 50)으로 이동 |
| 모든 GameObject가 있지만 안 보임 | Mesh Renderer 비활성화 | Renderer 활성화 |
| "Cannot combine mesh" 에러 | Read/Write 불가능한 Mesh | Cube 대신 Plane 사용 |

---

## 체크리스트

### Play 모드 전 확인
```
[ ] MapGenerator에 Land Tile Prefab 할당됨
[ ] MapGenerator에 Water Tile Prefab 할당됨
[ ] Land Tile Prefab에 Mesh Renderer 있음
[ ] Land Tile Prefab에 Material 할당됨
[ ] Land Tile Prefab의 Tag = "Terrain"
[ ] Water Tile Prefab의 Tag = "Wall"
```

### Play 모드 후 확인
```
[ ] Hierarchy에 Map GameObject 존재
[ ] Hierarchy에 CombinedLandMap 존재
[ ] CombinedLandMap의 Mesh Renderer 활성화
[ ] CombinedLandMap의 Materials가 None이 아님
[ ] Scene View에서 맵이 보임
[ ] Game View에서 맵이 보임
```

---

## 스크린샷으로 확인할 것

다음을 스크린샷으로 확인해주세요:

1. **MapGenerator Inspector**
   - Prefabs 섹션

2. **Hierarchy (Play 모드)**
   - Map, CombinedLandMap 존재 여부

3. **CombinedLandMap Inspector**
   - Mesh Filter, Mesh Renderer, Materials

4. **Land Tile Prefab Inspector**
   - 전체 컴포넌트 구조

5. **Scene View**
   - 맵이 보이는지 확인

6. **Game View**
   - 맵이 보이는지 확인

이 정보가 있으면 정확한 문제를 찾을 수 있습니다!

---

## 가장 흔한 원인 TOP 3

### 1. Land Tile Prefab에 Material이 없음 (70%)
→ Prefab에 녹색 Material 추가

### 2. Prefab이 MapGenerator에 할당되지 않음 (20%)
→ Inspector에서 Land/Water Tile Prefab 드래그

### 3. 카메라가 맵을 보고 있지 않음 (10%)
→ 카메라를 (50, 30, 50) 위치로 이동

이 3가지만 확인하면 대부분 해결됩니다!
