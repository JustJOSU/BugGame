# BugGame

C#을 활용하여 제작한 BugGame입니다.

프로그램의 소스는 

# 게임 방법

- 머리와 꼬리로 이루어진 Bug(벌레)가 두 마리 나옵니다.
- 머리가 필드위에 존재하는 먹이를 먹으면 몸통이 늘어납니다.
- 만약 길어진 자신의 몸통에 머리가 갇히면 더 이상 움직일 수 없습니다.
- 필드 위에 존재하는 먹이를 다 먹어치워 없어지게 될 경우 두 마리의 Bug중 몸 길이가 가장 긴 Bug가 승리하게 됩니다.

# 문제점

- A라는 Bug가 자신의 몸통에 갇혀버려 움직이지 않게 되었을 경우 B라는 Bug가 A의 머리쪽으로 지나가면
  A는 B가 지나온 경로로 다시 움직일 수 있게 되는 버그가 발생함
