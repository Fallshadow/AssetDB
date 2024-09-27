using UnityEngine;

namespace FallShadow.Asset.Runtime {
    [CreateAssetMenu(fileName = "RemoteConfig", menuName = "Create RemoteConfig", order = 1)]
    public class RemoteConfig : ScriptableObject {
        // Զ����Դ���·��
        public string remoteUrl;
    }
}
