정밀사격포탑 2단계 — AI Defenders

1단계 계승: 단일 사각 총열 유지, 받침 크기 유지.
총열 전방 끝 Z=1.89 → 2.46으로 연장.
총열 보호 덮개와 금색 포인트, 측면 장갑 추가.
조준경 확대, 큰 전방 대물부/금색 렌즈 테두리/상단 조절 노브 추가.
높이 약 2.34, 받침 폭 약 2.175. +Y 위, +Z 전방, 바닥 원점.

Unity 설치 (Built-in/URP)
Assets/AIDefendersPrecisionLevel2 폴더를 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Precision Turret Level 2 실행.
Assets/AIDefendersPrecisionLevel2Generated/PrecisionLevel2.prefab 생성.
재실행 시 새 폴더 생성, 기존 1단계 에셋은 수정하지 않습니다.
HDRP는 별도 머티리얼 설정 필요.

Base: 고정 받침 / YawPivot: Y축 회전 / PitchPivot: 높이1.4, X축 조준.
Recoil: 단일 총열과 보호 덮개 / MuzzlePoint: 발사 위치 Z=2.48.
ScopeAimPoint: 조준경 전방 기준점. 조준경과 본체는 같이 상하 조준.
조준경은 장식 메시이며 실제 확대 화면 기능은 없습니다.
모델/피벗/BoxCollider 포함. 공격 로직/반동 재생/발사 이펙트는 미포함.

Models/PrecisionLevel2.obj/.mtl: 정적 교환용 모델.
Unity 피벗 계층은 생성기로 구성. 색상 머티리얼 사용, 별도 텍스처 없음.
Preview.png는 실제 메시의 소프트웨어 렌더.
Python 생성 및 렌더 코드 포함 (numpy/Pillow).
메시 인덱스/면적/이름 확인. Unity 내부 컴파일/실행 미검증.
