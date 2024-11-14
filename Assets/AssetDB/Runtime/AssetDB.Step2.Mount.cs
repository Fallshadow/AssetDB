using FallShadow.Common;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

namespace FallShadow.Asset.Runtime {
    // mount 
    // ָ��һ���浵�ļ������� fileKey2FilePath
    // һ������浵�ļ�����Դ�嵥һ��
    public partial class AssetDB {
        private const int maxMountCount = 16;

        // Զ����Դ·��
        // ���� {remoteConfig.remoteUrl}/StreamingAssets
        // �� https://CDN/Project/Version/StreamingAssets
        // Ĭ��������� streamingAssets ·����ʹ�� CDN ��Դʱ��ָ��
        // ��ȡ��Դʱ�������� mount ·��
        private FixedString512Bytes specialDirectory;

        // ��Դ����·��  mount��"d:/app/sandbox"
        // �ļ����ؼ�   filekey: "arts/fonts/font1.bundle"
        // ��Դ����·�� filepath: "d:/app/sandbox/arts/fonts/font1.bundle"
        // ���������ʹ�������·��
        private NativeList<FixedString512Bytes> mounts;

        // �ļ�ȥ Hash ��Զ�·�����ļ���·��֮���ӳ��
        // example: Զ����Դ
        // scenes.bundle
        // https://CDN/Project/Version/StreamingAssets/scenes_8dda37713dd29cbf4dd8502556ba3db2.bundle
        private Dictionary<FixedString512Bytes, string> fileKey2FilePath;

        private void InitMount() {
            mounts = new NativeList<FixedString512Bytes>(maxMountCount, Allocator.Persistent);
            specialDirectory = Application.streamingAssetsPath.Replace("\\", sep.ToString());
            fileKey2FilePath = new Dictionary<FixedString512Bytes, string>();
        }

        private void DisposeMount() {
            if (mounts.IsCreated) {
                mounts.Dispose();
            }

            if (fileKey2FilePath != null) {
                fileKey2FilePath.Clear();
                fileKey2FilePath = null;
            }
        }

        public void SetSpecialDirectory(FixedString512Bytes url) {
            if (url.Length == 0) {
                throw new Exception($"invalid url: {url}");
            }

            specialDirectory = url;
        }

        public void Mount(FixedString512Bytes dirPath) {
            mounts.Add(FixedStringUtil.Replace(new FixedString512Bytes(dirPath), '\\', '/'));
        }

        public void UnMount(FixedString512Bytes dirPath) {
            if (!mounts.IsCreated)
                return;

            FixedString512Bytes unmount = FixedStringUtil.Replace(new FixedString512Bytes(dirPath), '\\', '/');
            int index = mounts.IndexOf(unmount);

            if (index == -1)
                return;

            mounts.RemoveAt(index);

            foreach (var kv in fileKey2FilePath) {
                FixedString512Bytes fileKey = kv.Key;

                if (fileKey.Length <= bundleSep.Length) {
                    continue;
                }

                if (!kv.Value.StartsWith(unmount.ToString())) {
                    continue;
                }

                if (!FixedStringUtil.Substring(fileKey, fileKey.Length - bundleSep.Length).Equals(bundleSep)) {
                    ReleaseByUrl($"asset://{fileKey}", true);
                    continue;
                }

                // ���Խ��� bundle �µ�������Դ Release
                foreach (var kv1 in url2AssetInfo) {
                    if (fileKey != kv1.Value.bundleKey) {
                        continue;
                    }

                    ReleaseByUrl(kv1.Key, true);
                }

                if (bundleKey2Deps.TryGetValue(fileKey, out var deps)) {
                    bundleKey2Deps.Remove(fileKey);
                    deps.Dispose();
                }
            }
        }

        // ����� fileKey2FilePath
        private void GlobFiles() {
            subSceneFileName2Path.Clear();
            url2ContentCatalogPath.Clear();

            foreach (var mount in mounts) {
                var dirPath = mount.ToString();
                string[] files;

                // �õ������ԴĿ¼�µ������ļ�
                // TODO: �����и����ʣ�Զ��·���� resourceBundleFilePaths ��ȡ�ģ����������Щ .assets�� �᲻�������⣿
                if (dirPath.StartsWith(specialDirectory.ToString())) {
                    var list = new List<string>();

                    foreach (var path in resourceBundleFilePaths) {
                        var fullPath = $"{specialDirectory}/{path}";
                        list.Add(fullPath);
                    }

                    files = list.ToArray();
                }
                else {
                    if (!Directory.Exists(dirPath)) {
                        continue;
                    }

                    files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
                }


                var length = mount.Length;
                var isRemoteDirectory = dirPath.StartsWith(httpsSep) || dirPath.StartsWith(httpSep);
                var isStreamingAssets = dirPath.StartsWith(Application.streamingAssetsPath.Replace("\\", sep.ToString()));

                foreach (var file in files) {
                    var fsFile = FixedStringUtil.Replace(new FixedString512Bytes(file), '\\', '/');
                    // ��ȡ���ļ���Զ�·��
                    var relativePath = FixedStringUtil.Substring(fsFile, length + 1);

                    // ECS subScene
                    if (relativePath.IndexOf(entitySceneDir) != -1 || relativePath.IndexOf(contentArchiveDir) != -1) {
                        if (relativePath.IndexOf(contentFileName) != -1) {
                            var relativeDirectory = FixedStringUtil.Substring(relativePath, 0, relativePath.Length - contentFileName.Length);

                            FixedString512Bytes url = protocolSep;
                            url.Append(FixedStringUtil.ToLower(relativeDirectory));
                            url.Append(bundleSep);

                            url2ContentCatalogPath[url] = fsFile;
                        }
                        else {
                            var fileName = Path.GetFileName(file);
                            subSceneFileName2Path[fileName] = fsFile;
                        }
                    }
                    // �����ļ�
                    else {
                        if (hash2normal.ContainsKey(relativePath)) {
                            relativePath = hash2normal[relativePath];
                        }

                        relativePath = FixedStringUtil.ToLower(relativePath);
                        var index = relativePath.IndexOf(bundleSep);
                        var isBundleFile = index != -1 && index + bundleSep.Length == relativePath.Length;

                        if (isBundleFile) {
                            fileKey2FilePath[relativePath] = fsFile.ToString();
                        }
                        else {
                            if (isRemoteDirectory) {
                                fileKey2FilePath[relativePath] = fsFile.ToString();
                            }
                            else {
                                switch (Application.platform) {
                                    case RuntimePlatform.Android:
                                        if (isStreamingAssets) {
                                            fileKey2FilePath[relativePath] = fsFile.ToString();
                                        }
                                        else {
                                            fileKey2FilePath[relativePath] = $"file://{fsFile}";
                                        }
                                        break;
                                    case RuntimePlatform.LinuxPlayer:
                                    case RuntimePlatform.OSXPlayer:
                                    case RuntimePlatform.OSXEditor:
                                    case RuntimePlatform.IPhonePlayer:
                                        fileKey2FilePath[relativePath] = $"file://{fsFile}";
                                        break;
                                    default:
                                        fileKey2FilePath[relativePath] = fsFile.ToString();
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}