기관총 1단계 — AI Defenders

신규 기관총 계열의 1단계 모델입니다.
기존 기관총 포탑을 정밀 사격 계열로 구분한다는 기획에 맞춰 별도 에셋으로 제작.
기존 파일이나 프로젝트의 포탑 명칭/공격 로직은 직접 변경하지 않았습니다.

첨부 참고 이미지 반영: 청남색 각진 하우징, 금색 전면 테두리,
원형 다각 받침대, 청록색 표시등, 삼각 배치의 사각 총열 3개.
기존 포탑과 같은 높이 2.1, 받침 지름 약 2.2, +Y 위 / +Z 발사 방향.
별도 텍스처 없이 색상 머티리얼 사용.

설치
Assets/AIDefendersMachineGunLevel1 폴더를 Unity 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Machine Gun Level 1 실행.
Assets/AIDefendersMachineGunLevel1Generated/MachineGunLevel1.prefab 생성.
재실행 시 새 폴더 생성. 기존 모델들과 다른 파일명/클래스명 사용.
Built-in/URP용. HDRP는 별도 머티리얼 설정 필요.

구조
Base: 고정 받침.
YawPivot: Y축 수평 회전.
PitchPivot: X축 상하 조준, 원점 높이 1.55.
Recoil: 총열 묶음, 로컬 Z축 음수 방향으로 반동 이동 가능.
MuzzlePoint / MuzzlePoint2 / MuzzlePoint3: 각 총열의 발사 위치.
프리팹에는 모델과 피벗 및 BoxCollider만 포함합니다.
발사/대상 탐색/총열 회전/반동 애니메이션/이펙트는 구현하지 않았습니다.

Models/MachineGunLevel1.obj 및 .mtl: 정적 교환용 모델.
Unity 관절 계층은 생성기를 통해 구성하세요.
Preview.png는 실제 메시의 소프트웨어 렌더.
메시 인덱스, 면적 및 파트 이름 검증. Unity 내부 컴파일/실행 미검증.
Python 생성 및 렌더링 소스 포함 (numpy/Pillow).
