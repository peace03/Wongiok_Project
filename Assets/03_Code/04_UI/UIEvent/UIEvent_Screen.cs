using UnityEngine;
using UnityEngine.Video;

// 타이틀 화면의 저장 파일 존재 여부 갱신 이벤트
public struct UISetTitleSaveStateEvent
{
    public bool HasSaveFile { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UISetTitleSaveStateEvent(bool hasSaveFile)
    {
        HasSaveFile = hasSaveFile;
    }
}

// 타이틀 화면에서 새 게임 시작 요청 시 발행 이벤트
public struct UITitleNewGameRequestedEvent
{

}

// 타이틀 화면에서 이어하기 요청 시 발행 이벤트
public struct UITitleContinueRequestedEvent
{

}

// 타이틀 화면에서 게임 종료 요청 시 발행 이벤트
public struct UITitleExitRequestedEvent
{

}

// 챕터 선택 화면에서 챕터 입장 요청 시 발행 이벤트
// 실제 챕터 로딩, 저장 처리, 썸네일 화면 이동 x
public struct UIChapterEnterRequestedEvent
{
    public int ChapterId { get; private set; }
    public Sprite Thumbnail { get; private set; }
    public Sprite Background { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIChapterEnterRequestedEvent(int chapterId, Sprite thumbnail, Sprite background)
    {
        ChapterId = chapterId;
        Thumbnail = thumbnail;
        Background = background;
    }
}

// 챕터 선택 화면에서 뒤로가기 시 발행 이벤트
public struct UIChapterBackRequestedEvent
{

}

// 챕터 카드 화면에 표시할 데이터 전달 전용 이벤트
public struct UISetChapterTitleCardEvent
{
    public int ChapterId { get; private set; }
    public Sprite Thumbnail { get; private set; }
    public Sprite Background { get; private set; }
    public VideoClip LoadingVideoClip { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UISetChapterTitleCardEvent(
        int chapterId,
        Sprite thumbnail,
        Sprite background,
        VideoClip loadingVideoClip)
    {
        ChapterId = chapterId;
        Thumbnail = thumbnail;
        Background = background;
        LoadingVideoClip = loadingVideoClip;
    }
}

// 챕터 화면에서 다음 진행을 요청 시 발행 이벤트
public struct UIChapterTitleCardContinueRequestedEvent
{
    public int ChapterId { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIChapterTitleCardContinueRequestedEvent(int chapterId)
    {
        ChapterId = chapterId;
    }
}

// 챕터 타이틀 카드 화면에서 입력으로 진행을 요청하는 이벤트
public struct UIChapterTitleCardInputContinueRequestedEvent
{

}

public struct UIChapterTitleCardLoadingReadyEvent
{
    public int ChapterId { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIChapterTitleCardLoadingReadyEvent(int chapterId)
    {
        ChapterId = chapterId;
    }
}

public struct UIChapterTitleCardInputSkipRequestedEvent
{
}

public struct UIChapterTitleCardActivateSceneRequestedEvent
{
    public int ChapterId { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIChapterTitleCardActivateSceneRequestedEvent(int chapterId)
    {
        ChapterId = chapterId;
    }
}

// 챕터 타이틀 카드 화면에서 뒤로가기 진행을 요청하는 이벤트
public struct UIChapterTitleCardBackRequestedEvent
{

}

// 챕터 리스트 드래그 상태 유지
public readonly struct UIChapterSelectScrollPreserveRequestedEvent
{
}

// UI 화면 변경 이벤트입니다.
// 외부 시스템이 타이틀, 로딩, 인게임 같은 기본 Screen 전환을 요청할 때 발행합니다.
public struct UIChangeScreenEvent
{
    // 전환할 목표 Screen 상태입니다.
    public UIScreenState ScreenState { get; private set; }

    // 이벤트 발행 시 목표 Screen 상태를 함께 넘깁니다.
    public UIChangeScreenEvent(UIScreenState screenState)
    {
        ScreenState = screenState;
    }
}

// 게임 오버 화면에 필요한 상태 전달 이벤트
// 체크포인트에서 재시작 가능한지만 확인
public struct UISetGameOverEvent
{
    // 2026.08.07_psb수정
    public bool HasCheckpoint { get; private set; }
    public bool HasRemainingLife { get; private set; }

    public bool CanLoadCheckpoint =>
        HasCheckpoint && HasRemainingLife;

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UISetGameOverEvent(
        bool hasCheckpoint,
        bool hasRemainingLife)
    {
        HasCheckpoint = hasCheckpoint;
        HasRemainingLife = hasRemainingLife;
    }
}

// 게임 오버 화면에서 현재 챕터 재시작 시 요청 이벤트
public struct UIGameOverRestartChapterRequestedEvent
{

}

// 게임 오버 화면에서 마지막 체크포인트부터 재시작 시 요청 이벤트
public struct UIGameOverLoadCheckpointRequestedEvent
{

}

// 게임 오버 화면에서 메인 화면으로 갈 때 요청 이벤트
public struct UIGameOverMainMenuRequestedEvent
{

}

// 챕터 클리어 화면에 필요한 상태 전달
public struct UISetChapterClearEvent
{
    public bool HasNextChapter { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UISetChapterClearEvent(bool hasNextChapter)
    {
        HasNextChapter = hasNextChapter;
    }
}

// 챕터 클리어 화면에서 다음 챕터 진행 요청
public struct UIChapterClearNextRequestedEvent
{

}

// 챕터 클리어 화면에서 메인 메뉴 이동 요청
public struct UIChapterClearMainMenuRequestedEvent
{

}

// 챕터 클리어 화면에서 게임 종료 요청
public struct UIChapterClearQuitGameRequestedEvent
{

}

// 임시 테스트 용으로 추가
public struct UISetChapterProgressEvent
{
    public int HighestClearedChapterId { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UISetChapterProgressEvent(int highestClearedChapterId)
    {
        HighestClearedChapterId = highestClearedChapterId;
    }
}
