using UnityEditor;
using UnityEngine;
using FallShadow.Asset.Runtime;

namespace FallShadow.Asset.Editor {
    public class MenuItems {
        private const string AssetDatabaseMenuPath = "Asset Tools/Debug/AssetDatabase Load Mode";
        private static readonly string OpenAssetDatabaseLoad = $"开启 AssetDatabase 模拟（仅 Editor 生效）\n可通过 PlayerPrefs.GetInt(\"{AssetDBKey.AssetDatabasePrefKey}\") 获取，开启为 1";
        private static readonly string CloseAssetDatabaseLoad = $"关闭 AssetDatabase 模拟（仅 Editor 生效）\n可通过 PlayerPrefs.GetInt(\"{AssetDBKey.AssetDatabasePrefKey}\") 获取，关闭为 0";

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
