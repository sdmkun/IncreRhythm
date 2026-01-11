using UnityEngine;
using UnityEngine.Pool;

public class NotePoolManager : MonoBehaviour
{
    [SerializeField] private NoteController notePrefab; // プレハブ

    // プール本体
    private ObjectPool<NoteController> _pool;

    private void Awake()
    {
        // ObjectPoolの初期化
        _pool = new ObjectPool<NoteController>(
            // 1. CreateFunc: プールが空の時に新規作成する処理
            createFunc: () =>
            {
                var note = Instantiate(notePrefab);
                note.SetPool(_pool); // 戻る場所を教えておく
                return note;
            },
            // 2. ActionOnGet: プールから取り出す時の処理（初期化など）
            actionOnGet: (note) =>
            {
                note.gameObject.SetActive(true);
                // 必要ならここで位置やタイプのリセットを行う
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
            defaultCapacity: 50,   // 最初に確保する数
            maxSize: 200           // プールの最大サイズ
        );
    }

    // ゲームロジックから呼ぶメソッド
    public NoteController SpawnNote(Vector3 position, float targetTime)
    {
        // Get()を呼ぶと、在庫があれば再利用、なければCreateFuncが走る
        var note = _pool.Get();

        note.transform.position = position;
        // その他、譜面データ（NoteTimeなど）をここでセットする
        // note.Init(targetTime); 

        return note;
    }
}