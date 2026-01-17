using UnityEngine;
using CriWare;
using System.Collections.Generic;

public class CRICueTest : MonoBehaviour
{
    [Header("CRI Settings")]
    [SerializeField] private CriAtomSource atomSource;
    [SerializeField] private string cueName = "Stage1_Cue"; // 再生するキュー名

    [Header("Pool Settings")]
    [SerializeField] private ArcNotePoolManager arcNotePoolManager;

    [Header("Gameplay Systems")]
    [SerializeField] private JudgmentSystem judgmentSystem;
    [SerializeField] private GrooveGaugeManager grooveGaugeManager;
    [SerializeField] private GameplayUI gameplayUI;
    [SerializeField] private JudgmentLineRing judgmentLineRing;

    [Header("Rhythm Game Settings")]
    [Tooltip("ループ1周あたりの拍数（例: 4/4拍子で32小節なら = 128）")]
    [SerializeField] private int loopLengthInBeats = 128; 
    
    [Tooltip("ノートが判定ラインに到達するまでの拍数（スクロール速度に相当）")]
    [SerializeField] private float appearTimeInBeats = 4.0f;

    [Header("Arc Note Settings")]
    [Tooltip("判定ラインの半径")]
    [SerializeField] private float judgmentRadius = 5.0f;
    [Tooltip("1ノートあたりの角度変化（時計回り）")]
    [SerializeField] private float angleStepPerNote = 45.0f;
    [Tooltip("判定ライン到達後、削除するまでの拍数")]
    [SerializeField] private float deleteDelayInBeats = 4.0f; // 1小節 = 4拍

    [Header("Aisac")]
    [SerializeField] private float[] aisacValues = { 1.0f, 0.0f };

    // 内部変数
    private CriAtomExPlayer player;
    private CriAtomExPlayback playback;
    private List<ArcNoteController> activeArcNotes = new List<ArcNoteController>();

    // ビート管理用
    private float lastLoopBeat = -1;
    private float accumulatedBeats = 0; // ループ回数分を含んだ通算ビート数
    private int lastSpawnedBeat = -1;   // 重複生成防止用
    private float currentTotalBeat = 0; // Update内で共有する現在の通算ビート

    // 角度管理用
    private float currentAngle = 0.0f;  // 現在の生成角度（時計回りで増加）

    // ブロック切り替え用
    private bool hasTriggeredBlockSwitch = false; // ブロック切り替え済みフラグ

    void Start()
    {
        // コンポーネント取得の保険
        if (atomSource == null) atomSource = GetComponent<CriAtomSource>();
        if (arcNotePoolManager == null) arcNotePoolManager = FindFirstObjectByType<ArcNotePoolManager>();
        if (judgmentSystem == null) judgmentSystem = GetComponent<JudgmentSystem>();
        if (grooveGaugeManager == null) grooveGaugeManager = GetComponent<GrooveGaugeManager>();
        if (gameplayUI == null) gameplayUI = FindFirstObjectByType<GameplayUI>();
        if (judgmentLineRing == null) judgmentLineRing = FindFirstObjectByType<JudgmentLineRing>();

        // プレイヤーの初期化（音声同期タイマ有効化）
        player = new CriAtomExPlayer(true);
    
        // TODO: エフェクトバス
        // CriAtom.AttachDspBusSetting("DspBus_Effects");
        // atomSource.SetBusSendLevel("DspBus_Effects", 0);

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
        if (arcNotePoolManager == null) return;

        // 1つのノートを現在の角度で生成
        var arcNote = arcNotePoolManager.SpawnArcNote(
            targetBeat,
            appearTimeInBeats,
            currentAngle,
            judgmentRadius
        );

        activeArcNotes.Add(arcNote);

        // 次の角度へ進める（時計回り = マイナス方向）
        currentAngle -= angleStepPerNote;

        // 360度でループ（0～359.99...の範囲に保つ）
        if (currentAngle < 0)
        {
            currentAngle += 360f;
        }
    }

    void UpdateAllNotes(float currentTotalBeat)
    {
        // 円弧ノート更新
        foreach (var arcNote in activeArcNotes)
        {
            arcNote.UpdatePositionByBeat(currentTotalBeat);
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
    /// 円弧ノートの判定（マウス位置に基づく角度判定）
    /// </summary>
    void JudgeNearestNote()
    {
        // マウスのワールド座標を取得
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // マウスの角度を計算
        float mouseAngle = Mathf.Atan2(mouseWorldPos.y, mouseWorldPos.x) * Mathf.Rad2Deg;
        if (mouseAngle < 0) mouseAngle += 360f; // 0-360度に正規化

        ArcNoteController closestNote = null;
        float closestAngleDiff = float.MaxValue;
        float closestBeatDiff = float.MaxValue;

        // 判定範囲内で、角度とビートが最も近いノートを探す
        foreach (var arcNote in activeArcNotes)
        {
            if (arcNote.IsJudged()) continue;

            float beatDiff = Mathf.Abs(arcNote.GetTargetBeat() - currentTotalBeat);

            // 判定範囲内のノートのみ対象
            if (judgmentSystem.IsInJudgmentRange(arcNote.GetTargetBeat(), currentTotalBeat))
            {
                // 角度差を計算（0-180度の範囲）
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(mouseAngle, arcNote.GetAngleDeg()));

                // 角度判定の許容範囲（ノート間隔の半分 + 余裕）
                float angleThreshold = angleStepPerNote / 2f + 10f;

                if (angleDiff <= angleThreshold)
                {
                    // ビート差が最も小さいものを優先
                    if (beatDiff < closestBeatDiff ||
                        (beatDiff == closestBeatDiff && angleDiff < closestAngleDiff))
                    {
                        closestNote = arcNote;
                        closestAngleDiff = angleDiff;
                        closestBeatDiff = beatDiff;
                    }
                }
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

            // 判定ライン到達時の処理（非表示 + 削除タイミング設定）
            closestNote.OnReachedJudgmentLine(currentTotalBeat, deleteDelayInBeats);

            Debug.Log($"<color=lime>HIT!</color> Angle: {closestNote.GetAngleDeg():F1}°, Beat diff: {closestBeatDiff:F3}");
        }
        else
        {
            Debug.Log("<color=gray>No arc note in judgment range</color>");
        }
    }

    /// <summary>
    /// 判定範囲を過ぎたノートをミス判定
    /// </summary>
    void CheckMissedNotes()
    {
        if (judgmentSystem == null || grooveGaugeManager == null) return;

        // 円弧ノートのミス判定
        for (int i = activeArcNotes.Count - 1; i >= 0; i--)
        {
            var arcNote = activeArcNotes[i];

            // すでに判定済みならスキップ
            if (arcNote.IsJudged()) continue;

            // 判定範囲を過ぎていたらミス
            if (judgmentSystem.HasPassedJudgmentRange(arcNote.GetTargetBeat(), currentTotalBeat))
            {
                grooveGaugeManager.UpdateGauge(JudgmentResult.Miss);
                arcNote.SetJudged(true);

                // ミス時も非表示にして削除タイミングを設定
                arcNote.OnReachedJudgmentLine(currentTotalBeat, deleteDelayInBeats);

                Debug.Log($"<color=red>MISS!</color> Missed arc note at beat {arcNote.GetTargetBeat():F2}, angle {arcNote.GetAngleDeg():F0}°");
            }
        }
    }

    void DestroyPassedNotes()
    {
        // 削除タイミングに達した円弧ノートを削除
        for (int i = activeArcNotes.Count - 1; i >= 0; i--)
        {
            if (activeArcNotes[i].ShouldBeDeleted(currentTotalBeat))
            {
                activeArcNotes[i].ReturnToPool();
                activeArcNotes.RemoveAt(i);
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

        // 円弧ノートをプールへ返却
        foreach (var arcNote in activeArcNotes)
        {
            if (arcNote != null) arcNote.ReturnToPool();
        }
        activeArcNotes.Clear();
    }
}