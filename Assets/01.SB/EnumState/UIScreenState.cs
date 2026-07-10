// UI의 가장 바닥에 깔리는 "기본 화면" 상태입니다.
// Title, Loading, InGame처럼 서로 동시에 떠 있으면 안 되는 큰 화면 단위를 관리합니다.
public enum UIScreenState
{
    // 아직 어떤 화면도 선택되지 않은 초기 상태입니다.
    None = 0,

    // 프롤로그 컷신 또는 프롤로그 텍스트 화면입니다.
    Prologue,

    // 게임 시작 시 가장 먼저 보여줄 타이틀 화면입니다.
    Title,

    // 플레이할 챕터를 고르는 화면입니다.
    ChapterSelect,

    // 챕터 입장 직전에 챕터 이름이나 표지를 보여주는 화면입니다.
    ChapterTitleCard,

    // 씬 전환, 데이터 로딩 등 사용자의 입력을 막아야 하는 로딩 화면입니다.
    Loading,

    // 실제 플레이가 진행되는 화면입니다.
    // 이 상태에서는 기본적으로 Player HUD가 함께 표시됩니다.
    InGame,

    // 플레이어 사망 후 보여주는 게임 오버 결과 화면입니다.
    GameOver,

    // 챕터 클리어 후 보여주는 결과 화면입니다.
    ChapterClear
}
