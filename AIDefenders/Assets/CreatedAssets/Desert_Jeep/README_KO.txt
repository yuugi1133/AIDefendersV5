AI Defenders — Desert Utility Jeep

첨부 사진을 참고한 모래색 4도어 군용 지프의 로우폴리 모델입니다.
검은 소프트톱, 원형 전조등, 수평 그릴, 도어 별 표식, 휠 아치, 후면 스페어타이어를 반영했습니다.
뒷모습은 사진에 보이지 않아 보완 제작했습니다.
실사 위장무늬·손상·실내는 재현하지 않았고, 유리는 불투명한 어두운 색입니다.

[Unity 적용]
1. Assets/AIDefendersJeep 폴더를 프로젝트 Assets에 복사.
2. 컴파일이 끝나면 Tools > AI Defenders > Create Desert Jeep 선택.
3. Assets/AIDefendersJeepGenerated/DesertJeep.prefab을 씬에 배치.
재실행하면 새 폴더를 생성하여 기존 결과를 보존합니다.
기존 포탑/탱크와 파일명 및 클래스명이 달라 함께 사용할 수 있습니다.
Built-in/URP용이며 HDRP는 별도 머티리얼 설정이 필요합니다.

[계층]
DesertJeep: 차량 이동 루트
  Body: 차체, 문, 지붕, 고정 스페어타이어
  Steer_FL: 앞 왼쪽 조향 Y축
    Wheel_FL: 바퀴 회전 X축
  Steer_FR: 앞 오른쪽 조향 Y축
    Wheel_FR: 바퀴 회전 X축
  Wheel_RL: 뒤 왼쪽 바퀴 회전 X축
  Wheel_RR: 뒤 오른쪽 바퀴 회전 X축

모든 바퀴는 같은 로컬 X축 방향을 사용합니다.
각 바퀴의 회전축은 바퀴 중심에 맞췄습니다.
좌우는 +Z 전방을 바라보는 차량 기준입니다.
차량 이동·바퀴 회전·조향 애니메이션·WheelCollider·Rigidbody는 포함하지 않습니다.
Body의 콜라이더는 차체/캐빈을 대략 감싸는 박스이며 바퀴 물리 설정은 별도로 추가하세요.
문과 지붕은 고정된 차체 메시이며 개폐 기능이 없습니다.

[크기]
+Z 전방 / +Y 위쪽 / 바퀴 최하단 Y=0.
전체 폭 약 2.465, 길이 약 4.218, 높이 2.225유닛.
기존 모델들과 함께 쓰기 위한 게임용 크기이며 실물의 정확한 축척은 아닙니다.
씬에서 Transform Scale로 조절할 수 있습니다.

[파일]
Models/Jeep.obj + Jeep.mtl: 부품별 이름이 있는 범용 모델. 같은 폴더에 유지하세요.
Assets/: 메시 데이터와 Unity 프리팹 생성 도구
Preview.png: 실제 메시 앞/뒤 미리보기
build_model.py: 재생성 소스 (numpy)
render_preview.py: 미리보기 소스 (numpy/Pillow/DejaVu Sans 폰트)

원본 부품 238개, 삼각형 8032개.
Unity 생성 시 같은 동작 그룹 안의 같은 색 부품을 묶어 렌더러 수를 줄입니다.
UV, 텍스처, FBX, 실내 모델, 리깅/애니메이션은 포함하지 않습니다.
OBJ에는 회전축 계층이 없으므로 Unity에서는 제공된 생성 도구를 권장합니다.

[검증]
메시 면적, 정점 인덱스, 바퀴 중심과 지면 높이, 앞/뒤 미리보기를 확인했습니다.
이 환경에 Unity Editor가 없어 Unity 내부 컴파일·실행 검증은 하지 못했습니다.
