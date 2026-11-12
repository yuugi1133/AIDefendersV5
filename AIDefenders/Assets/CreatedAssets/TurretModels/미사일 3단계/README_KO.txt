미사일 터렛 3단계 / 8발 발사대

Assets/AIDefendersMissile3 폴더를 Unity 프로젝트 Assets에 복사합니다.
Tools > AI Defenders > Create Missile 3 실행.
Assets/AIDefendersMissile3Generated/Missile3Turret.prefab 생성.

높이 2.1 유닛, 받침대 폭 2.1753 유닛. 1/2단계와 같은 바닥 크기.
상부 발사대는 약 2.69유닛으로 넓어졌습니다. 옆 포탑과 간격을 확보하세요.
+Y 위, +Z 전방. 루트 원점은 바닥 중앙.
YawPivot 로컬 Y축 회전 / PitchPivot 로컬 X축 상하 조준.
Missile_L1~L4, Missile_R1~R4: 개별 미사일 표시 오브젝트.
각 측면에서 1/2는 위, 3/4는 아래. 1/3은 로컬 -X, 2/4는 +X 위치.
MuzzlePoint_L1~L4, MuzzlePoint_R1~R4: 각각 고정된 발사 위치.
미사일을 숨기고 별도 투사체를 생성하거나 로컬 Z축으로 이동할 수 있습니다.
발사/유도/재장전/피해량 코드는 포함하지 않습니다.

Built-in/URP 재질 지원, HDRP는 별도 설정. Bloom 자동 설정 없음.
단순 박스 콜라이더 포함. 발사체 충돌 로직에서 자기 포탑을 제외하세요.
OBJ/MTL, 메시 생성기, Python 소스, 프리뷰 포함.
Python 재생성: numpy/Pillow 필요. Unity 사용은 Python 불필요.
메시와 부품/피벗 데이터 검사 및 프리뷰 확인. Unity 내부 실행은 미검증.
