# Lost Island V2 integration

## 실행

- 월드 씬: `Assets/LostIsland/Generated/WorldV2/Scenes/LostIsland_Bootstrap.unity`
- 생성: `Lost Island > V2 > Build Streaming World`
- 재검사: `Lost Island > V2 > Validate World and Streaming` (Play 모드 진입/종료 포함)
- 프리팹 연결: `Lost Island > V2 > Prefab Bindings` → Save bindings → 월드 재생성
- 조작: WASD/방향키, 왼쪽 Shift 달리기, 휠 줌, R 시작점 복귀
- 원래 작은 테스트 섬: `Assets/LostIsland/Scenes/TestIsland.unity`

## 통합 범위

첨부 V2 데이터를 바탕으로 16 km × 16 km, 2 km 타일 8 × 8을 생성한다.
Terrain, 오브젝트, POI, Spawn Zone은 타일 씬에 저장하고 Bootstrap에는 플레이어와 시스템만 둔다.
현재 주변 3 × 3 타일을 순차 비동기 로드한다. 월드 가장자리에서는 유효 타일만 로드한다.
지면 타일이 준비되기 전에는 이동과 중력을 보류한다.

수정 사항:

- 새 Input System에 맞춘 화면 기준 이동, 정규화한 대각선 속도, 쿼터뷰 추적 및 줌.
- 높이 함수로 계산한 시작 높이와 로딩 대기 처리.
- 공유 월드 좌표 높이 샘플링, 연속 해안 경사, 로드된 Terrain 이웃 연결.
- 지상 POI 주변의 평탄화와 지면 샘플링. 바다의 시설은 기반 플랫폼에, 해저 POI는 해저에 배치.
- 식생은 타일 중심이 아닌 개별 위치의 바이옴에 따라 선택. 경사/해안/POI 접근 공간을 검사.
- 밀림과 설산 나무 비율, 바이옴별 배치 밀도, 늪지 수로 높이 변화, 산호 해역의 얕은 해저 및 깊은 해역의 해구.
- MonoBehaviour를 클래스명과 일치하는 파일로 분리하여 씬에 정상 직렬화.
- 생물 프리뷰는 지면/수중 높이에 배치하고 중복 생성 방지. 실제 AI 대신 `ILostIslandCreatureSpawner` 인터페이스 제공.
- 시설을 포함한 프리팹 바인딩 적용 및 Editor UI.
- 기존 빌드 씬을 유지하면서 V2 Bootstrap과 타일을 등록.

## 검증

`Logs/LostIslandV2Validation.json`에서 실제 검사 결과를 확인한다.
64개 Terrain/씬, 30개 POI, 누락 스크립트, 인접 높이맵 경계 오차를 검사한다.
Play 검사에서는 네 위치로 이동해 필요한 타일의 로드와 이전 타일의 언로드를 확인한다.
검증 이미지는 렌더링 장치가 있을 때 `Logs/LostIslandV2-preview.png`에 저장한다.

## 현재 제한

그래픽은 기본 도형과 단색 Terrain Layer를 사용하는 개발용 블록아웃이다.
생물은 정적 캡슐 프리뷰이며 실제 AI/전투/생태 시스템이 아니다.
해상 이동은 탐사용 임시 수면 이동이며 배/수영/잠수 시스템이 아니다.
물은 불투명 블록아웃 재질이므로 해저는 물 오브젝트를 숨기고 확인해야 한다.
항구의 해안 재배치·부두/창고, POI 연결 도로, 산호 모델 배치는 후속 작업이다.
현재 생물 Spawn Zone은 지역 중심 타일에 있으며, 권역 전체로 분할하는 후속 작업이 필요하다.
월드 좌표 변환 API는 직사각형 이미지 범위를 정사각형 월드에 매핑한다. V1 지도 마커를 V2 지형에 정합하는 작업은 별도다.
재생성은 `Generated/WorldV2`의 기존 자동 생성 에셋을 대체하므로 직접 편집한 타일은 별도로 보관한다.
