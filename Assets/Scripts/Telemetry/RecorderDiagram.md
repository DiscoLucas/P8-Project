sequenceDiagram
    participant User
    participant MainMenuUIController as MMUI
    participant PerformanceRecorder as PR
    participant LabRecorderController as LRC
    participant ExternalLabRecorder as ExtLR
    participant GameManager as GM
    participant UnitySceneManager as SceneMgr
    participant BaselineSceneController as BSC

    Note over User, BSC: Initial Setup: User starts Unity App & External LabRecorder (with RCS enabled)

    User->>MMUI: Enters Participant ID & Selects Condition
    User->>MMUI: Clicks Start Button
    MMUI->>MMUI: HandleStartExperiment() -> StartExperimentAsync()
    MMUI->>MMUI: Validate Input
    MMUI->>PR: InitializeParticipantID(participantId, conditionIndex)
    PR->>PR: InitializeLSLStream()
    PR->>PR: Create LSL StreamOutlet (UnityGameEvents)
    PR->>PR: RecordData("SessionStart", ...)
    MMUI->>LRC: ConfigureAndStartRecordingAsync(participantId, sessionNumber, conditionName)
    LRC->>LRC: ConnectAsync() (if not connected)
    LRC->>ExtLR: Establish TCP Connection (Host:Port)
    LRC->>LRC: SendCommandAsync("select all")
    LRC->>ExtLR: Send "select all" command
    LRC->>LRC: SendCommandAsync("filename {...}")
    LRC->>ExtLR: Send "filename {root:...} {template:...} ..." command
    LRC->>LRC: SendCommandAsync("start")
    LRC->>ExtLR: Send "start" command
    ExtLR->>ExtLR: Start recording selected LSL streams (UnityGameEvents, ECG, etc.) to XDF file
    LRC-->>MMUI: return true (started)
    MMUI->>GM: currentCondition = selectedCondition
    MMUI->>GM: LoadBaselineScene()
    GM->>SceneMgr: LoadSceneAsync("BaselineScene")
    SceneMgr-->>GM: Scene Loaded

    Note over GM, BSC: Baseline Scene Starts

    BSC->>GM: StartBaseline() (Needs Implementation)
    GM->>GM: Start BaselineRecordingCoroutine() (Needs Implementation)
    GM->>PR: RecordData("BaselineStart", ...)
    PR->>ExtLR: Push "BaselineStart" marker via LSL
    loop Baseline Duration
        GM->>GM: Wait
    end
    GM->>PR: RecordData("BaselineComplete", ...)
    PR->>ExtLR: Push "BaselineComplete" marker via LSL
    GM->>GM: GetSceneForCondition(currentCondition)
    GM->>SceneMgr: LoadSceneAsync(gameSceneName)
    SceneMgr-->>GM: Scene Loaded

    Note over GM, ExtLR: Game Scene Starts - Gameplay Occurs

    loop Gameplay Loop
        GM/Other Scripts->>PR: RecordData("GameEvent", value1, ...)
        PR->>ExtLR: Push "GameEvent" marker via LSL
    end

    Note over GM, ExtLR: Game Finishes or User Quits

    opt Game Finish
        GM->>GM: endGame()
        GM->>GameMenuUIController: onFinnishGame.Invoke()
        GameMenuUIController->>GameMenuUIController: openEndGameMenu()
        User->>GameMenuUIController: Clicks Restart/Quit
    end

    User->>UnityApp: Quits Application
    UnityApp->>LRC: OnApplicationQuit()
    LRC->>ExtLR: Send "stop" command (synchronously)
    ExtLR->>ExtLR: Stop recording and finalize XDF file
    LRC->>LRC: Disconnect()
    LRC->>ExtLR: Close TCP Connection
