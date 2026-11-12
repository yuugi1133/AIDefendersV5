정밀사격포탑 3단계 — AI Defenders

단일 총열을 유지하는 최종 업그레이드 모델.
2단계보다 긴 총열, 강화 보호 덮개와 측면 레일, 큰 사각 총구 장치,
대형 조준경 대물부/조절 노브, 두꺼운 측면 장갑과 금색 포인트.
받침 폭 약2.175로 1·2단계와 동일. 높이2.50, 총구 끝 Z=3.22.
+Y 위 / +Z 사격 / 바닥 원점.

Unity 설치
Assets/AIDefendersPrecisionLevel3 폴더를 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Precision Turret Level 3 실행.
Assets/AIDefendersPrecisionLevel3Generated/PrecisionLevel3.prefab 생성.
기존 1·2단계와 별도 에셋. 재실행 시 별도 폴더 생성.
Built-in/URP용. HDRP는 별도 머티리얼 설정 필요.

Base: 고정 받침 / YawPivot: Y축 수평 조준 / PitchPivot: 높이1.4, X축 상하 조준.
Recoil: 총열 묶음 / MuzzlePoint: Z=3.24 / ScopeAimPoint: 조준경 전방 기준점.
조준경과 본체는 함께 조준. 실제 확대 화면 기능은 없습니다.
모델/피벗/BoxCollider 포함. 공격 AI/반동 재생/발사 이펙트/게임 스탯 미포함.
길어진 총열이 주변 건물과 겹치지 않도록 배치 간격을 확인하세요.

Models/PrecisionLevel3.obj/.mtl: 정적 교환용 모델.
Unity 계층은 생성기로 구성. 색상 머티리얼 사용, 별도 텍스처 없음.
Preview.png는 실제 메시 소프트웨어 렌더.
Python 소스 포함 (numpy/Pillow).
메시 인덱스/면적/이름 확인. Unity 내부 컴파일/실행 미검증.
