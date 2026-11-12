기관총 2단계 — AI Defenders

신규 기관총 계열 2단계. 첨부 이미지의 좌우 3연장 포드와 사각 받침대를 반영.
1단계와 같은 높이 2.1 / 받침 폭 약 2.175. +Y 위, +Z 발사 방향.
남색/청남색 장갑, 금색 테두리, 청록색 전면 표시등. 총열 6개.

Unity 적용
Assets/AIDefendersMachineGunLevel2 폴더를 Unity 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Machine Gun Level 2 실행.
Assets/AIDefendersMachineGunLevel2Generated/MachineGunLevel2.prefab 생성.
재실행 시 별도 폴더 생성. 1단계 및 이전 MG2와 다른 이름으로 공존 가능.
Built-in/URP용, HDRP 별도 설정 필요.

구조
Base: 고정 사각 받침대.
YawPivot: 로컬 Y축 수평 조준.
PitchPivot: 높이 1.55, 로컬 X축 상하 조준.
RecoilLeft/Right: 좌우 총열 묶음, 각 로컬 Z 음수로 반동 이동 가능.
각 Recoil 아래 MuzzlePoint1/2/3 발사 위치 (총 6개).
모델과 피벗 및 BoxCollider 포함. 반동/발사/총열 회전 동작 및 공격 AI는 미포함.
공격 속도/데미지/업그레이드 비용 등은 게임 로직에서 연결하세요.
기존 포탑을 정밀 사격 포탑으로 바꾸는 프로젝트 내부 작업은 수행하지 않았습니다.

Models/MachineGunLevel2.obj/.mtl: 정적 교환용 메시.
Unity 피벗 계층은 생성기를 통해 구성. 색상 머티리얼 사용, 텍스처 없음.
Preview.png: 실제 메시로 만든 소프트웨어 렌더.
Python 소스 포함 (numpy/Pillow).
메시 인덱스/면적/이름 검증 완료. Unity 컴파일 및 플레이 실행 미검증.
