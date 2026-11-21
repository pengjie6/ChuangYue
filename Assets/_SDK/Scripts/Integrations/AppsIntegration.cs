using System.Collections.Generic;
using UnityEngine;

namespace apps
{
    /// <summary>
    /// 应用级集成入口。
    /// 已移除全部第三方统计与归因 SDK，
    /// 仅保留内部 ADS 抽象（实际广告由 AdSDK 的抖音平台接管）。
    /// </summary>
    public static class AppsIntegration
    {
        public readonly static string s_AppSettingsName = "AppsSettings";
        private static List<object> s_Applications = new List<object>();
        private static List<IApplicationPause> s_ApplicationsPause = new List<IApplicationPause>();
        private static bool _isInited;
        private static GameObject _objectHelper;

        [RuntimeInitializeOnLoadMethod]
        public static void AutoInitialize()
        {
            AppsSettings settings = Resources.Load<AppsSettings>(s_AppSettingsName);
            if (settings == null) return;
            if (settings.autoInitialize) Initialize(settings);
        }

        public static void Initialize()
        {
            Initialize(Resources.Load<AppsSettings>(s_AppSettingsName));
        }

        public static void Initialize(AppsSettings settings)
        {
            if (_isInited == true) return;

            if (settings == null) throw new System.NullReferenceException("AppsSettings not found in resource folder!");

            InstintiateObjectHelper();

            if (settings.integrateADS)
            {
                IADS adsMaker = null;
                switch (settings.showADSType)
                {
                    case ShowADSType.Simulator:
                        adsMaker = new ADSSimulator("####### Simulator ######", settings.adsInfo.useBanner, settings.adsInfo.useInterstitial, settings.adsInfo.useRewardedVideo);
                        break;
                    case ShowADSType.Debug:
                    case ShowADSType.Non:
                        adsMaker = new ADSDebugger("####### Debuger ######", settings.showADSType == ShowADSType.Debug);
                        break;
                }
                ADSManager.Initialize(adsMaker, true, settings.adsInfo.showInterstitialEvery);
                s_Applications.Add(adsMaker);
            }

            if (settings.debugMode)
            {
                EventsDebug debug = new EventsDebug();
                EventsLogger.AddEvent(debug);
                s_Applications.Add(debug);
            }

            Debug.Log("The apps is initialized...");
            _isInited = true;
        }

        private static void InstintiateObjectHelper()
        {
            _objectHelper = new GameObject("Integrations Helper");
            _objectHelper.AddComponent(typeof(IntegrationHelper));
            Object.DontDestroyOnLoad(_objectHelper);
        }

        public static void OnApplicationPause(bool pause)
        {
            foreach (IApplicationPause Application in s_ApplicationsPause)
            {
                if (Application != null && !Application.Equals(null)) Application.OnApplicationPause(pause);
            }
        }
    }
}
