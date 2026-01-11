using UnityEngine;
using CriWare;
using System.Collections.Generic;

public class CRICueTest : MonoBehaviour
{
    [Header("CRI Settings")]
    [SerializeField] private CriAtomSource atomSource;
    [SerializeField] private string cueName = "Stage1_Cue"; // 再生するキュー名

    [Header("Pool Settings")]
    [SerializeField] private NotePoolManager notePoolManager;

    [Header("Gameplay Systems")]
    [SerializeField] private JudgmentSystem judgmentSystem;
    [SerializeField] private GrooveGaugeManager grooveGaugeManager;
    [SerializeField] private GameplayUI gameplayUI;

    [Header("Rhythm Game Settings")]
    [Tooltip("ループ1周あたりの拍数（例: 4/4拍子で32小節なら = 128）")]
    [SerializeField] private int loopLengthInBeats = 128; 
    
    [Tooltip("ノートが判定ラインに到達するまでの拍数（スクロール速度に相当）")]
    [SerializeField] private float appearTimeInBeats = 4.0f;

    [Header("Positions")]
    [SerializeField] private Vector3 noteSpawnPosition = new Vector3(0, 10, 0);
    [SerializeField] private Vector3 noteTargetPosition = new Vector3(0, -4, 0);
    [SerializeField] private float noteDestroyY = -5.0f;

    [Header("Aisac")]
    [SerializeField] private float[] aisacValues = { 1.0f, 0.0f };

    // 内部変数
    private CriAtomExPlayer player;
    private CriAtomExPlayback playback;
    private List<NoteController> activeNotes = new List<NoteController>();

    // ビート管理用
    private float lastLoopBeat = -1;
    private float accumulatedBeats = 0; // ループ回数分を含んだ通算ビート数
    private int lastSpawnedBeat = -1;   // 重複生成防止用
    private float currentTotalBeat = 0; // Update内で共有する現在の通算ビート

    // ブロック切り替え用
    private bool hasTriggeredBlockSwitch = false; // ブロック切り替え済みフラグ

    void Start()
    {
        // コンポーネント取得の保険
        if (atomSource == null) atomSource = GetComponent<CriAtomSource>();
        if (notePoolManager == null) notePoolManager = FindFirstObjectByType<NotePoolManager>();
        if (judgmentSystem == null) judgmentSystem = GetComponent<JudgmentSystem>();
        if (grooveGaugeManager == null) grooveGaugeManager = GetComponent<GrooveGaugeManager>();
        if (gameplayUI == null) gameplayUI = FindFirstObjectByType<GameplayUI>();

        // プレイヤーの初期化（音声同期タイマ有効化）
        player = new CriAtomExPlayer(true);

        PlayCue();
    }

    void PlayCue()
    {
        if (atomSource == null || player == null) return;

        CriAtomExAcb acb = CriAtom.GetAcb(atomSource.cueSheet);
        if (acb == null)
        {
            Debug.LogError($"ACB data not found: {atomSource.cueSheet}");
            return;
        }

        player.SetCue(acb, cueName);
        playback = player.Start();

        Debug.Log($"Playback started: {cueName}");
    }

    void Update()
    {
        // 再生中でなければ何もしない
        if (playback.id == CriAtomExPlayback.invalidId || playback.GetStatus() != CriAtomExPlayback.Status.Playing)
        {
            return;
        }

        // --- 0. 入力処理 ---
        HandleInput();

        // --- 1. ビート情報の取得と計算 ---
        playback.GetBeatSyncInfo(out CriAtomExBeatSync.Info info);

        // Debug.Log($"BPM: {info.bpm}, BarCount: {info.barCount}, BeatCount: {info.beatCount}, BeatProgress: {info.beatProgress}");

        // まだビート情報が来ていない（再生直後など）場合はスキップ
        if (info.bpm <= 0) return;

        // 現在のループ内でのビート位置を計算
        float currentLoopBeat = (info.barCount) * info.numBeats 
                                + info.beatCount 
                                + info.beatProgress;

        // --- 2. ループ検出と通算ビートの更新 ---
        // 前回より値が大きく減っていたらループしたとみなす
        // (許容誤差として -1.0f くらい見ておくと安全)
        if (lastLoopBeat != -1 && currentLoopBeat < lastLoopBeat - 1.0f)
        {
            accumulatedBeats += loopLengthInBeats;
            Debug.Log($"<color=cyan>Loop Detected!</color> Accumulated: {accumulatedBeats}");
        }
        lastLoopBeat = currentLoopBeat;

        // ゲーム全体での「通算ビート数」
        currentTotalBeat = accumulatedBeats + currentLoopBeat;


        // --- 3. ノート生成ロジック（テスト用：毎拍生成） ---
        // 「今」より appearTimeInBeats(4拍) 先の未来にノートを置く
        int targetBeatIndex = Mathf.FloorToInt(currentTotalBeat + appearTimeInBeats);

        // まだその拍のノートを作っていなければ生成
        if (targetBeatIndex > lastSpawnedBeat)
        {
            SpawnNote(targetBeatIndex);
            lastSpawnedBeat = targetBeatIndex;
        }


        // --- 4. 全ノートの座標更新 ---
        UpdateAllNotes(currentTotalBeat);


        // --- 5. 画面外のノート回収とミス判定 ---
        CheckMissedNotes();
        DestroyPassedNotes();


        // --- 6. AISAC更新（グルーブゲージに基づく） ---
        UpdateAisacByGrooveGauge();


        // --- 7. グルーブゲージがマックスならブロック切り替え ---
        CheckAndSwitchBlock();

        player.UpdateAll();
    }

    /// <summary>
    /// 指定された通算ビート(targetBeat)に着弾するノートを生成
    /// </summary>
    void SpawnNote(float targetBeat)
    {
        if (notePoolManager == null) return;

        var note = notePoolManager.SpawnNote(noteSpawnPosition, 0);

        // NoteControllerに「目標ビート」を設定
        // initializeメソッドには「現在の通算ビート」ではなく「目標の通算ビート」を渡すのがポイント
        note.Initialize(
            noteSpawnPosition,
            noteTargetPosition,
            targetBeat // これが NoteTime になる
        );

        activeNotes.Add(note);
    }

    void UpdateAllNotes(float currentTotalBeat)
    {
        foreach (var note in activeNotes)
        {
            // ノート側で y = TargetY + (TargetBeat - CurrentTotalBeat) * Speed を計算させる
            note.UpdatePositionByBeat(currentTotalBeat);
        }
    }

    /// <summary>
    /// 左クリック入力を処理してノート判定を行う
    /// </summary>
    void HandleInput()
    {
        // 左クリックが押された瞬間
        if (Input.GetMouseButtonDown(0))
        {
            JudgeNearestNote();
        }
    }

    /// <summary>
    /// 現在のビートに最も近いノートを判定
    /// </summary>
    void JudgeNearestNote()
    {
        if (judgmentSystem == null || grooveGaugeManager == null) return;

        NoteController closestNote = null;
        float closestBeatDifference = float.MaxValue;

        // 判定範囲内の最も近いノートを探す
        foreach (var note in activeNotes)
        {
            if (note.IsJudged()) continue; // すでに判定済みのノートはスキップ

            float beatDiff = Mathf.Abs(note.GetTargetBeat() - currentTotalBeat);

            // 判定範囲内かつ最も近いノートを記録
            if (judgmentSystem.IsInJudgmentRange(note.GetTargetBeat(), currentTotalBeat)
                && beatDiff < closestBeatDifference)
            {
                closestNote = note;
                closestBeatDifference = beatDiff;
            }
        }

        // 最も近いノートを判定
        if (closestNote != null)
        {
            JudgmentResult result = judgmentSystem.Judge(closestNote.GetTargetBeat(), currentTotalBeat);
            grooveGaugeManager.UpdateGauge(result);
            closestNote.SetJudged(true);

            // UIに判定結果を表示
            if (gameplayUI != null)
            {
                gameplayUI.ShowJudgment(result);
            }

            // ノートを即座に削除（判定済みノートを残さない）
            closestNote.ReturnToPool();
            activeNotes.Remove(closestNote);
        }
        else
        {
            Debug.Log("<color=gray>No note in judgment range</color>");
        }
    }

    /// <summary>
    /// 判定範囲を過ぎたノートをミス判定
    /// </summary>
    void CheckMissedNotes()
    {
        if (judgmentSystem == null || grooveGaugeManager == null) return;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];

            // すでに判定済みならスキップ
            if (note.IsJudged()) continue;

            // 判定範囲を過ぎていたらミス
            if (judgmentSystem.HasPassedJudgmentRange(note.GetTargetBeat(), currentTotalBeat))
            {
                grooveGaugeManager.UpdateGauge(JudgmentResult.Miss);
                note.SetJudged(true);
                Debug.Log($"<color=red>MISS!</color> Missed note at beat {note.GetTargetBeat():F2}");
            }
        }
    }

    void DestroyPassedNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            if (activeNotes[i].transform.position.y <= noteDestroyY)
            {
                activeNotes[i].ReturnToPool();
                activeNotes.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// グルーブゲージの値に基づいてAISACを更新
    /// </summary>
    void UpdateAisacByGrooveGauge()
    {
        if (grooveGaugeManager == null || player == null) return;

        // ゲージ値を0.0～1.0に正規化
        float normalizedGauge = grooveGaugeManager.GetNormalizedGaugeValue();

        // AISAC[0]にゲージ値を設定（0.0～1.0）
        player.SetAisacControl(0, normalizedGauge);

        // デバッグ用に配列の値も更新（インスペクタで確認できるように）
        if (aisacValues.Length > 0)
        {
            aisacValues[0] = normalizedGauge;
        }
    }

    /// <summary>
    /// グルーブゲージがマックスになったらブロック1に切り替え
    /// </summary>
    void CheckAndSwitchBlock()
    {
        if (grooveGaugeManager == null || playback.id == CriAtomExPlayback.invalidId) return;

        // まだブロック切り替えしていない かつ ゲージが満タン
        if (!hasTriggeredBlockSwitch && grooveGaugeManager.IsGaugeFull())
        {
            playback.SetNextBlockIndex(1);
            hasTriggeredBlockSwitch = true;
            Debug.Log("<color=cyan>★ GROOVE MAX! Switching to Block 1 ★</color>");
        }
    }

    void OnDestroy()
    {
        // 終了処理
        if (player != null)
        {
            player.Stop();
            player.Dispose();
        }

        // 残ったノートをプールへ返却
        foreach (var note in activeNotes)
        {
            if (note != null) note.ReturnToPool();
        }
        activeNotes.Clear();
    }
}