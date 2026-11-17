using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 激励广告弹窗。
/// 玩家点「确认」→ 播放抖音激励视频 → 播完回调成功；关闭弹窗或广告失败回调失败。
/// </summary>
public class AdConfirm : MonoBehaviour
{
    private static AdConfirm instance;

    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnConfirm;

    [Tooltip("弹窗中间显示奖励数量的文本，留空则不刷新数量")]
    [SerializeField] private Text rewardValueText;

    private Action onCompleted;
    private Action onFailed;

    /// <summary>一次弹窗生命周期内只允许回调一次，避免重复发奖。</summary>
    private bool _finished;

    /// <summary>Show 主动拉起弹窗时置位，避免 Awake 里又把它关掉。</summary>
    private static bool _showRequested;

    /// <summary>
    /// 弹出确认框。
    /// </summary>
    /// <param name="callBack">看完广告后的成功回调</param>
    /// <param name="rewardAmount">本次奖励数量，大于 0 时会刷新到弹窗上（可不传）</param>
    /// <param name="onFail">玩家关闭弹窗 / 广告加载或播放失败时的回调（可为 null）</param>
    public static void Show(Action callBack, int rewardAmount = 0, Action onFail = null)
    {
        if (instance == null)
        {
            instance = FindOrCreateInstance();
            if (instance == null)
            {
                onFail?.Invoke();
                return;
            }
        }

        // 设置回调并显示弹窗
        instance.onCompleted = callBack;
        instance.onFailed = onFail;
        instance._finished = false;

        if (rewardAmount > 0 && instance.rewardValueText != null)
        {
            instance.rewardValueText.text = rewardAmount.ToString();
        }

        // 如果这个实例之前从未被激活过，Awake 会在 SetActive 时同步执行，
        // 用这个标记告诉 Awake「是 Show 主动要显示的，别再自己关掉」。
        _showRequested = true;
        instance.gameObject.SetActive(true);
        _showRequested = false;
    }

    /// <summary>
    /// 取弹窗实例：优先复用场景里已有的那个（哪怕它是未激活状态）。
    /// 复用场景副本很重要——它挂在主 Canvas 下，跟随主 Canvas 的 CanvasScaler 缩放；
    /// 如果直接 Instantiate 到场景根节点，弹窗会用自己的 CanvasScaler（720×1280），
    /// 缩放系数和主 Canvas（1080×1920）不一致，弹窗会明显偏大。
    /// </summary>
    private static AdConfirm FindOrCreateInstance()
    {
        AdConfirm found = FindObjectOfType<AdConfirm>(true);
        if (found != null)
        {
            return found;
        }

        // 场景里没有，才从 Resources 兜底创建一个：挂到主 Canvas 下，保证缩放一致
        GameObject prefab = Resources.Load<GameObject>("AdConfirm");
        if (prefab == null)
        {
            Debug.LogError("AdConfirm 预制体未找到，请确保其位于 Resources 文件夹中。");
            return null;
        }

        Canvas canvas = FindRootCanvas();
        GameObject go = canvas != null
            ? Instantiate(prefab, canvas.transform, false)
            : Instantiate(prefab);

        AdConfirm created = go.GetComponent<AdConfirm>();
        if (created == null)
        {
            Debug.LogError("AdConfirm 脚本未附加到预制体上，请检查预制体设置。");
            Destroy(go);
        }

        return created;
    }

    /// <summary>找场景里最外层的 Canvas（嵌套 Canvas 的缩放不可控，不能用来当父节点）。</summary>
    private static Canvas FindRootCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].isRootCanvas)
            {
                return canvases[i];
            }
        }

        return null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (btnClose != null)
        {
            btnClose.onClick.RemoveListener(this.OnClose);
            btnClose.onClick.AddListener(this.OnClose);
        }

        if (btnConfirm != null)
        {
            btnConfirm.onClick.RemoveListener(this.OnConfirm);
            btnConfirm.onClick.AddListener(this.OnConfirm);
        }

        // 正常情况（场景里预摆放的副本）开局先隐藏，等 Show 再拉起；
        // 如果这次激活本身就是 Show 要求的，就不要关。
        if (!_showRequested)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnClose()
    {
        Finish(false);
    }

    void OnConfirm()
    {
        if (_finished)
            return;

#if UNITY_EDITOR
        // 编辑器里没有真实广告，直接当成功处理，方便调试
        Finish(true);
#else
        if (AdManager.Instance == null)
        {
            Debug.LogError("[广告] 场景里没有 AdManager，无法播放广告");
            OnAdError("广告服务未初始化");
            return;
        }
        AdManager.Instance.ShowRewardedVideoAd(OnAdWatched, OnAdError);
#endif
    }

    private void OnAdWatched()
    {
        Debug.Log("广告观看完成");
        Finish(true);
    }

    private void OnAdError(string errorMessage)
    {
        Debug.LogError($"广告播放失败: {errorMessage}");
        // 加载 AdError 预制体
        GameObject adErrorPrefab = Resources.Load<GameObject>("AdError");
        if (adErrorPrefab != null)
        {
            // 实例化 AdError
            GameObject adErrorInstance = Instantiate(adErrorPrefab, transform.position, Quaternion.identity);
            // 1 秒后销毁实例
            Destroy(adErrorInstance, 1.5f);
        }
        else
        {
            Debug.LogError("AdError 预制体未找到，请确保其位于 Resources 文件夹中。");
        }

        Finish(false);
    }

    /// <summary>结束本次弹窗：只回调一次，然后隐藏弹窗。</summary>
    private void Finish(bool success)
    {
        if (_finished)
            return;
        _finished = true;

        Action callback = success ? onCompleted : onFailed;
        onCompleted = null;
        onFailed = null;

        gameObject.SetActive(false);
        callback?.Invoke();
    }
}
