AI DEFENDERS — MG2 TWIN-BARREL UPGRADE

첨부 이미지를 참고한 머신건 2 업그레이드용 외형 모델입니다.
쌍열 사각 포신, 두 개의 독립 장갑 하우징, 상단 금색 볼트,
측면 탄창 형태 장식, 사각 받침대와 금색 테두리를 구성했습니다.
사진에 보이지 않는 뒷면은 냉각구 형태로 보완했습니다.

설치
1. 압축을 풉니다.
2. Assets/AIDefendersMG2Upgrade 폴더를 Unity 프로젝트 Assets에 복사합니다.
3. 컴파일이 끝나면 Tools > AI Defenders > Create MG2 Upgrade 선택.
4. Assets/AIDefendersMG2UpgradeGenerated/MG2UpgradeTurret.prefab을 씬에 배치합니다.
기존 MG2와 폴더/클래스/메뉴 이름이 달라 함께 설치할 수 있습니다.
메뉴를 다시 실행하면 새 폴더에 생성하여 이전 결과를 보존합니다.

기존 모델과 크기
높이 2.10유닛, 받침대 폭/깊이 약 2.1753유닛.
이전 머신건 2와 같은 높이 및 받침대 X/Z 최대 폭입니다.
원형 받침대를 사각으로 바꾸었으므로 모서리의 점유 면적은 더 넓습니다.
전방 +Z / 위쪽 +Y / 바닥 원점 / 기본 Scale (1,1,1).

계층
MG2UpgradeTurret
  Base: 고정 사각 받침대
  YawPivot: 로컬 Y축 좌우 조준
    PitchPivot: 로컬 X축 상하 조준, 위치 (0,1.50,0)
      Recoil_L: 왼쪽 포신 반동, 로컬 위치 (-0.40,0,0)
        MuzzlePoint_L: 왼쪽 발사 위치, 로컬 (0,0,1.68)
      Recoil_R: 오른쪽 포신 반동, 로컬 위치 (0.40,0,0)
        MuzzlePoint_R: 오른쪽 발사 위치, 로컬 (0,0,1.68)

반동은 각 Recoil의 초기 위치를 기준으로 로컬 Z 음수 방향으로 이동합니다.
초기 X 위치는 유지해야 합니다. +Z가 발사 방향이며 음수 X 조준각이 위쪽입니다.
두 개의 발사 위치와 반동 부모를 통해 교대/동시 사격을 연결할 수 있습니다.
포신별 반동을 위한 구조만 제공하며 실제 사격 애니메이션은 포함하지 않습니다.

이전 기관포 FX 연결
AutocannonFX 프리팹을 MuzzlePoint_L, MuzzlePoint_R 아래에 각각 배치합니다.
두 이펙트의 로컬 위치/회전을 0, 크기를 1로 설정합니다.
각 이펙트 컴포넌트의 Recoil에 Recoil_L 또는 Recoil_R을 연결합니다.
실제 공격 코드에서 원하는 쪽의 Fire(...)를 호출하도록 연결해야 합니다.
두 개의 발사 위치를 제공해도 자동으로 공격력/연사속도가 증가하지 않습니다.

게임의 업그레이드 연결
이 패키지는 업그레이드 외형이며 코인 지불·레벨·공격력 증가 기능은 포함하지 않습니다.
기존 포탑의 이동/체력/레벨 상태를 유지하고 시각 모델을 교체한 다음,
YawPivot/PitchPivot과 두 MuzzlePoint 참조를 다시 연결하는 방식으로 통합할 수 있습니다.
기존 공격 스크립트가 발사 위치를 하나만 받는다면 두 포구를 지원하도록 수정해야 합니다.

포함 파일
Assets/: Unity 생성 도구와 메시 데이터
Models/MG2Upgrade.obj + MG2Upgrade.mtl: 범용 모델. 같은 폴더에 유지하세요.
Preview.png: 실제 메시의 앞/뒤 미리보기
ModelInfo.json: 크기와 삼각형 수, 발사 위치
build_model.py: Python 모델 재생성 소스 (numpy)
render_preview.py: 미리보기 소스 (numpy/Pillow/DejaVu Sans 폰트)
53개 메시 / 1912개 삼각형 / 공유 머티리얼.

검증 및 제한
메시 삼각형/정점 인덱스/표면 방향, 피벗 좌표 복원, 기존 MG2와 높이 및 받침대 폭을 확인했습니다.
Unity Editor가 없는 환경이라 실제 Unity 컴파일/실행은 검증하지 못했습니다.
Built-in/URP용이며 HDRP는 별도 머티리얼 설정이 필요합니다.
콜라이더는 받침대와 머리를 대략 감싸는 박스이며 포신에는 별도 콜라이더가 없습니다.
OBJ는 피벗 계층을 보존하지 않으므로 Unity에서는 생성 도구 사용을 권장합니다.
UV/텍스처/FBX/발사 이펙트/공격 코드/애니메이션은 포함하지 않습니다.
