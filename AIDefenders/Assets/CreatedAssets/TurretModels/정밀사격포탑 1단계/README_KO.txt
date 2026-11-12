정밀사격포탑 1단계 — AI Defenders

사용자 1.jpg의 본체/보강띠와 원형 받침대 기반.
2.jpg처럼 길고 단순한 단일 사각 총열, 본체 위 작은 조준경 추가.
신규 정밀사격 계열: 단계가 올라가도 총열 수는 1개를 유지하고
총열 길이/조준경 크기/장갑과 외형을 강화하는 방향입니다.
이 패키지에는 1단계만 포함합니다.
이전 머신건 2·3단계는 기획상 폐기안이며, 해당 파일을 삭제하지 않았습니다.

높이 약 2.1 (조준경 포함), 받침 지름 약 2.2.
+Y 위 / +Z 발사 방향 / 바닥 원점.
Unity 설치: Assets/AIDefendersPrecisionLevel1 폴더를 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Precision Turret Level 1 실행.
Assets/AIDefendersPrecisionLevel1Generated/PrecisionLevel1.prefab 생성.
반복 실행 시 새 폴더 생성. 기존 에셋들과 이름/클래스 충돌 없이 보관 가능.
Built-in/URP용, HDRP 별도 머티리얼 설정 필요.

Base: 고정 받침 / YawPivot: Y축 회전 / PitchPivot: 높이1.4, X축 조준.
Recoil: 단일 총열, 로컬 -Z로 반동 이동 가능.
MuzzlePoint: 실제 발사 위치 / ScopeAimPoint: 조준경 전방 기준점.
조준경과 본체는 함께 상하 조준합니다. 조준경은 구조 메시이며 확대 화면 기능 없음.
피벗과 BoxCollider 포함. 발사/타깃 탐색/반동 재생/이펙트/스탯은 미포함.

Models/PrecisionLevel1.obj/.mtl: 정적 교환용 모델.
색상 머티리얼 사용. 청록 표시등/렌즈는 발광 머티리얼.
Preview.png는 실제 메시로 만든 소프트웨어 렌더.
메시 인덱스/면적/이름 확인. Unity 컴파일/플레이 실행 미검증.
Python 생성/렌더 소스 포함 (numpy/Pillow).
