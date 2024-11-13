using FallShadow.Common;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    public partial class AssetDB {
        // ��Դ��Ҫ·������Դ��ϸ���Ե�ӳ��
        // example 1:
        // Key: "asset://graphics/pipelines.bundle"
        // value: AssetInfo.type = TaskType.Bundle AssetInfo.bundleKey = "graphics/pipelines.bundle" �Ǿ�����Դ����û�� assetPath
        // example 2:
        // Key: "asset://graphics/pipelines/forwardrenderer.asset"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "graphics/pipelines.bundle"  AssetInfo.assetPath = "forwardrenderer.asset"
        // example 3:
        // Key: "asset://samples/tps/arts/character/clips/chr_player_actor/clr_fall2idle.anim"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "samples/tps/arts/character/clips.bundle"  AssetInfo.assetPath = "chr_player_actor/clr_fall2idle.anim"
        internal NativeHashMap<FixedString512Bytes, AssetInfo> url2AssetInfo;
        // һ�� bundle ��Ӧ�����Դ Asset
        // ����� bundle ������ "graphics/pipelines.bundle" ���ֲ���ǰ׺Ҳ���� hash �ģ��������ԴҲ��������� bundle �ġ�
        private NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>> bundleKey2Assets;
    }
}