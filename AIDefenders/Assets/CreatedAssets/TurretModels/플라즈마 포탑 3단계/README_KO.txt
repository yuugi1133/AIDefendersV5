플라즈마 포탑 3단계 — 최종 재설계판 (첫 번째 버전 계열)

1단계 및 새 2단계와 실루엣 차이를 키운 최종 업그레이드입니다.
4개의 대각 지지대, 대형 팔각 반응로, 삼각형으로 배치된 3갈래 전극 암,
암마다 붙은 축전기와 중앙 대형 에너지 구체로 구성합니다.
단일 사격 코어를 유지합니다. 1·2단계와 색상 및 충전-사격 연출을 계승.
+Y 위 / +Z 발사 / 바닥 원점. 지지대 폭 약2.16, 상부 높이 약2.72.

Unity 적용 (Built-in/URP)
Assets/AIDefendersPlasmaLevel3 폴더 전체를 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Plasma Turret Level 3 실행.
Assets/AIDefendersPlasmaLevel3Generated/PlasmaLevel3.prefab 생성.
기존 생성 프리팹은 자동으로 변경되지 않습니다. 새 프리팹을 사용하세요.
재생성 시 별도 폴더 생성. HDRP는 별도 머티리얼 설정 필요.

사격 모션: C# 스크립트 방식 (Animator/AnimationClip 파일 없음).
Play → Plasma Level 3 Fire Animation → Fire Once로 1회 / Auto Fire로 반복.
충전0.30초 → 섬광/반동0.05초 → 복귀0.40초.
PlayFire() 시작 성공 true, 재생 중/비활성/참조 미설정 false.
On Shot 이벤트를 통해 실제 투사체 생성을 연결하세요.
CancelFire() 또는 컴포넌트 비활성화로 기본 상태 복원.

YawPivot: Y축 조준 / PitchPivot: 높이1.65, X축 조준.
EmitterPivot: 발사부 전체 후퇴/복귀 / ChargeOrbPivot: (0,1.65,1.25).
MuzzlePoint: (0,1.65,1.89). 투사체/피해/타격/소리/실시간 광원 미포함.
Bloom 별도 설정. 발광 재질을 MaterialPropertyBlock으로 제어.
OBJ/MTL은 정적 모델. Unity 모션과 계층은 생성기로 구성.
Preview.png, FirePreview.gif는 실제 메시를 이용한 소프트웨어 렌더.
메시 인덱스/면적/이름 검증. Unity 내부 컴파일/실행 미검증.
Python 생성 및 렌더 소스 포함 (numpy/Pillow).
