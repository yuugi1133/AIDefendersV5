플라즈마 포탑 1단계 — AI Defenders

기존 남색/금색 로우폴리 포탑 계열과 맞춘 신규 에너지 구체 발사 포탑.
중앙 발사 링, 보라색 충전 코일, 3개 전극, 양옆 축전기와 독립 충전 구체.
받침 폭 약2.175, +Y 위 / +Z 발사 / 바닥 원점.

Unity 설치
Assets/AIDefendersPlasmaLevel1 폴더를 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Plasma Turret Level 1 실행.
Assets/AIDefendersPlasmaLevel1Generated/PlasmaLevel1.prefab 생성.
재실행 시 별도 폴더 생성. Built-in/URP 지원. HDRP 별도 설정 필요.

구조
Base: 고정 받침 / YawPivot: Y축 수평 조준.
PitchPivot: 높이1.53, X축 상하 조준. 발사부 전체가 함께 조준.
MuzzlePoint: 발사 위치 (월드 기본 좌표 0,1.53,1.25).
ChargeOrbPivot: 충전 구체 중심 (기본 좌표 0,1.53,0.94).
이 피벗의 스케일/활성 상태를 제어해 충전 연출을 연결할 수 있습니다.
충전 구체는 포탑 위의 시각적 모델이며 실제 날아가는 투사체가 아닙니다.

모델/피벗/BoxCollider와 스크립트 사격 모션 제공. 투사체 이동/폭발 및 공격 로직 미포함.
빛나는 부품은 Emission 머티리얼이며 Bloom은 프로젝트에서 별도 설정.
실시간 광원은 추가하지 않았습니다.
Models/PlasmaLevel1.obj/.mtl: 정적 교환용 모델. Unity 계층은 생성기 사용.
색상 머티리얼, 텍스처 없음. Preview.png는 실제 메시 소프트웨어 렌더.
Python 소스 포함 (numpy/Pillow).
메시 인덱스/면적/이름 검증. Unity 컴파일/플레이 실행 미검증.

사격 애니메이션 업데이트
- 보라색 발사 링이 있는 첫 번째 버전용입니다. V2/V3용이 아닙니다.
- Assets/AIDefendersPlasmaLevel1 폴더 전체를 복사한 뒤 생성 메뉴를 다시 실행하세요.
  새 프리팹을 사용해야 새 피벗 계층과 사격 컴포넌트가 연결됩니다.
- Play 모드에서 Plasma Fire Animation > Fire Once 체크로 1회 재생.
  Auto Fire 체크로 반복 시연 (기본 간격 1.25초, 실제 자동 사격 AI는 아님).
- 순서: 0.30초 충전 → 0.05초 섬광/후퇴 → 0.40초 복귀/구체 재생성.
- 실제 애니메이션은 C# 스크립트 방식이며 Animator/AnimationClip 파일은 없습니다.
- 코드에서는 GetComponent<PlasmaFireAnimation>().PlayFire() 호출.
  true면 새 모션 시작, 재생 중/비활성/참조 미설정이면 false.
- On Shot 이벤트는 충전이 끝나는 실제 발사 시점에 1회 호출됩니다.
  여기에 MuzzlePoint를 사용하는 투사체 생성 코드를 연결하세요.
- 공격 코드로 제어할 때 Auto Fire를 끄세요. 조준은 YawPivot/PitchPivot에서 제어.
  EmitterPivot과 ChargeOrbPivot은 이 컴포넌트가 제어합니다.
- CancelFire() 또는 컴포넌트 비활성화로 기본 상태 복원.
- 모델에서 구체가 번쩍이며 사라지는 모션만 포함합니다.
  날아가는 탄, 타격, 피해 판정, 소리, 실제 광원은 별도입니다.
- FirePreview.gif는 동일한 모션 공식을 적용한 소프트웨어 렌더이며 Unity 실행 영상이 아닙니다.
- Unity 내부 컴파일/실행 검증은 아직 하지 못했습니다.
