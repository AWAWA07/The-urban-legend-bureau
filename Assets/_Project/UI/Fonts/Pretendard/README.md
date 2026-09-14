# Pretendard

- 버전: v1.3.9
- 출처: https://github.com/orioncactus/pretendard (공식 릴리스 `Pretendard-1.3.9.zip`)
- 라이선스: SIL Open Font License 1.1 (`LICENSE.txt`)
- 원본 이름 유지 (Reserved Font Name: Pretendard)

게임 배포 시 `LICENSE.txt` 를 함께 포함해야 한다.

## 포함 굵기

| 파일 | 용도 |
|---|---|
| Pretendard-Regular.otf  | 본문 / 긴 설명 / 디버그 텍스트 |
| Pretendard-Medium.otf   | 일반 UI / 버튼 / 보조 정보 |
| Pretendard-SemiBold.otf | 섹션 제목 / 강조 |
| Pretendard-Bold.otf     | 메인 제목 / 강한 강조 |

TMP Font Asset 은 같은 폴더의 `*.asset` 파일이며 Dynamic 아틀라스를 사용한다.
Regular 에셋의 Font Weight 테이블에 나머지 3종이 연결되어 있어
`<b>` 태그와 fontWeight 지정이 실제 굵기 파일로 연결된다.
