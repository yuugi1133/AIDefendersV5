기관총 3단계 — AI Defenders

첨부 이미지 기반: 가로 3개 개틀링 포드, 포드당 6개 원형 총열 (총 18개),
금색 전면 테두리/상부 포인트/외측 장갑, X자 지지대와 청록색 띠.
1·2단계와 같은 높이 약 2.1, 같은 좌표 기준 (+Y 위, +Z 발사).
모델은 새 기관총 계열입니다. 기존 정밀 사격 계열의 파일은 수정하지 않습니다.

Unity 설치
Assets/AIDefendersMachineGunLevel3 폴더를 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Machine Gun Level 3 실행.
Assets/AIDefendersMachineGunLevel3Generated/MachineGunLevel3.prefab 생성.
재실행 시 별도 폴더 생성. Built-in/URP용, HDRP 별도 설정 필요.

Base: X자 고정 받침 / YawPivot: Y축 조준 / PitchPivot: 높이 1.55, X축 조준.
RecoilLeft/Center/Right: 3개 총열 묶음, 각 로컬 Z축으로 반동 이동 가능.
각 묶음 아래 MuzzlePoint1~6 발사 위치. 총 18개.
모델, 피벗, BoxCollider만 제공. 총열 회전, 반동 재생, 공격 로직,
대상 탐색 및 발사 이펙트는 미포함. 게임 코드에서 연결하세요.

Models/MachineGunLevel3.obj/.mtl: 정적 모델, Unity 계층은 생성기로 구성.
색상 머티리얼, 별도 텍스처 없음. 청록색 띠는 발광 머티리얼.
Preview.png는 실제 메시 소프트웨어 렌더. Python 소스 포함 (numpy/Pillow).
메시 인덱스/면적/이름 검증. Unity 컴파일/플레이 실행 미검증.
