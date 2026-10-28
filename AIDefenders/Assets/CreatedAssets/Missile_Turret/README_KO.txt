AI Defenders 미사일 터렛 — 이전 포탑과 동일한 스케일

첨부 미사일 터렛 이미지를 참고한 로우폴리 유사 모델입니다.
사각형 빈 발사관, 노란 탄두, 양옆 금색 육각 장식, 청록색 표시등을 구성했습니다.
보이지 않는 뒷면은 환기구가 있는 형태로 보완했습니다.

[Unity 설치]
1. Assets/AIDefendersMissileTurret 폴더를 프로젝트의 Assets에 복사합니다.
2. 컴파일 완료 후 Tools > AI Defenders > Create Missile Turret 선택.
3. Assets/AIDefendersMissileTurretGenerated/MissileTurret.prefab을 씬에 배치합니다.
4. 이전 포탑과 같은 Transform Scale을 사용하세요. 원본 기준 (1,1,1)입니다.
생성 메뉴를 다시 실행하면 새 폴더가 생기며 기존 결과를 덮어쓰지 않습니다.
이전 기관포 생성 도구와 클래스·폴더·파일명이 달라 함께 설치할 수 있습니다.

[크기]
이전 포탑의 Foundation, GoldBelt, UpperPlinth, TopRim을 그대로 재사용했습니다.
모델 높이는 두 모델 모두 2.10유닛입니다.
받침대의 X/Z 실제 메시 폭은 약 2.1753유닛, 외접원 지름은 2.252유닛입니다.
이전 포탑은 포신이 앞으로 길게 돌출되지만 이 모델은 미사일이 짧아 전체 앞뒤 길이는 다릅니다.
즉 동일한 받침대와 높이를 기준으로 맞춘 스케일입니다.

[계층]
MissileTurret
  Base
  YawPivot             Y축 좌우 회전
    PitchPivot         X축 상하 조준, 로컬 음수 X 회전이 위쪽
      LoadedMissile    미사일 몸체/탄두/노즐의 부모
      LaunchPoint      전방 +Z, 발사 위치

모델 전방 +Z, 위쪽 +Y, 바닥 원점.
LoadedMissile을 숨겨 빈 발사관을 보여주고 재장전 시 다시 표시할 수 있습니다.
LaunchPoint는 탄두 앞에 배치되어 있습니다.
LoadedMissile의 원점은 발사관 중심 기준이므로 별도 투사체로 재사용할 때 원점을 확인하세요.
발사/유도/피해/폭발/재장전 동작 스크립트는 포함하지 않습니다.
기존 기관포용 섬광 이펙트는 이 모델에 자동 연결되지 않습니다.

[포함 파일]
Models/MissileTurret.obj + MissileTurret.mtl: 다른 3D 도구에서도 사용할 모델
Assets/: Unity 메시 데이터 및 프리팹 생성 도구
Preview.png: 실제 메시의 앞면/뒷면 미리보기
build_model.py: 모델 재생성 소스 (numpy 필요)
render_preview.py: 미리보기 소스 (numpy/Pillow 및 DejaVu Sans 폰트 필요)

[검증 범위]
메시 면적, 정점 인덱스, 이전 포탑과 받침대 일치/높이 일치, 미리보기 확인.
이 환경에는 Unity Editor가 없어 Unity 컴파일 및 실행은 검증하지 못했습니다.
Built-in 및 URP용 단색 머티리얼을 생성합니다. HDRP는 별도 설정이 필요합니다.
OBJ는 피벗 계층을 보존하지 않으므로 Unity에서는 프리팹 생성 도구 사용을 권장합니다.
UV/텍스처/FBX/애니메이션은 포함하지 않습니다.
