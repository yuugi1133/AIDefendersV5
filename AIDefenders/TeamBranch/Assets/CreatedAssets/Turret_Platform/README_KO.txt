AI Defenders 공용 포탑 설치 플랫폼

1. Assets/AIDefendersPlatform 폴더를 Unity 프로젝트 Assets 폴더에 복사합니다.
2. Tools > AI Defenders > Create Turret Platform을 실행합니다.
3. Assets/AIDefendersPlatformGenerated에 생성된 TurretPlatform.prefab을 배치합니다.
4. 기존 포탑을 TurretMount의 자식으로 넣고 로컬 위치/회전을 0, 스케일을 1로 설정합니다.

크기: 가로/세로 3.2 유닛, 설치 높이 .301 유닛. Y 위, Z 전방.
기존 포탑 받침대 폭 약 2.18 유닛에 맞춘 평평한 설치면입니다.
중앙 십자 표시는 포탑을 설치하면 가려집니다.
TurretMount 위치는 (0,.301,0)입니다. 플랫폼과 포탑 루트 스케일은 1을 권장합니다.
프리뷰 오른쪽은 기존 MG2 업그레이드 모델을 올린 배치 예시이며 포탑은 이 ZIP에 포함하지 않습니다.
Built-in/URP 재질 생성 지원. HDRP는 별도 재질 설정이 필요합니다.
발광 재질은 적용되지만 Bloom 후처리는 자동 설정하지 않습니다.
설치/판매/업그레이드 게임 로직은 포함하지 않습니다.
박스 콜라이더는 모서리를 단순화한 범위입니다.
Unity에서 컴파일/실행은 검증하지 않았습니다. 메시 인덱스·면 방향·퇴화 삼각형은 검사했습니다.
Models에는 OBJ/MTL, Assets에는 Unity 메시 생성 데이터/에디터가 있습니다.
Python 소스 재생성에는 numpy/Pillow가 필요하며 Unity 사용에는 Python이 필요하지 않습니다.
