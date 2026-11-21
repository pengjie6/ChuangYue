using UnityEngine;

namespace apps
{
    public enum ShowADSType { Simulator, Debug, Non }

    /// <summary>
    /// 应用集成配置。
    /// 已移除全部第三方统计与归因 SDK，仅保留内部 ADS 模拟/调试开关。
    /// </summary>
    [CreateAssetMenu(fileName = "AppsSettings", menuName = "AppsSettings", order = 1)]
    public class AppsSettings : ScriptableObject
    {
        public readonly static string globalDirectoryPath = "Assets/_SDK/Resources/";

        public bool autoInitialize = true;

        public bool integrateADS = false;
        public ShowADSType showADSType = ShowADSType.Simulator;
        public ADSInfo adsInfo;

        public bool debugMode = true;
    }
}
