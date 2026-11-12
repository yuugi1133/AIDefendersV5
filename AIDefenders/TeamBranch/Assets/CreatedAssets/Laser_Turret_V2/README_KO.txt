AI DEFENDERS — LASER TURRET V2

레이저(1).jpg를 참고한 로우폴리 게임용 모델입니다.
기존 레이저 모델을 바탕으로 앞뒤 모서리, 금색 장갑의 경사면,
측면 발광 캡슐, 포구와 받침대의 금색 장식을 다듬은 별도 버전입니다.
사진에 보이지 않는 뒷면에는 간단한 냉각구를 보완했습니다.

바로 설치하기
1. ZIP 압축을 풉니다.
2. Assets/AIDefendersLaserV2 폴더를 Unity 프로젝트의 Assets에 복사합니다.
3. 스크립트 컴파일 완료 후 상단 메뉴에서 다음을 누릅니다.
   Tools > AI Defenders > Create Laser Turret V2
4. 생성된 Assets/AIDefendersLaserV2Generated/LaserTurretV2.prefab을 씬으로 드래그합니다.
5. 다른 포탑들과 같은 Transform Scale을 적용합니다. 기본값은 (1,1,1)입니다.

이전 레이저 생성 도구와 클래스·폴더명이 달라 같이 설치할 수 있습니다.
생성 메뉴를 반복 실행하면 새 폴더에 저장되어 기존 프리팹을 덮어쓰지 않습니다.
Built-in/URP 재질을 지원합니다. 프로젝트 내 C# 컴파일 오류가 있으면 먼저 해결해야 메뉴가 표시됩니다.

크기와 연결
- 위쪽 +Y, 전방 +Z, 바닥 원점
- 높이 2.10 Unity 단위 (기존 포탑과 같음)
- 받침대 실제 X/Z 폭 약 2.1753 Unity 단위 (기존 포탑과 같음)
- 전체 앞뒤 길이 약 2.6676 Unity 단위
- Base: 고정 받침대
- YawPivot: Y축 좌우 회전
- PitchPivot: X축 상하 조준, 로컬 위치 (0,1.55,0)
- LaserOrigin: PitchPivot의 자식, 로컬 위치 (0,0,1.60)
  이 Transform의 position과 forward를 기존 레이저 발사 코드에 연결합니다.
- 머리를 위로 올릴 때 PitchPivot의 로컬 X 각도는 음수입니다.
- 기본 반동 부품은 없습니다.

머티리얼
에너지 포신, 측면 캡슐, 상태 표시등과 렌즈는 Glow 머티리얼을 공유합니다.
Unity에서 해당 머티리얼의 Emission을 조절하면 발광 강도를 바꿀 수 있습니다.
화면에서 빛이 번지는 효과는 별도의 Bloom 설정이 필요합니다.
Preview.png에는 실제 메시와 기본 발광색이 표시되며 Unity 화면 캡처는 아닙니다.
참고 사진과 달리 모델 뒷면은 새롭게 구성했으며 단색 재질을 사용합니다.

포함 파일
Assets/: Unity 메시 데이터와 프리팹 생성 도구
Models/LaserTurretV2.obj + LaserTurretV2.mtl: 범용 3D 모델
Preview.png: 실제 메시 앞/뒤 렌더
ModelInfo.json: 크기, 폴리곤 수, 축과 발사 위치 정보
build_model.py: 메시 재생성 소스 (Python + numpy)
render_preview.py: 미리보기 소스 (numpy + Pillow, DejaVu Sans 폰트 경로 조정 가능)

OBJ와 MTL은 같은 폴더에 유지하세요.
OBJ에는 피벗 계층이 없으므로 Unity에서는 제공된 생성 도구를 권장합니다.
37개 메시 부품 / 1532개 삼각형 / 공유 머티리얼 9개.
텍스처, UV, FBX, 애니메이션은 포함하지 않습니다.

검증 범위
정점 인덱스, 삼각형 면적, 높이와 받침대 폭, 피벗 기준 좌표,
표면 방향 및 실제 메시 앞/뒤 미리보기를 확인했습니다.
이 환경에는 Unity Editor가 없어 Unity 내부 컴파일/실행 검증은 하지 못했습니다.
HDRP는 별도의 머티리얼 설정이 필요합니다.
레이저 빔, 명중/피해, 적 탐지, 충전 애니메이션은 별도 게임 로직에 연결해야 합니다.
콜라이더는 받침대와 머리를 대략 감싸며 포신에는 별도 콜라이더가 없습니다.
