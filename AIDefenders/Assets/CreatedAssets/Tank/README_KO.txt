AI Defenders — Desert Tank

참고 사진의 실루엣을 바탕으로 제작한 모래색 로우폴리 탱크입니다.
실사 위장무늬와 표면 손상은 재현하지 않았으며 게임용으로 형태를 단순화했습니다.
주요 요소: 긴 주포, 측면 장갑판, 좌우 궤도와 차륜, 해치, 조준경, 안테나, 후방 적재함.

[Unity 적용]
1. Assets/AIDefendersTank 폴더를 프로젝트 Assets에 복사합니다.
2. 컴파일 완료 후 Tools > AI Defenders > Create Desert Tank 선택.
3. Assets/AIDefendersTankGenerated/DesertTank.prefab을 씬에 배치합니다.
다시 실행하면 새 폴더를 생성하여 기존 결과를 보존합니다.
Built-in/URP용이며 HDRP는 별도 머티리얼 설정이 필요합니다.

[구조]
DesertTank: 이동 스크립트를 연결할 루트
  Hull: 차체
  LeftTrack / RightTrack: 좌우 궤도와 차륜
  YawPivot: 포탑 좌우 조준 (로컬 Y축)
    PitchPivot: 주포 상하 조준 (로컬 X축, 음수가 위쪽)
      Recoil: 주포 반동 (로컬 Z 음수 방향으로 후퇴)
        MuzzlePoint: 발사 위치, forward가 전방

차체와 포탑, 주포를 독립적으로 제어할 수 있습니다.
궤도는 정적인 모델입니다. 궤도 순환/차륜 회전/주행 애니메이션은 포함하지 않습니다.
같은 동작 그룹 안의 같은 색 부품은 Unity 생성 시 합쳐 렌더러 수를 줄입니다.
개별 부품을 수정하려면 이름이 분리된 OBJ 또는 Python 생성 소스를 사용하세요.

[크기]
+Z 전방 / +Y 위쪽 / 지면 Y=0.
전체 폭 약 2.645, 전체 길이(주포 포함) 5.20유닛.
차체 길이 3.94유닛, 지붕 장비 높이 약 2.10, 안테나 끝 2.43유닛.
기존 높이 2.1유닛 포탑과 함께 배치하기 위한 게임용 크기이며 실물 축척은 아닙니다.
프리팹 Scale로 게임 맵에 맞춰 조절하세요.

[게임 연결]
주행/경로 추종/공격/체력/피해/AI/폭발 기능은 포함하지 않습니다.
기존 포탑 코드에 YawPivot, PitchPivot, MuzzlePoint를 연결해 사용할 수 있습니다.
이전에 제작한 AutocannonFX를 붙일 경우 MuzzlePoint 아래에 두고 로컬 위치·회전을 0으로 설정합니다.
해당 효과는 기관포용이므로 탱크 주포에 맞는 별도 연출 조정이 필요합니다.
콜라이더는 차체/포탑을 대략 감싸는 박스이며 주포·안테나에는 콜라이더가 없습니다.

[파일]
Models/Tank.obj + Tank.mtl: 범용 3D 모델. 같은 폴더에 유지하세요.
Assets/: 메시 데이터와 Unity 프리팹 생성 도구
Preview.png: 실제 메시의 앞/뒤 미리보기
build_model.py: 재생성 소스 (numpy)
render_preview.py: 미리보기 소스 (numpy, Pillow, DejaVu Sans 폰트)
원본 메시 부품 227개, 삼각형 7516개. Unity에서는 그룹/색 기준으로 합칩니다.
UV, 텍스처, FBX, 리깅, 애니메이션은 포함하지 않습니다.

[검증]
정점 인덱스, 삼각형 면적, 지면 높이, 미리보기를 확인했습니다.
Unity Editor가 없는 환경이라 Unity 내부 컴파일·실행은 검증하지 못했습니다.
