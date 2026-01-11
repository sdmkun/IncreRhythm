using UnityEngine;
using CriWare;
using System.Collections.Generic;

public class CRICueTest : MonoBehaviour
{
    private CriAtomSource atomSource;
    private CriAtomExPlayer player;
    private CriAtomExPlayback playback;

    private NotePoolManager notePoolManager;

    // ノートの管理
    private List<NoteController> activeNotes = new List<NoteController>();

    // ノートの設定
    private readonly Vector3 noteSpawnPosition = new Vector3(0, 10, 0);  // 出現位置
    private readonly Vector3 noteTargetPosition = new Vector3(0, -4, 0); // 目標位置（4Beatでここに到達）
    private readonly float noteDestroyY = -5.0f;                         // 削除位置のY座標

    // ビート管理
    private int lastBeatCount = -1;  // 前回のビートカウント
    private float beatDuration = 0f;  // 1ビートの長さ（秒）
    private long lastAudioTime = -1; // 前回のオーディオ時間（ミリ秒）

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        notePoolManager = GameObject.Find("NotePoolManager").GetComponent<NotePoolManager>();

        // CriAtomSourceコンポーネントを取得
        atomSource = GetComponent<CriAtomSource>();
        if (atomSource == null)
        {
            Debug.LogError("CRI Atom Source component not found!");
            return;
        }

        // CriAtomExPlayerを音声同期タイマ利用で作成
        player = new CriAtomExPlayer(true);
        if (player == null)
        {
            Debug.LogError("CriAtomExPlayer not found!");
            return;
        }

        // キューの再生
        PlayCue();
    }

    /// <summary>
    /// CRI Atom Sourceに設定されているキューを再生します
    /// </summary>
    void PlayCue()
    {
        if (atomSource == null || player == null)
        {
            return;
        }

        // ACBデータを取得
        CriAtomExAcb acb = CriAtom.GetAcb(atomSource.cueSheet);
        if (acb == null)
        {
            Debug.LogError($"ACB not found: {atomSource.cueSheet}");
            return;
        }

        // キューをセット
        player.SetCue(acb, atomSource.cueName);

        // 再生を開始
        playback = player.Start();

        Debug.Log($"Playing cue: {atomSource.cueName} from sheet: {atomSource.cueSheet}");
    }

    // Update is called once per frame
    void Update()
    {
        if (playback.id == CriAtomExPlayback.invalidId)
        {
            return;
        }

        // オーディオと同期した時間を取得（ミリ秒）
        long currentAudioTimeMs = playback.GetTimeSyncedWithAudio();
        float currentAudioTime = currentAudioTimeMs / 1000.0f; // 秒に変換

        // ループ検出: 時間が減少したらループしたと判断
        if (lastAudioTime != -1 && currentAudioTimeMs < lastAudioTime)
        {
            Debug.Log($"Loop detected! Time reset from {lastAudioTime}ms to {currentAudioTimeMs}ms");
            // 全てのノートをクリア
            foreach (var note in activeNotes)
            {
                note.ReturnToPool();
            }
            activeNotes.Clear();
            lastBeatCount = -1;
        }
        lastAudioTime = currentAudioTimeMs;

        // ビート同期情報を取得
        playback.GetBeatSyncInfo(out CriAtomExBeatSync.Info info);

        // BPMから1ビートの長さを計算（秒）
        if (info.bpm > 0)
        {
            beatDuration = 60.0f / info.bpm;
        }

        // 現在のビートカウントをチェック
        int currentBeatCount = (int)info.beatCount;

        // 新しいビートに入った時にノートを生成
        if (currentBeatCount != lastBeatCount && lastBeatCount != -1)
        {
            SpawnNote(currentAudioTime, beatDuration);
        }
        lastBeatCount = currentBeatCount;

        // 全てのアクティブなノートの位置を更新
        UpdateAllNotes(currentAudioTime);

        // 削除位置に到達したノートをプールに返却
        DestroyPassedNotes();
    }

    /// <summary>
    /// ノートを生成する
    /// </summary>
    /// <param name="currentTime">現在時刻</param>
    /// <param name="beatDuration">1ビートの長さ（秒）</param>
    void SpawnNote(float currentTime, float beatDuration)
    {
        if (notePoolManager == null)
        {
            Debug.LogError("NotePoolManager not found!");
            return;
        }

        // プールからノートを取得
        NoteController note = notePoolManager.SpawnNote(noteSpawnPosition, 0);

        // ノートの初期化
        // 開始位置から目標位置まで、4ビートの時間で移動するように設定（速度1/4）
        note.Initialize(noteSpawnPosition, noteTargetPosition, currentTime, currentTime + beatDuration * 4);

        // アクティブリストに追加
        activeNotes.Add(note);

        Debug.Log($"Note spawned at time: {currentTime}, will reach target at: {currentTime + beatDuration * 4}");
    }

    /// <summary>
    /// 全てのノートの位置を更新
    /// </summary>
    /// <param name="currentTime">現在時刻</param>
    void UpdateAllNotes(float currentTime)
    {
        foreach (var note in activeNotes)
        {
            note.UpdatePosition(currentTime);
        }
    }

    /// <summary>
    /// 削除位置を過ぎたノートをプールに返却
    /// </summary>
    void DestroyPassedNotes()
    {
        // 後ろから削除していく（リストの削除による影響を避けるため）
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            if (activeNotes[i].ShouldBeDestroyed(noteDestroyY))
            {
                activeNotes[i].ReturnToPool();
                activeNotes.RemoveAt(i);
            }
        }
    }

    void OnDestroy()
    {
        // 全てのノートをプールに返却
        foreach (var note in activeNotes)
        {
            note.ReturnToPool();
        }
        activeNotes.Clear();

        // 再生を停止
        if (player != null && playback.id != CriAtomExPlayback.invalidId)
        {
            player.Stop();
        }
    }

    private void OnBeatSyncCallback(ref CriAtomExBeatSync.Info info)
    {
        Debug.Log($"OnBeatSyncCallback: {info.barCount} {info.beatCount} {info.beatProgress} {info.bpm} {info.offset} {info.numBeats}");
    }
}
