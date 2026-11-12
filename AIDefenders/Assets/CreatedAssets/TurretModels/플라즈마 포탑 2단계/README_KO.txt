플라즈마 포탑 2단계 — 첫 번째 버전 계열

1단계와 실루엣 차이를 키운 2단계 재설계판입니다.
원형 받침 → 모서리를 깎은 사각 받침 (설치 폭은 약2.175로 동일).
원통형 링 발사부 → 길게 갈라진 좌우 전극 암과 4개 분리된 원호 전극.
넓고 낮은 반응로 본체, 상단 좌우 대형 축전기, 중앙의 큰 부유 구체.
기존 1번 버전 계열의 남색/금색/보라색 표현과 사격 모션을 계승합니다.
기존 3단계 파일은 이번 요청에서 변경하지 않았습니다.

Unity 적용 (Built-in/URP)
Assets/AIDefendersPlasmaLevel2 폴더 전체를 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Plasma Turret Level 2 실행.
Assets/AIDefendersPlasmaLevel2Generated/PlasmaLevel2.prefab 생성.
반복 생성 시 새 폴더 생성. 1단계와 별도 파일/클래스명으로 함께 보관 가능.
HDRP는 별도 머티리얼 설정 필요.

사격 모션 포함 (C# 스크립트 방식, Animator/AnimationClip 파일은 없음)
Play → Plasma Level 2 Fire Animation → Fire Once 체크: 1회 시연.
Auto Fire: 반복 시연. 공격 코드 연결 시 Auto Fire는 끄세요.
충전 0.30초 → 섬광/반동 0.05초 → 복귀/구체 재생성 0.40초.
PlayFire()로 시작 (시작 성공 true, 재생 중/비활성 false).
On Shot 이벤트는 충전 완료 때 1회 호출. 실제 투사체 생성을 연결하세요.
CancelFire() 또는 컴포넌트 비활성화로 기본 상태 복원.

구조
YawPivot: Y축 조준 / PitchPivot: 높이1.53, X축 상하 조준.
EmitterPivot: 사격 시 로컬 Z 후퇴/복귀.
ChargeOrbPivot: (0,1.53,1.17), 충전 구체의 스케일/활성 제어.
MuzzlePoint: 기본 발사 위치 (0,1.53,1.64).
실제 날아가는 탄/피해 판정/타격/소리/실시간 광원은 미포함.
Bloom은 프로젝트에서 별도 설정. 발광 재질을 MaterialPropertyBlock으로 제어.

OBJ/MTL은 정적 모델이며 Unity 모션/계층은 생성기를 통해 적용합니다.
Preview.png 및 FirePreview.gif는 실제 메시를 이용한 소프트웨어 렌더입니다.
Unity 내부 컴파일/실행은 미검증. 메시 인덱스/면적/이름 검증 완료.
Python 생성/렌더 코드 포함 (numpy/Pillow).

재설계 적용: 새 Assets 폴더 복사 후 Create Plasma Turret Level 2를 다시 실행하고
새로 생성된 프리팹을 사용하세요. 기존 씬의 프리팹은 자동으로 바뀌지 않습니다.
