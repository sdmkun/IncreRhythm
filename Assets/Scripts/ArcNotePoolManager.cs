using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 円弧状のノートのプール管理
/// </summary>
public class ArcNotePoolManager : MonoBehaviour
{
    [SerializeField] private ArcNoteController arcNotePrefab; // プレハブ

    // プール本体
    private ObjectPool<ArcNoteController> _pool;

    private void Awake()
    {
        // ObjectPoolの初期化
        _pool = new ObjectPool<ArcNoteController>(
            // 1. CreateFunc: プールが空の時に新規作成する処理
            createFunc: () =>
            {
                var note = Instantiate(arcNotePrefab);
                note.SetPool(_pool); // 戻る場所を教えておく
                return note;
            },
            // 2. ActionOnGet: プールから取り出す時の処理（初期化など）
            actionOnGet: (note) =>
            {
                note.gameObject.SetActive(true);
            },
            // 3. ActionOnRelease: プールに戻す時の処理
            actionOnRelease: (note) =>
            {
                note.gameObject.SetActive(false);
            },
            // 4. ActionOnDestroy: プールが許容量を超えて破棄する時の処理
            actionOnDestroy: (note) =>
            {
                Destroy(note.gameObject);
            },
            collectionCheck: true, // 二重解放のチェック（開発時はtrue推奨）
            defaultCapacity: 100,  // 最初に確保する数（円形なので多めに）
            maxSize: 500           // プールの最大サイズ
        );
    }

    /// <summary>
    /// 円弧ノートを生成
    /// </summary>
    /// <param name="targetBeat">目標ビート</param>
    /// <param name="appearTime">出現時間（拍数）</param>
    /// <param name="angle">角度（度数法）</param>
    /// <param name="radius">目標半径</param>
    public ArcNoteController SpawnArcNote(float targetBeat, float appearTime, float angle, float radius)
    {
        // Get()を呼ぶと、在庫があれば再利用、なければCreateFuncが走る
        var note = _pool.Get();

        // ノートの初期化
        note.Initialize(targetBeat, appearTime, angle, radius);

        return note;
    }
}
