# Water Tile 프리팹 생성 가이드

MapGenerator의 새로운 구조에서 Water Tile은 **투명한 벽** 역할을 합니다.

---

## Water Tile의 역할

1. **시각적 표현**: 파란색 물 타일 표시
2. **물리적 장벽**: 5m 높이의 투명한 벽으로 플레이어가 물에 빠지는 것 방지
3. **Mesh Combining**: 모든 물 타일을 하나의 Mesh로 결합하여 성능 최적화

---

## Water Tile 프리팹 생성 방법

### 1단계: 기본 Cube 생성

1. **Hierarchy에서 3D Object > Cube 생성**
   - 이름: `WaterTile`

2. **Transform 설정**
   ```
   Transform
   ├─ Position: X=0, Y=0, Z=0
   ├─ Rotation: X=0, Y=0, Z=0
   └─ Scale: X=1, Y=1, Z=1
   ```

   **중요**: Scale을 1,1,1로 유지하세요. MapGenerator가 tileSize로 자동 스케일링합니다.

### 2단계: Material 적용

1. **Material 생성**
   - Project 창에서 우클릭 > Create > Material
   - 이름: `WaterMaterial`

2. **Material 설정**
   ```
   WaterMaterial
   ├─ Shader: Standard (또는 URP/Lit)
   ├─ Albedo Color: 파란색 (R:0, G:0.5, B:1)
   ├─ Metallic: 0
   ├─ Smoothness: 0.8 (물처럼 반사)
   └─ Emission: 선택사항
   ```

3. **WaterTile Cube에 Material 적용**
   - WaterMaterial을 WaterTile의 Inspector > Mesh Renderer > Materials에 드래그

### 3단계: Collider 설정 (중요!)

**기존 Box Collider 제거**:
- WaterTile 선택
- Inspector에서 Box Collider 컴포넌트 우클릭 > Remove Component

**왜 제거하나요?**
- MapGenerator.cs의 `CreateWaterWallsForTile()` 함수가 자동으로 5m 높이 BoxCollider를 생성합니다
- 기본 Collider는 1m 높이라서 플레이어가 점프하면 넘어갈 수 있습니다

### 4단계: Tag 설정 (필수!)

1. **WaterTile 선택**
2. **Inspector 상단 Tag 드롭다운 클릭**
3. **`Wall` 태그 선택**
   - 태그가 없다면: Add Tag... > + 버튼 > "Wall" 입력 > Save

**Tag가 왜 필요한가요?**
- MapGenerator가 "Wall" 태그로 모든 물 타일을 찾아서 Mesh Combining을 수행합니다
- `CombineTilesByTag("Wall", "CombinedWaterWalls")` 함수가 이 태그로 검색합니다

### 5단계: Layer 설정 (선택사항)

물 타일만의 Layer를 만들면 충돌 필터링 가능:
```
Layer: Water (새로 생성)
```

Edit > Project Settings > Tags and Layers에서 Layer 추가 가능

### 6단계: Prefab으로 저장

1. **Project 창에서 Prefabs 폴더 생성**
   - 우클릭 > Create > Folder
   - 이름: `Prefabs` (없다면)

2. **WaterTile을 Prefab으로 저장**
   - Hierarchy의 WaterTile을 Project 창의 Prefabs 폴더로 드래그
   - 파란색 Cube 아이콘으로 변경됨 (Prefab 표시)

3. **Hierarchy에서 WaterTile 삭제**
   - Scene에는 프리팹만 남기고 원본 삭제

---

## 최종 Water Tile 프리팹 구조

```
WaterTile (Prefab)
├─ Transform
│   └─ Scale: (1, 1, 1)
├─ Mesh Filter
│   └─ Mesh: Cube
├─ Mesh Renderer
│   └─ Material: WaterMaterial (파란색)
├─ Tag: Wall ← 필수!
└─ (Box Collider 없음) ← MapGenerator가 자동 생성
```

---

## MapGenerator에 연결

1. **MapGenerator GameObject 선택**
2. **Inspector > Prefabs 섹션**
   ```
   Prefabs
   ├─ Land Tile Prefab: [LandTile]
   └─ Water Tile Prefab: [WaterTile] ← 여기에 드래그
   ```

---

## 작동 원리 이해

### MapGenerator.cs의 물 타일 처리 과정

#### 1. 타일 생성 (`CreateTile()`)
```csharp
private void CreateTile(int x, int y, int value)
{
    if (value == 0) // 물 타일
    {
        GameObject tile = Instantiate(waterTilePrefab, position, Quaternion.identity);
        tile.tag = "Wall"; // 태그 설정
        CreateWaterWallsForTile(tile, position); // 투명 벽 생성
    }
}
```

#### 2. 투명 벽 생성 (`CreateWaterWallsForTile()`)
```csharp
private void CreateWaterWallsForTile(GameObject waterTile, Vector3 position)
{
    // 5m 높이의 BoxCollider 추가
    BoxCollider wallCollider = waterTile.AddComponent<BoxCollider>();
    wallCollider.size = new Vector3(tileSize, 5f, tileSize); // 높이 5m
    wallCollider.center = new Vector3(0, 2.5f, 0); // 중심을 위로

    // Renderer 비활성화 (투명 벽)
    MeshRenderer renderer = waterTile.GetComponent<MeshRenderer>();
    if (renderer != null)
    {
        renderer.enabled = false; // 보이지 않음
    }
}
```

#### 3. Mesh Combining (`OptimizeMap()`)
```csharp
private void OptimizeMap()
{
    // "Wall" 태그를 가진 모든 물 타일 찾기
    CombineTilesByTag("Wall", "CombinedWaterWalls", false);

    // 결과: 10,000개 물 타일 → 1개의 Combined Mesh
}
```

---

## 고급 설정 (선택사항)

### 옵션 1: 애니메이션 효과 추가

**물결 효과 Shader** (URP):
1. Shader Graph로 물결 Shader 생성
2. Sine Wave로 UV 왜곡
3. WaterMaterial에 적용

### 옵션 2: 반투명 물

**Material 설정**:
```
Rendering Mode: Transparent
Albedo: 파란색 + Alpha 조절 (0.5 정도)
```

**주의**: 반투명 Material은 Mesh Combining 후에도 작동하지만 성능 영향 있음

### 옵션 3: 물 높이 조절

WaterTile의 Transform > Position Y 값 조정:
- Y = -0.1: 육지보다 약간 낮은 물
- Y = 0: 육지와 같은 높이
- Y = 0.1: 육지보다 약간 높은 물

---

## Land Tile 프리팹 (참고용)

Water Tile과 거의 동일하지만 차이점:

```
LandTile (Prefab)
├─ Tag: Terrain ← "Wall"이 아닌 "Terrain"
├─ Material: LandMaterial (녹색/갈색)
├─ Box Collider: 유지 (1x1x1) ← 제거하지 않음
└─ Scale: (1, 1, 1)
```

**Land Tile 생성 방법**:
1. Cube 생성 → 이름: `LandTile`
2. Material: 녹색/갈색
3. Tag: `Terrain`
4. Box Collider: **유지** (기본 그대로)
5. Prefab으로 저장

---

## 문제 해결

### Q: 물 타일이 보이지 않아요
A:
- MeshRenderer가 있는지 확인
- Material이 할당되어 있는지 확인
- MapGenerator가 CreateWaterWallsForTile()을 호출하면 Renderer가 비활성화되므로, 생성 전에는 보여야 합니다

### Q: 플레이어가 물에 빠져요
A:
- WaterTile의 Tag가 "Wall"인지 확인
- MapGenerator가 CreateWaterWallsForTile()을 호출하는지 확인 (Console 로그 확인)
- 생성된 BoxCollider의 높이가 5m인지 확인

### Q: Mesh Combining이 안 돼요
A:
- WaterTile의 Tag가 정확히 "Wall"인지 확인 (대소문자 구분)
- Mesh Filter와 Mesh Renderer가 있는지 확인
- OptimizeMap() 함수가 호출되는지 Console 확인

### Q: 성능이 여전히 느려요
A:
- Mesh Combining 후 "CombinedWaterWalls" GameObject가 생성되었는지 확인
- Static Batching 활성화: WaterTile을 Static으로 설정 (상단 Static 체크박스)

---

## 빠른 설정 체크리스트

```
[ ] Cube 생성 → 이름: WaterTile
[ ] Scale: (1, 1, 1) 유지
[ ] Material: 파란색 WaterMaterial 적용
[ ] Box Collider 제거
[ ] Tag: "Wall" 설정
[ ] Prefabs 폴더에 Prefab으로 저장
[ ] MapGenerator의 Water Tile Prefab 필드에 할당
```

---

## 실행 테스트

1. **Play 모드 진입**
2. **Scene View에서 확인**:
   - 물 타일이 맵에 생성됨
   - "CombinedWaterWalls" GameObject가 생성됨
   - 개별 물 타일들은 Combined Mesh에 합쳐짐
3. **플레이어 이동 테스트**:
   - 물 경계에서 투명한 벽에 막히는지 확인
   - 물에 빠지지 않는지 확인

완료!
