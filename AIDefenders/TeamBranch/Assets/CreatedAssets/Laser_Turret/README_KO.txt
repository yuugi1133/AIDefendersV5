AI Defenders 레이저 터렛

첨부 사진을 참고한 로우폴리 유사 모델입니다.
청록색 에너지 포신, 금색 전면 장갑, 옆면 발광 패널과 레일을 구성했습니다.
보이지 않는 뒷면은 환기구가 있는 형태로 보완했습니다.

[Unity 적용]
1. Assets/AIDefendersLaserTurret 폴더를 프로젝트 Assets에 복사합니다.
2. 컴파일 완료 후 Tools > AI Defenders > Create Laser Turret 선택.
3. Assets/AIDefendersLaserTurretGenerated/LaserTurret.prefab을 씬에 배치합니다.
4. 이전 기관포 및 미사일 터렛과 같은 Scale을 적용하세요 (원본 기준 1,1,1).
다시 생성하면 새 폴더에 저장해 기존 결과를 보존합니다.
다른 두 포탑과 파일명·클래스명이 달라 함께 사용할 수 있습니다.

[크기와 구조]
받침대는 이전 두 포탑과 정확히 같은 메시를 사용합니다.
높이 2.10유닛, 받침대 X/Z 실제 폭 약 2.1753유닛.
전방 +Z, 위쪽 +Y, 바닥 원점.
LaserTurret
  Base
  YawPivot        로컬 Y축 좌우 회전
    PitchPivot    로컬 X축 상하 조준 (음수 X가 위쪽)
      LaserOrigin 레이저 시작 위치, forward가 발사 방향
      기타 모델 부품

기관포와 달리 기본 반동 부품은 두지 않았습니다.
LaserOrigin을 기존 레이저/LineRenderer 스크립트의 시작점에 연결하세요.
레이저 빔, 충돌 검사, 공격 판정, 피해 및 충전 애니메이션은 포함하지 않습니다.
청록색 부품에 Emission 머티리얼을 적용합니다. 빛 번짐은 프로젝트의 Bloom 설정이 필요합니다.

[포함 파일]
Models/LaserTurret.obj + LaserTurret.mtl
Assets/: Unity 프리팹 생성 도구와 메시 데이터
Preview.png: 실제 메시를 렌더한 앞면/뒷면 미리보기 (Bloom 없음)
build_model.py: 메시 재생성 (numpy)
render_preview.py: 미리보기 재생성 (numpy, Pillow, DejaVu Sans 폰트)

[검증 및 제한]
31개 메시 부품, 1164개 삼각형.
이전 모델과 받침대·높이 일치, 삼각형 면적과 정점 인덱스, 미리보기를 확인했습니다.
Unity Editor가 없는 환경이라 Unity 내부 컴파일·실행은 검증하지 못했습니다.
Built-in 및 URP용입니다. HDRP 머티리얼은 별도 설정이 필요합니다.
OBJ는 피벗 계층을 보존하지 않으므로 Unity에서는 생성 도구 사용을 권장합니다.
UV, 텍스처, FBX, 애니메이션 파일은 포함하지 않습니다.
