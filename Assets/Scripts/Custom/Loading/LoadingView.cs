using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace loading
{
    /// <summary>
    /// Loading 场景：后台异步加载 Main，同时播放开场视频。
    /// 规则：
    ///   - 视频能播 → 播完再切场景（另有超时兜底）；
    ///   - 视频不能播（WebGL / 小游戏内核不支持内嵌 VideoClip）→ 直接放行。
    /// 关键点：放行 = 把 allowSceneActivation 置 true。
    /// Unity 会在 Main 真正就绪的那一刻才切换，提前置 true 是安全的，
    /// 所以这条路径不存在"放行早了切到半成品"或"永远不放行"的问题。
    /// </summary>
    public class LoadingView : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _player;

        [Tooltip("加载页最短展示时间（秒），避免一闪而过")]
        [SerializeField] private float _minDisplayTime = 0.3f;

        [Tooltip("视频分支的兜底放行时间（秒），拿不到视频长度时使用")]
        [SerializeField] private float _maxWaitTime = 20f;

        private AsyncOperation _loadingScene;
        private bool _finished;

        protected void Start()
        {
            _loadingScene = GameScenes.LoadMainScene();

            if (_loadingScene != null)
                _loadingScene.allowSceneActivation = false;

            bool videoAvailable = IsVideoAvailable();
            Debug.Log($"[Loading] 开始加载 Main，视频可用={videoAvailable}，平台={Application.platform}");

            if (videoAvailable)
            {
                _player.loopPointReached += End;
                _player.Play();
            }
            else if (_player != null)
            {
                // WebGL / 小游戏不支持内嵌 VideoClip：直接关掉播放器，不参与切场景判断
                _player.gameObject.SetActive(false);
            }

            StartCoroutine(WaitLoading(videoAvailable));
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.loopPointReached -= End;
        }

        /// <summary>判断当前环境能不能真正播放这段内嵌视频。</summary>
        private bool IsVideoAvailable()
        {
            if (_player == null || _player.clip == null)
                return false;

            // WebGL（含抖音小游戏）不支持内嵌 VideoClip，只能走 URL 播放
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return false;

            return true;
        }

        private IEnumerator WaitLoading(bool videoAvailable)
        {
            float startTime = Time.realtimeSinceStartup;

            // 加载页至少展示 _minDisplayTime
            while (Time.realtimeSinceStartup - startTime < _minDisplayTime)
                yield return null;

            if (!videoAvailable)
            {
                // 不看 progress，直接放行：
                // allowSceneActivation = true 只是「允许切换」，Main 没加载完不会切。
                Debug.Log("[Loading] 无视频分支，直接放行");
                End(_player);
                yield break;
            }

            float videoLength = _player != null && _player.clip != null ? (float)_player.clip.length : 0f;
            float limit = videoLength > 0f ? videoLength + 3f : Mathf.Max(_maxWaitTime, 5f);

            while (!_finished)
            {
                if (Time.realtimeSinceStartup - startTime > limit)
                {
                    Debug.LogWarning($"[Loading] 视频 {limit:0.0}s 内未播完，强制放行");
                    break;
                }

                yield return null;
            }

            End(_player);
        }

        private void End(VideoPlayer source)
        {
            if (_finished)
                return;

            _finished = true;

            if (_player != null)
                _player.loopPointReached -= End;

            if (_loadingScene != null)
            {
                Debug.Log("[Loading] 放行，等待 Main 场景激活");
                _loadingScene.allowSceneActivation = true;
            }
            else
            {
                // 极端情况下拿不到 AsyncOperation，兜底同步切场景
                Debug.LogWarning("[Loading] 未取到 AsyncOperation，直接切 Main");
                SceneManager.LoadScene("Main");
            }
        }
    }
}
