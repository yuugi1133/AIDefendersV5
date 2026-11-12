레이저 터렛 2단계 - 첨부 이미지 기반 로우폴리 모델

Assets/AIDefendersLaserLevel2 폴더를 Unity 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Laser Level 2 실행.
Assets/AIDefendersLaserLevel2Generated/LaserLevel2Turret.prefab 생성.
이전 LaserV2 모델과 다른 이름으로 생성되며 기존 모델을 덮어쓰지 않습니다.

높이 2.1유닛, 사각 받침대 폭 약 2.1753유닛.
+Y 위, +Z 전방, 루트는 바닥 중앙.
YawPivot 로컬 Y축으로 회전, PitchPivot 로컬 X축으로 상하 조준.
LaserOrigin은 (0,1.55,1.60)의 초기 월드 좌표에 해당하는 발사 위치.
청록색 포신/캡슐은 발광 재질을 적용한 실제 메시입니다.
레이저 빔/피해량/타깃 추적/공격 스크립트는 포함하지 않습니다.
Bloom 후처리는 프로젝트에서 별도로 설정해야 합니다.
공용 플랫폼의 TurretMount 아래에 넣고 로컬 위치와 회전을 0, 스케일을 1로 설정하세요.

Built-in/URP 재질 지원, HDRP 별도 설정.
단순 박스 콜라이더 포함. 메시/인덱스/피벗 검사 및 프리뷰 확인.
Unity 내부 컴파일/실행 미검증.
OBJ/MTL, Unity 생성기, Python 소스 포함. Python 재생성에 numpy/Pillow 필요.
