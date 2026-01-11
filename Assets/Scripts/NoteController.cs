using UnityEngine;
using UnityEngine.Pool; // これが必要

public class NoteController : MonoBehaviour
{
    // 自身の所属するプールを覚えておく変数
    private IObjectPool<NoteController> _managedPool;

    // ノートの移動に関する変数
    private Vector3 startPosition;    // 開始位置
    private Vector3 targetPosition;   // 目標位置
    private float startTime;          // 開始時刻
    private float targetTime;         // 到達目標時刻
    private bool isMoving = false;    // 移動中かどうか

    // 生成時にプールをセットするメソッド
    public void SetPool(IObjectPool<NoteController> pool)
    {
        _managedPool = pool;
    }

    // ノートの初期化（開始位置、目標位置、開始時刻、到達時刻を設定）
    public void Initialize(Vector3 start, Vector3 target, float currentTime, float arrivalTime)
    {
        startPosition = start;
        targetPosition = target;
        startTime = currentTime;
        targetTime = arrivalTime;
        isMoving = true;
        transform.position = start;
    }

    // 現在の時刻に基づいてノートの位置を更新
    public void UpdatePosition(float currentTime)
    {
        if (!isMoving) return;

        // 経過時間の割合を計算（0.0 ～ 1.0）
        float progress = (currentTime - startTime) / (targetTime - startTime);
        progress = Mathf.Clamp01(progress);

        // 線形補間で位置を更新
        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
    }

    // ノートが削除位置に到達したかチェック
    public bool ShouldBeDestroyed(float destroyY)
    {
        return transform.position.y <= destroyY;
    }

    // 画面外に出た時などに呼ぶ
    public void ReturnToPool()
    {
        if (_managedPool != null)
        {
            isMoving = false;
            _managedPool.Release(this); // 自分自身をプールに返却
        }
    }

    public void SetPosition(float y)
    {
        gameObject.transform.position = new Vector3(0, y, 0);
    }
}