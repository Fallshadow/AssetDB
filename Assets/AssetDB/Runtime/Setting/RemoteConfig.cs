using UnityEngine;

namespace FallShadow.Asset.Runtime {
    [CreateAssetMenu(fileName = "RemoteConfig", menuName = "Create RemoteConfig", order = 1)]
    public class RemoteConfig : ScriptableObject {
        // 远程资源存放路径
        public string remoteUrl;
        // 远程设置文件存放路径
        public string settingUrl;
    }
}
