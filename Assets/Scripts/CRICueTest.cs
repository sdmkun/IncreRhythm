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

    [Header("Rhythm Game Settings")]
    [Tooltip("ループ1周あたりの拍数（例: 4/4拍子で32小節なら = 128）")]
    [SerializeField] private int loopLengthInBeats = 128; 
    
    [Tooltip("ノートが判定ラインに到達するまでの拍数（スクロール速度に相当）")]
    [SerializeField] private float appearTimeInBeats = 4.0f;

    [Header("Positions")]
    [SerializeField] private Vector3 noteSpawnPosition = new Vector3(0, 10, 0);
    [SerializeField] private Vector3 noteTargetPosition = new Vector3(0, -4, 0);
    [SerializeField] private float noteDestroyY = -5.0f;

    // 内部変数
    private CriAtomExPlayer player;
    private CriAtomExPlayback playback;
    private List<NoteController> activeNotes = new List<NoteController>();

    // ビート管理用
    private float lastLoopBeat = -1;
    private float accumulatedBeats = 0; // ループ回数分を含んだ通算ビート数
    private int lastSpawnedBeat = -1;   // 重複生成防止用

    void Start()
    {
        // コンポーネント取得の保険
        if (atomSource == null) atomSource = GetComponent<CriAtomSource>();
        if (notePoolManager == null) notePoolManager = FindFirstObjectByType<NotePoolManager>();

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

        // --- 1. ビート情報の取得と計算 ---
        playback.GetBeatSyncInfo(out CriAtomExBeatSync.Info info);

        Debug.Log($"BPM: {info.bpm}, BarCount: {info.barCount}, BeatCount: {info.beatCount}, BeatProgress: {info.beatProgress}");

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
        float totalLinearBeat = accumulatedBeats + currentLoopBeat;


        // --- 3. ノート生成ロジック（テスト用：毎拍生成） ---
        // 「今」より appearTimeInBeats(4拍) 先の未来にノートを置く
        int targetBeatIndex = Mathf.FloorToInt(totalLinearBeat + appearTimeInBeats);

        // まだその拍のノートを作っていなければ生成
        if (targetBeatIndex > lastSpawnedBeat)
        {
            SpawnNote(targetBeatIndex);
            lastSpawnedBeat = targetBeatIndex;
        }


        // --- 4. 全ノートの座標更新 ---
        UpdateAllNotes(totalLinearBeat);


        // --- 5. 画面外のノート回収 ---
        DestroyPassedNotes();
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