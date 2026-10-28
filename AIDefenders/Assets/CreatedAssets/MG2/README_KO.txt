AI Defenders 머신건 II 터렛

MG.jpg의 사각 포신, 남색 장갑, 금색 전면 테두리를 참고한 로우폴리 유사 모델입니다.
이전 기관포·미사일·레이저 터렛과 동일한 받침대 형상 및 전체 높이(2.10유닛)를 사용합니다.
받침대 X/Z 실제 폭은 약 2.1753유닛입니다. 모델 전방 +Z / 위쪽 +Y / 바닥 원점.

[설치]
1. Assets/AIDefendersMG2Turret 폴더를 Unity 프로젝트 Assets에 복사합니다.
2. 컴파일 후 Tools > AI Defenders > Create MG2 Turret 선택.
3. Assets/AIDefendersMG2TurretGenerated/MG2Turret.prefab을 씬에 배치합니다.
4. 이전 포탑들과 같은 Scale을 사용하세요 (원본 1,1,1).
재생성 시 새 폴더를 만들어 이전 결과를 보존합니다.
기존 포탑 생성 도구와 이름이 달라 같이 설치할 수 있습니다.

[구조]
MG2Turret
  Base
  YawPivot        로컬 Y축 좌우 회전
    PitchPivot    로컬 X축 상하 조준 (음수 X는 위쪽)
      Recoil      포신 반동용 부모, 로컬 Z 음수 방향으로 후퇴
        MuzzlePoint  발사 위치 (forward = 발사 방향)
        포신 부품

[이전 기관포 이펙트와 연결]
이미 생성한 AutocannonFX.prefab이 있으면 MuzzlePoint 아래에 배치합니다.
이펙트 로컬 Position/Rotation을 0, Scale을 1로 설정합니다.
AutocannonFX 컴포넌트의 Recoil 칸에 이 모델의 Recoil을 연결합니다.
이펙트의 실제 발사 호출은 기존 공격 코드에 연결해야 합니다.
모델 자체에는 공격/피해/AI 코드나 애니메이션이 없습니다.

[파일]
Models/MG2Turret.obj + MG2Turret.mtl: 범용 모델 (같은 폴더에 유지)
Assets/: Unity 프리팹 생성 도구와 메시 데이터
Preview.png: 실제 메시 앞면/뒷면 렌더
build_model.py: 재생성 소스 (numpy)
render_preview.py: 미리보기 소스 (numpy/Pillow/DejaVu Sans 폰트)

[검증 및 제한]
20개 메시, 776개 삼각형.
이전 모델과 받침대·높이 일치, 메시 인덱스·삼각형 면적, 미리보기를 확인했습니다.
이 환경에는 Unity Editor가 없어 Unity 내부 컴파일/실행은 검증하지 못했습니다.
Built-in/URP용입니다. HDRP는 별도 머티리얼 설정이 필요합니다.
OBJ에는 피벗 계층이 없으므로 Unity에서는 제공된 생성 도구를 사용하세요.
UV/텍스처/FBX/애니메이션은 포함하지 않습니다.
