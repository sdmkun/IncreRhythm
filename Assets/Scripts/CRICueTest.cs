using UnityEngine;
using CriWare;
using System.Collections.Generic;

public class CRICueTest : MonoBehaviour
{
    [Header("CRI Settings")]
    [SerializeField] private CriAtomSource atomSource;
    [SerializeField] private string cueName = "Stage1_Cue"; // 再生するキュー名

    [Header("SE Settings")]
    [SerializeField] private CriAtomSource seAtomSource;  // SE用のAtomSource
    [SerializeField] private string hitSoundPerfectCueName = "Hit_Perfect";  // Perfect用SE
    [SerializeField] private string hitSoundGreatCueName = "Hit_Great";      // Great用SE
    [SerializeField] private string hitSoundGoodCueName = "Hit_Good";        // Good用SE

    [Header("Pool Settings")]
    [SerializeField] private ArcNotePoolManager arcNotePoolManager;

    [Header("Gameplay Systems")]
    [SerializeField] private JudgmentSystem judgmentSystem;
    [SerializeField] private VoltageManager voltageManager;
    [SerializeField] private GameplayUI gameplayUI;
    [SerializeField] private JudgmentLineRing judgmentLineRing;
    [SerializeField] private JudgmentPointController judgmentPoint;
    [SerializeField] private SkillTreeModeController skillTreeModeController;

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
    [SerializeField] private float[] aisacValues = { 0.0f, 0.5f }; // [0]: Voltage, [1]: SkillTree Mode

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

    // スキルツリーモード管理用
    private bool isInSkillTreeMode = false;

    void Start()
    {
        // コンポーネント取得の保険
        if (atomSource == null) atomSource = GetComponent<CriAtomSource>();
        if (arcNotePoolManager == null) arcNotePoolManager = FindFirstObjectByType<ArcNotePoolManager>();
        if (judgmentSystem == null) judgmentSystem = GetComponent<JudgmentSystem>();
        if (voltageManager == null) voltageManager = GetComponent<VoltageManager>();
        if (gameplayUI == null) gameplayUI = FindFirstObjectByType<GameplayUI>();
        if (judgmentLineRing == null) judgmentLineRing = FindFirstObjectByType<JudgmentLineRing>();
        if (judgmentPoint == null) judgmentPoint = FindFirstObjectByType<JudgmentPointController>();
        if (skillTreeModeController == null) skillTreeModeController = FindFirstObjectByType<SkillTreeModeController>();

        // SE用のAtomSourceが未設定の場合、新規に追加
        if (seAtomSource == null)
        {
            // GameObjectに新しいCriAtomSourceコンポーネントを追加
            seAtomSource = gameObject.AddComponent<CriAtomSource>();
            // 同じCueSheetを使用（必要に応じてSE専用のCueSheetに変更可能）
            if (atomSource != null)
            {
                seAtomSource.cueSheet = atomSource.cueSheet;
            }
        }

        // 判定ポイントにリング半径を設定
        if (judgmentPoint != null)
        {
            judgmentPoint.SetRingRadius(judgmentRadius);
        }

        // プレイヤーの初期化（音声同期タイマ有効化）
        player = new CriAtomExPlayer(true);
    
        // TODO: エフェクトバス
        // CriAtom.AttachDspBusSetting("DspBus_Effects");
        // atomSource.SetBusSendLevel("DspBus_Effects", 0);

        // スキルツリーモードのイベントを購読
        if (skillTreeModeController != null)
        {
            skillTreeModeController.OnSkillTreeModeChanged += OnSkillTreeModeChanged;
        }

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

        UpdateAisacByVoltage();

        Debug.Log($"Playback started: {cueName}");
    }

    void Update()
    {
        // 再生中でなければ何もしない
        if (playback.id == CriAtomExPlayback.invalidId || playback.GetStatus() != CriAtomExPlayback.Status.Playing)
        {
            return;
        }

        // --- 0. 自動判定処理 ---
        CheckAutoJudgment();

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

        // スキルツリーモードでなければノート生成
        if (!isInSkillTreeMode && targetBeatIndex > lastSpawnedBeat)
        {
            SpawnNote(targetBeatIndex);
            lastSpawnedBeat = targetBeatIndex;
        }


        // --- 4. 全ノートの座標更新 ---
        UpdateAllNotes(currentTotalBeat);


        // --- 5. 画面外のノート回収とミス判定 ---
        CheckMissedNotes();
        DestroyPassedNotes();


        // --- 6. AISAC更新（Voltageに基づく） ---
        UpdateAisacByVoltage();


        // --- 7. Voltageがマックスならブロック切り替え ---
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
    /// 自動判定処理：ノートがリングに到達したときに判定ポイントと重なっているかチェック
    /// </summary>
    void CheckAutoJudgment()
    {
        if (judgmentSystem == null || voltageManager == null || judgmentPoint == null) return;

        // 判定ポイントの現在の角度を取得
        float pointAngle = judgmentPoint.GetCurrentAngleDeg();

        // 全ノートをチェック
        for (int i = activeArcNotes.Count - 1; i >= 0; i--)
        {
            var arcNote = activeArcNotes[i];

            // すでに判定済みならスキップ
            if (arcNote.IsJudged()) continue;

            // ノートの現在の半径を取得
            float currentRadius = arcNote.GetCurrentRadius(currentTotalBeat);
            float targetRadius = arcNote.GetTargetRadius();

            // ノートが判定ラインに到達した以降のみ判定
            // currentRadius >= targetRadius （到達済み）
            // かつ currentRadius - targetRadius <= radiusTolerance （通過しすぎていない）
            float radiusTolerance = 0.3f; // 半径の許容範囲（Unity単位）

            if (currentRadius >= targetRadius && currentRadius - targetRadius <= radiusTolerance)
            {
                // 判定ポイントが円弧の範囲内にあるかチェック
                if (IsPointInArcRange(pointAngle, arcNote))
                {
                    // 判定成功！
                    JudgmentResult result = judgmentSystem.Judge(arcNote.GetTargetBeat(), currentTotalBeat);
                    arcNote.SetJudged(true);

                    // UIに判定結果を表示
                    if (gameplayUI != null)
                    {
                        gameplayUI.ShowJudgment(result);
                    }

                    // ヒット効果音を再生
                    PlayHitSound(result);

                    // ヒットエフェクトを再生（判定ポイントの角度で）
                    arcNote.PlayHitEffect(pointAngle, result);

                    // 判定ライン到達時の処理（非表示 + 削除タイミング設定）
                    arcNote.OnReachedJudgmentLine(currentTotalBeat, deleteDelayInBeats);

                    voltageManager.UpdateVoltage(result);

                    float beatDiff = arcNote.GetTargetBeat() - currentTotalBeat;
                    Debug.Log($"<color=lime>AUTO HIT!</color> Angle: {arcNote.GetAngleDeg():F1}°, Point Angle: {pointAngle:F1}°, Beat diff: {beatDiff:F3}, Result: {result}, Radius: {currentRadius:F2}/{targetRadius:F2}");
                }
            }
        }
    }

    /// <summary>
    /// 判定ポイントが円弧の角度範囲内にあるかチェック
    /// </summary>
    bool IsPointInArcRange(float pointAngle, ArcNoteController arcNote)
    {
        float arcStartAngle = arcNote.GetStartAngleDeg();
        float arcEndAngle = arcNote.GetEndAngleDeg();

        // 角度の正規化（0～360度）
        arcStartAngle = NormalizeAngle(arcStartAngle);
        arcEndAngle = NormalizeAngle(arcEndAngle);
        pointAngle = NormalizeAngle(pointAngle);

        // 円弧が0度をまたぐ場合の処理
        if (arcStartAngle > arcEndAngle)
        {
            return pointAngle >= arcStartAngle || pointAngle <= arcEndAngle;
        }
        else
        {
            return pointAngle >= arcStartAngle && pointAngle <= arcEndAngle;
        }
    }

    /// <summary>
    /// 角度を0～360度の範囲に正規化
    /// </summary>
    float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += 360f;
        while (angle >= 360f) angle -= 360f;
        return angle;
    }

    /// <summary>
    /// ヒット時のSEを再生
    /// </summary>
    /// <param name="result">判定結果</param>
    void PlayHitSound(JudgmentResult result)
    {
        if (seAtomSource == null) return;

        string cueName = "";

        // 判定結果に応じてCue名を選択
        switch (result)
        {
            case JudgmentResult.Perfect:
                cueName = hitSoundPerfectCueName;
                break;
            case JudgmentResult.Great:
                cueName = hitSoundGreatCueName;
                break;
            case JudgmentResult.Good:
                cueName = hitSoundGoodCueName;
                break;
            // Bad/Missは音を鳴らさない（または別のSEを設定可能）
        }

        // Cue名が設定されていれば再生
        if (!string.IsNullOrEmpty(cueName))
        {
            seAtomSource.cueName = cueName;
            seAtomSource.Play();
        }
    }

    /// <summary>
    /// 判定範囲を過ぎたノートをミス判定
    /// </summary>
    void CheckMissedNotes()
    {
        if (judgmentSystem == null || voltageManager == null) return;

        // 円弧ノートのミス判定
        for (int i = activeArcNotes.Count - 1; i >= 0; i--)
        {
            var arcNote = activeArcNotes[i];

            // すでに判定済みならスキップ
            if (arcNote.IsJudged()) continue;

            // 判定範囲を過ぎていたらミス
            if (judgmentSystem.HasPassedJudgmentRange(arcNote.GetTargetBeat(), currentTotalBeat))
            {
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
    /// Voltageとスキルツリーモードに基づいてAISACを更新
    /// </summary>
    void UpdateAisacByVoltage()
    {
        if (voltageManager == null || player == null) return;

        // Voltage値を0.0～1.0に正規化
        // float normalizedVoltage = voltageManager.GetNormalizedVoltageValue();

        // AISAC[0]にVoltage値を設定（0.0～1.0）
        // player.SetAisacControl(0, normalizedVoltage);

        // AISAC[1]にスキルツリーモードの値を設定
        if (skillTreeModeController != null)
        {
            float aisac1Value = skillTreeModeController.GetCurrentAisacValue();
            player.SetAisacControl(1, aisac1Value);

            // デバッグ用に配列の値も更新
            if (aisacValues.Length > 1)
            {
                // aisacValues[0] = normalizedVoltage;
                aisacValues[1] = aisac1Value;
            }
        }
        else
        {
            // SkillTreeModeControllerがない場合はデフォルト値
            player.SetAisacControl(1, 0.5f);
            if (aisacValues.Length > 0)
            {
                // aisacValues[0] = normalizedVoltage;
                aisacValues[0] = 1.0f;
            }
            if (aisacValues.Length > 1)
            {
                aisacValues[1] = 0.5f;
            }
        }
    }

    /// <summary>
    /// Voltageがマックスになったらブロック1に切り替え
    /// </summary>
    void CheckAndSwitchBlock()
    {
        if (voltageManager == null || playback.id == CriAtomExPlayback.invalidId) return;

        // まだブロック切り替えしていない かつ Voltageが満タン
        // if (!hasTriggeredBlockSwitch && voltageManager.IsGaugeFull())
        // {
        //     playback.SetNextBlockIndex(1);
        //     hasTriggeredBlockSwitch = true;
        //     Debug.Log("<color=cyan>★ VOLTAGE MAX! Switching to Block 1 ★</color>");
        // }
    }

    /// <summary>
    /// スキルツリーモードの切り替え時に呼ばれる
    /// </summary>
    void OnSkillTreeModeChanged(bool isSkillTreeMode)
    {
        isInSkillTreeMode = isSkillTreeMode;

        if (isSkillTreeMode)
        {
            // スキルツリーモードに入ったら全ノートを削除
            ClearAllNotes();

            // 判定ポイントを非表示
            if (judgmentPoint != null)
            {
                judgmentPoint.SetVisible(false);
            }

            Debug.Log("<color=yellow>Skill Tree Mode: All notes cleared, judgment point hidden</color>");
        }
        else
        {
            // 通常モードに戻ったら判定ポイントを再表示
            if (judgmentPoint != null)
            {
                judgmentPoint.SetVisible(true);
            }

            Debug.Log("<color=yellow>Normal Mode: Judgment point visible</color>");
        }
    }

    /// <summary>
    /// 全てのノートを削除してプールに返却
    /// </summary>
    void ClearAllNotes()
    {
        for (int i = activeArcNotes.Count - 1; i >= 0; i--)
        {
            if (activeArcNotes[i] != null)
            {
                activeArcNotes[i].ReturnToPool();
            }
        }
        activeArcNotes.Clear();
    }

    void OnDestroy()
    {
        // イベントの購読解除
        if (skillTreeModeController != null)
        {
            skillTreeModeController.OnSkillTreeModeChanged -= OnSkillTreeModeChanged;
        }

        // 終了処理
        if (player != null)
        {
            player.Stop();
            player.Dispose();
        }

        // 円弧ノートをプールへ返却
        ClearAllNotes();
    }
}