사막색 4륜 장갑차 - 참고 이미지 기반 로우폴리 스타일 재해석

설치: Assets/AIDefendersArmoredCar 폴더를 Unity 프로젝트에 복사합니다.
Tools > AI Defenders > Create Armored Car를 실행합니다.
Assets/AIDefendersArmoredCarGenerated/ArmoredCar.prefab을 씬에 배치합니다.

Y 위, +Z 전방. 바퀴 중심 Y=.51, 차체 길이 약 4.25유닛.
바퀴는 WheelFL/FR/RL/RR의 로컬 X축으로 회전합니다.
앞바퀴 조향은 SteerFL/FR의 로컬 Y축 회전으로 조절합니다.
TurretYaw는 로컬 Y축, GunPitch는 로컬 X축으로 조준합니다.
MuzzlePoint에 발사 이펙트를 연결할 수 있습니다.

운전 AI/물리/사격/피해량/바퀴 애니메이션 코드는 포함하지 않습니다.
차체 박스 콜라이더만 포함됩니다. WheelCollider/리지드바디는 용도에 맞게 설정하세요.
Built-in과 URP 지원. HDRP 재질은 별도 설정이 필요합니다.
참고 사진의 실사 텍스처/완전한 디테일을 복제한 모델은 아닙니다.
Unity 에디터에서 컴파일/실행은 미검증입니다.
OBJ/MTL, 메시 생성 에디터, Python 소스와 미리보기가 포함됩니다.
Python 재생성은 numpy/Pillow 필요. Unity 사용은 Python 불필요.
