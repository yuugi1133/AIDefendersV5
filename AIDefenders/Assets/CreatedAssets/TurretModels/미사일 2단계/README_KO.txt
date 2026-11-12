미사일 터렛 2단계 - 첨부 이미지 기반 로우폴리 모델

Assets/AIDefendersMissile2 폴더를 Unity 프로젝트 Assets에 복사합니다.
Tools > AI Defenders > Create Missile 2 메뉴를 실행합니다.
Assets/AIDefendersMissile2Generated/Missile2Turret.prefab을 씬에 배치합니다.

높이 2.1, 받침대 폭 2.1753 유닛. +Y 위, +Z 전방, 원점은 바닥 중앙.
YawPivot 로컬 Y축으로 포탑 회전, PitchPivot 로컬 X축으로 발사대 상하 조준.
Missile_L / Missile_R는 각각 분리된 미사일 표시 오브젝트입니다.
로컬 Z축으로 이동시키거나 발사 시 숨긴 후 별도 투사체를 생성할 수 있습니다.
MuzzlePoint_L / MuzzlePoint_R는 고정된 발사 위치이며 미사일과 함께 움직이지 않습니다.
공용 플랫폼 TurretMount 아래에 넣고 로컬 위치/회전 0, 스케일 1로 배치 가능합니다.

발사/유도/피해량/재장전/업그레이드 게임 코드는 포함하지 않습니다.
Built-in/URP 지원, HDRP는 별도 재질 설정 필요. Bloom 자동 설정 없음.
OBJ/MTL, Unity 생성기, 메시 데이터, Python 소스, 미리보기 포함.
Python 소스 재생성은 numpy/Pillow 필요, Unity 사용에는 Python 불필요.
메시/피벗 검증 및 미리보기 확인. Unity 컴파일/실행은 미검증.
