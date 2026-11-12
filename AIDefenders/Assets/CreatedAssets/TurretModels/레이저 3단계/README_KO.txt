레이저 터렛 3단계 - 첨부 이미지 기반 로우폴리 모델

Assets/AIDefendersLaserLevel3 폴더를 Unity 프로젝트 Assets에 복사.
Tools > AI Defenders > Create Laser Level 3 실행.
Assets/AIDefendersLaserLevel3Generated/LaserLevel3Turret.prefab 생성.

높이 2.1유닛, 4분할 받침대 전체 폭 약 2.1753유닛.
+Y 위, +Z 전방, 루트는 바닥 중앙.
YawPivot 로컬 Y축으로 회전, PitchPivot 로컬 X축으로 상하 조준.
LaserOrigin은 초기 월드 위치 (0,1.55,1.60)에 해당하는 발사 위치.
후방 에너지 셀 3개, 포신, 받침대 발광 띠는 발광 재질이 적용된 메시.
빛 번짐은 프로젝트 Bloom 설정 필요. 실제 레이저 빔은 포함하지 않습니다.
레이저 공격/추적/피해량 코드는 별도 연결 필요.
공용 플랫폼 TurretMount 아래 배치 후 로컬 위치/회전 0, 스케일 1로 설정.

Built-in/URP 지원, HDRP 별도 재질 설정.
받침대 4개와 중앙 연결부에 단순 박스 콜라이더 적용.
OBJ/MTL, Unity 생성기, 메시 데이터, Python 소스, 미리보기 포함.
Python 재생성에 numpy/Pillow 필요. Unity 사용에는 Python 불필요.
메시/인덱스/피벗 검사 및 미리보기 확인. Unity 내부 컴파일/실행 미검증.
