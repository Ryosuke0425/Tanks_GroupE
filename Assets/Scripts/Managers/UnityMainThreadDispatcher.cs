using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> actions = new Queue<Action>();
    private static UnityMainThreadDispatcher instance;
    private static readonly object lockObject = new object();

    /// <summary>
    /// シングルトンインスタンスの取得
    /// </summary>
    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        var obj = new GameObject("UnityMainThreadDispatcher");
                        instance = obj.AddComponent<UnityMainThreadDispatcher>();
                        DontDestroyOnLoad(obj);
                    }
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// 毎フレーム呼び出され、キュー内のアクションを実行
    /// </summary>
    private void Update()
    {
        int actionCount;

        // アクション数を制限してフレーム内での処理時間を抑制
        lock (actions)
        {
            actionCount = actions.Count;
        }

        for (int i = 0; i < actionCount; i++)
        {
            Action action;
            lock (actions)
            {
                if (actions.Count == 0)
                    return;

                action = actions.Dequeue();
            }

            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while executing action: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// メインスレッドで実行するアクションをキューに追加
    /// </summary>
    public static void Enqueue(Action action)
    {
        if (action == null) return;
        lock (actions)
        {
            actions.Enqueue(action);
        }
    }
}
