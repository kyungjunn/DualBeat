# DualBeat

**실시간 1:1 대전 리듬게임** · Unity 6 · Photon PUN2

> Google Antigravity AI Agent 활용 프로젝트

---

## 개요

| 항목 | 내용 |
|---|---|
| 장르 | 리듬 · 실시간 1:1 대전 |
| 엔진 | Unity 6000.3.10f1 (URP) |
| 언어 | C# |
| 네트워크 | Photon PUN2 (Realtime, KR 리전) |
| 음원 | Suno AI 자체 제작 |
| 개발 방식 | Antigravity AI Agent 협업 |

**핵심 컨셉** — 같은 곡 · 같은 채보 · 동시 시작 · 실시간 점수 비교 · 즉시 재대결

---

## 주요 기능

### 네트워크
- 닉네임 설정 → 로비 접속 → 방 목록 조회
- 방 생성 · 이름 지정 · 랜덤 매칭
- 방 목록 실시간 갱신 (호스트명 · 인원 표시)
- 씬 자동 동기화 (`AutomaticallySyncScene`)

### 대기실
- 곡 선택 (Master 전용 · Guest 실시간 반영)
- 레디 / 언레디 토글
- 레디 완료 시에만 시작 버튼 활성화
- 실시간 텍스트 채팅 (RPC 브로드캐스트)
- 게임 시작 시 방 자동 잠금

### 인게임
- 6레인 키 입력 — `Q` `W` `E` / `I` `O` `P`
- 키캡 시각 피드백 (좌 시안 · 우 오렌지)
- 4단계 판정 · 콤보 · 실시간 점수
- 상단 점수 비교 슬라이더 (우세 시각화)
- 3초 카운트다운 후 동시 시작

### 결과
- 승 / 패 / 무 판정
- 양측 최종 점수 대조
- 재대결(Revenge) 상호 동의 → 즉시 재시작
- 대기실 복귀

---

## 아키텍처

### 씬 흐름

```
IntroScene  →  LobbyScene  →  RoomLobbyScene  →  InGameScene
  설정           방 목록          곡 선택            플레이
  볼륨           방 생성          레디               판정
  해상도         랜덤 매칭        채팅               결과 오버레이
```

### 게임플레이 — 단일 책임 분리

`RhythmGameplay` 오케스트레이터가 5개 서브시스템 조합

| 클래스 | 책임 |
|---|---|
| `RhythmClock` | 곡 시간 기준점 · `AudioSettings.dspTime` 기반 |
| `ChartData` | BPM → 초 변환 · 노트 시간순 정렬 |
| `NoteSpawner` | Look-ahead 2초 · 레인별 노트 생성 |
| `NoteView` | 프레임별 위치 갱신 · 시간 → 좌표 |
| `JudgeSystem` | 근접 노트 탐색 · 판정 결정 · 미스 처리 |
| `ScoreManager` | 점수 누적 · 콤보 관리 · UI 반영 |

### 데이터

| 타입 | 역할 |
|---|---|
| `SongData` | ScriptableObject · 곡 메타 + 채보 |
| `NoteInfo` | `beat` + `lane` — BPM 독립 채보 |
| `ChartData` | 런타임 변환본 · `hitTime` 산출 |

**BPM 기반 채보** — 곡 속도 변경 시 채보 재작성 불필요

---

## 기술적 포인트

### 1. 프레임 독립 타이밍

`Time.deltaTime` 누적 대신 오디오 하드웨어 클럭 직접 참조

```csharp
public double SongTime => AudioSettings.dspTime - DspSongStartTime;
```

프레임 드랍 · 가변 프레임레이트 상황에서도 판정 정확도 유지

### 2. 네트워크 동시 시작

```
Master  →  PhotonNetwork.Time + 3.0  →  룸 프로퍼티 기록
Guest   →  OnRoomPropertiesUpdate    →  동일 시각 수신
양측    →  AudioSource.PlayScheduled →  DSP 레벨 동시 재생
```

Photon 서버 시각 기준 · 클라이언트 로컬 시계 편차 무관

### 3. Custom Properties 기반 상태 동기화

RPC 최소화 · 프로퍼티 중심 설계 → 늦게 입장한 클라이언트도 현재 상태 즉시 복원

| 스코프 | 키 | 용도 |
|---|---|---|
| Room | `SelectedSong` | 선택 곡 인덱스 |
| Room | `SongStartTime` | 동시 시작 시각 |
| Room | `MasterName` | 로비 호스트명 노출 |
| Player | `Score` | 실시간 점수 |
| Player | `IsReady` | 대기실 레디 |
| Player | `IsFinished` | 곡 완주 |
| Player | `Revenge` | 재대결 동의 |

### 4. 판정 시스템

| 판정 | 허용 오차 | 점수 | 콤보 |
|---|---|---|---|
| PERFECT | ±30ms | 1,000 | 유지 |
| GREAT | ±70ms | 600 | 유지 |
| GOOD | ±120ms | 300 | 유지 |
| MISS | 초과 | 0 | 초기화 |

동일 레인 최근접 노트 탐색 → 오차 구간 매핑 → 점수 반영 → 네트워크 전파

### 5. 재대결 핸드셰이크

```
양측 Revenge 요청  →  Master 검증  →  PhotonNetwork.LoadLevel  →  전원 재시작
```

방 유지 · 재매칭 불필요 · 로딩 최소화

---

## 프로젝트 구조

```
Assets/
├─ Scripts/
│  ├─ Gameplay/          # 리듬 게임 코어
│  │  ├─ RhythmGameplay.cs    # 오케스트레이터
│  │  ├─ RhythmClock.cs       # DSP 클럭
│  │  ├─ NoteSpawner.cs       # 노트 생성
│  │  ├─ NoteView.cs          # 노트 이동
│  │  ├─ JudgeSystem.cs       # 판정
│  │  ├─ ScoreManager.cs      # 점수 · 콤보
│  │  ├─ ChartData.cs         # 채보 변환
│  │  ├─ SongData.cs          # 곡 데이터 (SO)
│  │  └─ GameSyncManager.cs   # 인게임 네트워크 동기화
│  ├─ Network/
│  │  └─ NetworkManager.cs    # 연결 · 방 생성 · 입퇴장
│  ├─ UI/
│  │  ├─ IntroManager.cs      # 타이틀 · 설정
│  │  ├─ LobbyManager.cs      # 방 목록 · 매칭
│  │  ├─ RoomListItem.cs      # 방 항목
│  │  ├─ RoomLobbyManager.cs  # 대기실 · 곡선택 · 채팅
│  │  └─ ResultManager.cs     # 결과 · 재대결
│  └─ Editor/
│     └─ UIBuilder.cs         # 4개 씬 UI 자동 생성 툴
├─ Scenes/                # Intro · Lobby · RoomLobby · InGame
├─ Prefabs/               # 노트 · 방 항목 · 곡 항목
└─ Song/                  # 오디오 + 채보 에셋
```

---

## AI 활용 워크플로

| 도구 | 담당 |
|---|---|
| Google Antigravity AI Agent | 설계 · 구현 · 에디터 자동화 |
| Suno AI | 게임 수록 음원 제작 |
| Unity MCP | 에디터 직접 제어 연동 |

### Antigravity — 개발

| 영역 | 활용 |
|---|---|
| 설계 | 씬 흐름 · 클래스 책임 분리 도출 |
| 구현 | 게임플레이 · 네트워크 로직 작성 |
| 자동화 | `UIBuilder` — 에디터 메뉴 클릭 → 4개 씬 UI 계층 일괄 생성 |
| 연동 | Unity MCP 서버 — 에디터 직접 제어 |
| 리팩터링 | 단일 클래스 → 6개 서브시스템 분리 |

**UIBuilder 자동화**

```
Rhythm Game ▸ Build UI Hierarchies
   ↓
Canvas · EventSystem · 패널 · 버튼 · 텍스트 · 프리팹 · 참조 바인딩
   ↓
4개 씬 저장 완료
```

수작업 UI 배치 제거 · 씬 구성 재현성 확보

### Suno — 음원

게임 수록곡 **Suno AI로 직접 제작**

| 항목 | 내용 |
|---|---|
| 제작 | 프롬프트 기반 곡 생성 |
| 장점 | 저작권 자유 · 템포 의도대로 조절 · 곡 추가 비용 최소 |
| 활용 | BPM 확정 → beat 채보 작성 → `SongData` 등록 |

---

## 실행

```
1. Unity 6000.3.10f1 이상으로 프로젝트 열기
2. Photon AppId 등록  →  Window ▸ Photon Unity Networking ▸ Highlight Server Settings
3. IntroScene 열기  →  Play
4. 빌드 실행 + 에디터 실행  →  2인 대전 테스트
```

> Multiplayer Play Mode 패키지로 단일 PC 다중 인스턴스 테스트 지원

---

## 수록곡

**Suno AI 자체 제작 음원** — 외부 저작권 부담 없음 · 게임 템포에 맞춘 곡 직접 생성

| 곡 | BPM | 채보 | 음원 |
|---|---|---|---|
| Electric Static | 130 | 6레인 | Suno AI 제작 |

**음원 → 채보 파이프라인**

```
Suno AI 곡 생성  →  BPM 측정  →  beat 단위 채보 작성  →  SongData 에셋 등록
```

BPM 기준 채보 → 곡 교체 시 동일 워크플로 재사용
