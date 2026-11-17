using engine.coin;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 「看广告领金币」按钮。
/// 点击弹出 AdConfirm 确认框 → 播完抖音激励视频 → 给玩家加 <see cref="rewardAmount"/> 金币。
///
/// 用法：挂到任意带 Button 的物体上，把 ScriptableObjects/Data/Coins Data.asset 拖到 coinsData 字段，
/// rewardAmount 填 1000 即可。金币数字会自动通过 CoinsData.onUpdate 刷新到 HUD 上。
/// </summary>
[RequireComponent(typeof(Button))]
public class FreeCoinButton : MonoBehaviour
{
    [Header("奖励设置")]
    [Tooltip("看完激励视频后发放的金币数量")]
    [SerializeField] private int rewardAmount = 1000;

    [Header("引用")]
    [Tooltip("金币存档资源（Coins Data.asset），必须赋值，否则发不了奖励")]
    [SerializeField] private CoinsData coinsData;

    private Button _button;

    /// <summary>防止连点：一次广告流程未结束前不再响应点击。</summary>
    private bool _isPlaying;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
        {
            Debug.LogError("[广告] FreeCoinButton 所在物体没有 Button 组件，按钮无法点击");
        }
    }

    private void OnEnable()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClick);
            _button.onClick.AddListener(OnClick);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClick);
        }
    }

    /// <summary>点击入口（也挂在 Button 的 OnClick 里手动拖方法也行）。</summary>
    public void OnClick()
    {
        if (_isPlaying)
        {
            Debug.Log("[广告] 上一次广告流程还没结束，忽略本次点击");
            return;
        }

        if (coinsData == null)
        {
            Debug.LogError("[广告] FreeCoinButton 的 coinsData 未赋值，无法发放奖励");
            return;
        }

        _isPlaying = true;
        Debug.Log($"[广告] 打开激励视频确认框，奖励 {rewardAmount} 金币");

        // 看完 → 发奖励；中途关闭或广告失败 → 复位，允许再次点击
        AdConfirm.Show(GrantReward, rewardAmount, ResetState);
    }

    /// <summary>激励视频播放完成，发放金币。</summary>
    private void GrantReward()
    {
        _isPlaying = false;

        if (coinsData == null)
        {
            Debug.LogError("[广告] coinsData 丢失，奖励发放失败");
            return;
        }

        coinsData.AddCoins(rewardAmount);
        Debug.Log($"[广告] 激励视频播放完成，发放 {rewardAmount} 金币，当前总数 {coinsData.totalCoins}");
    }

    /// <summary>玩家关掉了确认框，或者广告加载/播放失败。</summary>
    private void ResetState()
    {
        _isPlaying = false;
        Debug.Log("[广告] 未获得奖励（取消或广告失败），可重新点击");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (coinsData == null)
        {
            coinsData = editor.AssetUtility.FindScribtableObjectOfType<CoinsData>();
        }
    }
#endif
}
