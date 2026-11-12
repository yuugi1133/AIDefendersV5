AI Defenders — 레퍼런스 기반 로우폴리 포탑

제공 이미지의 색상과 형태를 참고해 절차적으로 제작한 유사 모델입니다.
뒷면은 새롭게 구성했습니다. 텍스처 없이 단색 머티리얼을 사용합니다.

[Unity에서 권장 사용법]
1. 압축을 풉니다.
2. 압축 안 Assets/AIDefendersTurret 폴더를 프로젝트의 Assets 안에 복사합니다.
3. Unity 스크립트 컴파일이 끝나면 상단 메뉴에서
   Tools > AI Defenders > Create Reference Turret 를 선택합니다.
4. Assets/AIDefendersTurretGenerated 안의 ReferenceTurret.prefab을 씬에 끌어 넣습니다.
   재실행하면 새 폴더를 만들어 기존 결과를 보존합니다.

[구조]
ReferenceTurret
  Base                 고정 받침대
  YawPivot             로컬 Y축으로 좌우 회전
    PitchPivot         로컬 X축으로 상하 조준 (음수 각도가 위쪽)
      Recoil           로컬 Z를 음수로 이동해 반동 구현
        MuzzlePoint    탄환 생성 위치, forward가 발사 방향

전방 +Z / 위쪽 +Y / 바닥 원점. 전체 높이 약 2.1 Unity 단위.
메시 부품 18개. 생성된 프리팹의 부품별 메시와 머티리얼을 수정할 수 있습니다.
기본 BoxCollider는 전체 외형을 대략 감싸며 필요에 따라 수정하세요.
AI 탐지, 공격, 탄환, 반동 애니메이션 스크립트는 포함하지 않았습니다.
기존 게임의 포탑 스크립트에 회전부와 MuzzlePoint를 연결해야 합니다.

[다른 모델링 도구]
Models/Turret.obj 와 Turret.mtl을 같은 폴더에 두고 가져오세요.
OBJ는 부품 이름과 색상이 있지만 회전 피벗 계층은 저장하지 않습니다.
Unity에서는 위 프리팹 생성 도구를 사용하는 것을 권장합니다.
build_model.py는 수정 및 재생성용 Python 소스이며 numpy와 matplotlib가 필요합니다.
Preview.png는 실제 메시를 그린 미리보기입니다. Unity 조명에 따라 외관은 달라집니다.

[검증 및 제한]
메시 인덱스와 삼각형 면적 검증 및 모델 미리보기 확인을 수행했습니다.
이 환경에는 Unity Editor가 없어 Unity 안에서 컴파일·실행 검증은 하지 못했습니다.
프리팹 생성 도구는 Built-in 및 URP용입니다. HDRP는 별도 머티리얼 설정이 필요합니다.
UV, 텍스처, 리깅, 애니메이션 및 FBX 파일은 포함하지 않습니다.
