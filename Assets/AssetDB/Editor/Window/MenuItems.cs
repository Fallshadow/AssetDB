using UnityEditor;
using UnityEngine;
using FallShadow.Asset.Runtime;

namespace FallShadow.Asset.Editor {
    public class MenuItems {
        private const string AssetDatabaseMenuPath = "Asset Tools/Debug/AssetDatabase Load Mode";
        private static readonly string OpenAssetDatabaseLoad = $"���� AssetDatabase ģ�⣨�� Editor ��Ч��\n��ͨ�� PlayerPrefs.GetInt(\"{AssetDBKey.AssetDatabasePrefKey}\") ��ȡ������Ϊ 1";
        private static readonly string CloseAssetDatabaseLoad = $"�ر� AssetDatabase ģ�⣨�� Editor ��Ч��\n��ͨ�� PlayerPrefs.GetInt(\"{AssetDBKey.AssetDatabasePrefKey}\") ��ȡ���ر�Ϊ 0";

        [MenuItem(AssetDatabaseMenuPath)]
        private static void AssetDatabaseLoadMode() {
            var currentMode = PlayerPrefs.GetInt(AssetDBKey.AssetDatabasePrefKey, 0);
            var isChecked = currentMode == 1;
            isChecked = !isChecked;
            PlayerPrefs.SetInt(AssetDBKey.AssetDatabasePrefKey, isChecked ? 1 : 0);
            PlayerPrefs.Save();

            Menu.SetChecked(AssetDatabaseMenuPath, isChecked);
            Debug.LogWarning(isChecked ? OpenAssetDatabaseLoad : CloseAssetDatabaseLoad);
        }

        [MenuItem(AssetDatabaseMenuPath, true)]
        private static bool ToggleAssetDatabaseLoadMode_Validate() {
            var currentMode = PlayerPrefs.GetInt(AssetDBKey.AssetDatabasePrefKey, 0);
            Menu.SetChecked(AssetDatabaseMenuPath, currentMode == 1);
            return true;
        }
    }
}
