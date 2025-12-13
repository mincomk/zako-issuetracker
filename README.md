# zako-issuetracker
Zako IssueTracker ♥ for minco

## plans


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
- `/이슈 내보내기 [태그]`: 태그를 가진 이슈를 파일로 내보냄. 태그 옵션 없으면 전체 다 내보냄. <br> 전송 문자가 2000자 미만이면 코드블록으로 감싼 메세지로 보내고, 2000자 초과인 경우에는 json파일로 보냄

![`/이슈 내보내기`](img/export.png)

## 구현
최대한 간단하게, PostgresSQL이나 Mongo 쓰면 좋을듯?
언어는 뭐 파이썬을 쓰던 go를쓰던 TS하던 알아서 끌리는거<br><br>
귀찮은 부분이라 SQLite 쓰기로 결정
<br> SQLite 만세 ㅌㅌ
![SQLite](https://img1.daumcdn.net/thumb/R1280x0/?scode=mtistory2&fname=https%3A%2F%2Fblog.kakaocdn.net%2Fdna%2FbdKwrt%2Fbtr0qYWMFi1%2FAAAAAAAAAAAAAAAAAAAAAE2y4AQDhL4Vfn9ZGBV9iCzC6NndkWRFn_ZvujRNQDhw%2Fimg.png%3Fcredential%3DyqXZFxpELC7KVnFOS48ylbz2pIh7yKj8%26expires%3D1767193199%26allow_ip%3D%26allow_referer%3D%26signature%3DHqIKe%252FRZHBZouLYGEf2jRJAVaLA%253D)
