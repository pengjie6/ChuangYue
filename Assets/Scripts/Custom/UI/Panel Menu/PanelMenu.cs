using UnityEngine;

namespace main.ui
{
    public class PanelMenu : Panel
    {
        [SerializeField] protected MainCanvasManager _mainCanvasManager;
        [SerializeField] protected LevelsProgress _levelsProgress;

        public void StartGame()
        {
            // 预制体无法保存场景对象的引用，实例化后可能为空，这里兜底查找
            if (_mainCanvasManager == null)
                _mainCanvasManager = GetComponentInParent<MainCanvasManager>();

            if (_mainCanvasManager == null)
            {
                Debug.LogError("[PanelMenu] 找不到 MainCanvasManager，点击无法响应");
                return;
            }

            _mainCanvasManager.StartGame();
        }

        public override void Show()
        {
            base.Show();

            if (_levelsProgress == null)
            {
                Debug.LogWarning("[PanelMenu] _levelsProgress 未赋值，跳过关卡进度初始化");
                return;
            }

            try
            {
                _levelsProgress.Initialize();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PanelMenu] 关卡进度初始化异常: " + e);
            }
        }

        protected void OnValidate()
        {
            if (_mainCanvasManager == null)
                _mainCanvasManager = GetComponentInParent<MainCanvasManager>();
        }
    }
}
