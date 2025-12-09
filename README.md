# zako-issuetracker
Zako IssueTracker ♥ for minco

## 이슈의 구성 요소
- ID (1부터 쭈르륵 증가)
- 이름
- 태그 (종류 식별용 텍스트: 설정, 기능 등 쓰일 예정)
- 내용
- 생성자
- 상태: proposed, approved, rejected

## 기능
- `/이슈 추가`: 아무나 가능. Modal창 띄워서 새 이슈를 생성

![`/이슈 추가`](/img/new.png)
<br> Modal image<br>
!['이슈추가모달'](/img/Modal.png)

- `/이슈 삭제`: ~~본인 혹은 관리자가 사용 가능. proposed 또는 rejected인 이슈는 본인이 삭제 가능. approved인 이슈는 본인은 못하고 관리자만 가능. 관리자는 언제나 삭제 가능.~~
- `/이슈 상태 <상태: proposed, approved, rejected중 택1>`: 관리자만 사용 가능. 이슈 상태를 바꿈.

![`/이슈 상태`](img/status.png)
- `/이슈 목록 <태그>`: 아무나 사용 가능. 태그를 가진 이슈를 페이지 전환 버튼이 있는 목록으로 보여줌.

![`/이슈 목록`](img/list.png)
- `/이슈 내보내기 [태그]`: 태그를 가진 이슈를 파일로 내보냄. 태그 옵션 없으면 전체 다 내보냄.

![`/이슈 내보내기`](img/export.png)

## 구현
최대한 간단하게, PostgresSQL이나 Mongo 쓰면 좋을듯?
언어는 뭐 파이썬을 쓰던 go를쓰던 TS하던 알아서 끌리는거
