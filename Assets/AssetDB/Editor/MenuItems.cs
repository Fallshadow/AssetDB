using FallShadow.Asset.Runtime;
using System;
using UnityEditor;
using UnityEngine;


namespace FallShadow.Asset.Editor {
    public class MenuItems {
        private const string UnSelectedTip = "Unity Selection.objects 没有数据，请尝试在 Project 右侧浏览项里右键！";
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

        [MenuItem("Assets/Dev Tools/Build", false, 1)]
        private static void BuildFolder() {
            if (Selection.objects == null || Selection.objects.Length == 0) {
                Debug.LogWarning(UnSelectedTip);
                return;
            }

            Build(true);
        }

        private static void Build(bool clearManifest, bool fromPkgflow = false) {

            var selections = Selection.objects;
            string[] selectAssetPaths = new string[selections.Length];

            for (var i = 0; i < selections.Length; i++) {
                selectAssetPaths[i] = AssetDatabase.GetAssetPath(selections[i]);
            }

            try {
                if (!fromPkgflow) {
                    Command.BuildSelectAssets(selectAssetPaths, clearManifest, Builder.DefaultOutput, Builder.IgnorePatterns);
                }
                else {
                    // Command.BuildPkgflowSelectAssets(selectAssetPaths, Builder.DefaultOutput, Builder.IgnorePatterns);
                }

                Command.GenerateFileList(Builder.DefaultOutput, new[] { "!files.txt", "!**/*.dll" });
            }
            catch (Exception e) {
                Debug.LogError($"MenuItems Build error: {e.StackTrace}");
                throw;
            }
        }
    }
}
