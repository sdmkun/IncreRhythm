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

    // NoteController.cs 内に追加
    private float _targetBeat;
    private Vector3 _startPos;
    private Vector3 _targetPos;

    // 生成時にプールをセットするメソッド
    public void SetPool(IObjectPool<NoteController> pool)
    {
        _managedPool = pool;
    }

    // // ノートの初期化（開始位置、目標位置、開始時刻、到達時刻を設定）
    // public void Initialize(Vector3 start, Vector3 target, float currentTime, float arrivalTime)
    // {
    //     startPosition = start;
    //     targetPosition = target;
    //     startTime = currentTime;
    //     targetTime = arrivalTime;
    //     isMoving = true;
    //     transform.position = start;
    // }

    // 初期化（生成時に呼ばれる）
    public void Initialize(Vector3 start, Vector3 target, float targetBeat)
    {
        _startPos = start;
        _targetPos = target;
        _targetBeat = targetBeat;

        // 見た目のリセットなど
        transform.position = start;
    }

    // 毎フレーム座標更新（Managerから呼ばれる）
    public void UpdatePositionByBeat(float currentTotalBeat)
    {
        // 残り拍数（ここがマイナスになると判定ラインを過ぎたことになる）
        float beatsRemaining = _targetBeat - currentTotalBeat;

        // 拍数に応じた座標計算（単純な線形補間ではなく、距離ベースで計算）
        // 4拍分の距離 = (_startPos - _targetPos) と仮定した場合の簡易計算
        // 必要に応じて調整してね

        // 例: 判定ライン(Target)を基準に、残り拍数分だけ上にずらす
        // 1拍あたりの移動距離（Y軸）
        float unitsPerBeat = (_startPos.y - _targetPos.y) / 4.0f; // 4.0fはAppearTime

        float newY = _targetPos.y + (beatsRemaining * unitsPerBeat);

        transform.position = new Vector3(_targetPos.x, newY, _targetPos.z);
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

    // デバッグ用：targetBeatをオブジェクトのそばに表示
    private void OnGUI()
    {
        if (Camera.main == null) return;

        // ワールド座標をスクリーン座標に変換
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        
        // スクリーン座標系ではY軸が反転しているので補正
        screenPos.y = Screen.height - screenPos.y;

        // オブジェクトの少し上に表示するためのオフセット
        screenPos.y -= 30;

        // 文字色を黒に設定
        Color originalColor = GUI.color;
        GUI.color = Color.black;

        // targetBeatの値を表示
        string label = $"Beat: {_targetBeat:F2}";
        GUI.Label(new Rect(screenPos.x - 50, screenPos.y, 100, 20), label);

        // 色を元に戻す
        GUI.color = originalColor;
    }
}

