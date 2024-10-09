using FallShadow.Asset.Runtime;
using FallShadow.Common;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// 此类作用：从 atlas 加载 png 等 sprite 图片资源
/// 主要接口 AssetDBAtlasSpriteLoader.LoadSprite
/// </summary>
public class AssetDBAtlasSpriteLoader {
    public struct AtlasSpriteHandle {
        public int id;
        public AssetDBAtlasSpriteLoader manager;

        public bool isValid => id != TASK_INVALID_ID;
        public bool isDone => manager != null && manager.IsSpriteLoadDone(id);
        public Sprite sprite => manager?.GetSprite(id);

        public static readonly AtlasSpriteHandle invalid = new() {
            id = TASK_INVALID_ID
        };
    }

    private struct AtlasSpriteTask {
        public int id;
        public FixedString128Bytes atlasUrl;
        public FixedString32Bytes spriteName;
        public Handle<UAsset> atlasHandle;

        public static readonly AtlasSpriteTask invalid = new() {
            id = TASK_INVALID_ID
        };
    }

    private struct AtlasSpriteCache {
        public int id;
        public FixedString128Bytes atlasUrl;
        public FixedString32Bytes spriteName;
        // 超过指定帧数就视为作废，从缓存移除
        public int frame;

        public static readonly AtlasSpriteCache invalid = new() {
            id = TASK_INVALID_ID,
            frame = int.MaxValue
        };
    }

    private const int MAX_ATLAS_COUNT = 256;
    private const int MAX_SPRITE_COUNT = 256;
    private const int TASK_INVALID_ID = -1;
    private const int CACHE_FRAMES = 4;

    private Dictionary<FixedString128Bytes, SpriteAtlas> atlases;

    private int atlasSpriteTaskIdCounter = 0;

    private AtlasSpriteTask[] atlasSpriteTasks = new AtlasSpriteTask[MAX_SPRITE_COUNT];
    private int atlasSpriteTaskCount = 0;

    private AtlasSpriteCache[] atlasSpriteCaches = new AtlasSpriteCache[MAX_SPRITE_COUNT];
    private int atlasSpriteCacheCount = 0;

    public void Init() {
        atlases = new Dictionary<FixedString128Bytes, SpriteAtlas>(MAX_ATLAS_COUNT);

        InitTask();
        InitCache();
    }

    public void Update() {
        CheckCaches();
        CheckTasks();
    }

    public void Dispose() {
        if (atlases != null) {
            atlases.Clear();
            atlases = null;
        }

        atlasSpriteTasks = null;
        atlasSpriteCaches = null;
    }

    // 总结：从 atlas 加载 sprite，atlas 一经加载就保存起来，n 帧缓存 AtlasSpriteCache。
    // 先从 atlases 缓存中检查是否刚刚加载过
    // atlases 缓存存在，AtlasSpriteCache 缓存也存在，返回
    // atlases 缓存存在，AtlasSpriteCache 缓存不存在，创建 AtlasSpriteCache 缓存，返回
    // atlases 缓存不存在，查看 AtlasSpriteTask 是否正在加载。
    // AtlasSpriteTask 存在，返回
    // AtlasSpriteTask 不存在，创建 atlas 加载任务，在 update 中检查是否加载完成。
    // 加载完成就创建 atlases 缓存和 AtlasSpriteCache 缓存。
    public AtlasSpriteHandle LoadSprite(FixedString128Bytes atlasUrl, FixedString32Bytes spriteName) {
        AtlasSpriteHandle resultHandle = new AtlasSpriteHandle { manager = this };

        if (atlases.ContainsKey(atlasUrl)) {
            for (var i = 0; i < atlasSpriteCacheCount; i++) {
                var cache = atlasSpriteCaches[i];

                if (cache.atlasUrl == atlasUrl && cache.spriteName == spriteName) {
                    resultHandle.id = cache.id;
                    return resultHandle;
                }
            }

            var newCache = new AtlasSpriteCache() {
                id = GenerateSpriteTaskId(),
                atlasUrl = atlasUrl,
                spriteName = spriteName,
                frame = Time.frameCount
            };

            atlasSpriteCaches[atlasSpriteCacheCount++] = newCache;

            resultHandle.id = newCache.id;
            return resultHandle;
        }

        for (var i = 0; i < atlasSpriteTaskCount; i++) {
            var task = atlasSpriteTasks[i];

            if (task.atlasUrl == atlasUrl && task.spriteName == spriteName) {
                resultHandle.id = task.id;
                return resultHandle;
            }
        }

        var id = GenerateSpriteTaskId();

        atlasSpriteTasks[atlasSpriteTaskCount++] = new AtlasSpriteTask() {
            id = id,
            atlasUrl = atlasUrl,
            spriteName = spriteName,
            atlasHandle = ResHelper.db.Load(atlasUrl)
        };

        resultHandle.id = id;
        return resultHandle;
    }

    private Sprite GetSprite(int id) {
        for (var i = 0; i < atlasSpriteCacheCount; i++) {
            var cache = atlasSpriteCaches[i];

            if (cache.id != id) {
                continue;
            }

            if (atlases.TryGetValue(cache.atlasUrl, out var spriteAtlas)) {
                return spriteAtlas.GetSprite(cache.spriteName.ToString());
            }
        }

        return null;
    }

    private int GenerateSpriteTaskId() {
        atlasSpriteTaskIdCounter++;

        if (atlasSpriteTaskIdCounter <= 0) {
            atlasSpriteTaskIdCounter = 1;
        }

        return atlasSpriteTaskIdCounter % int.MaxValue;
    }

    private bool IsSpriteLoadDone(int id) {
        for (var i = 0; i < atlasSpriteCacheCount; i++) {
            var cache = atlasSpriteCaches[i];

            if (cache.id == id) {
                return true;
            }
        }

        return false;
    }

    private void InitCache() {
        atlasSpriteCaches = new AtlasSpriteCache[MAX_SPRITE_COUNT];
        atlasSpriteCacheCount = 0;
        for (var i = 0; i < atlasSpriteCaches.Length; i++) {
            atlasSpriteCaches[i] = AtlasSpriteCache.invalid;
        }
    }

    private void InitTask() {
        atlasSpriteTasks = new AtlasSpriteTask[MAX_SPRITE_COUNT];
        atlasSpriteTaskCount = 0;
        for (var i = 0; i < atlasSpriteTasks.Length; i++) {
            atlasSpriteTasks[i] = AtlasSpriteTask.invalid;
        }
    }

    private void CheckCaches() {
        for (var i = 0; i < atlasSpriteCacheCount; i++) {
            var cache = atlasSpriteCaches[i];

            if (Time.frameCount - cache.frame > CACHE_FRAMES) {
                atlasSpriteCaches[i] = atlasSpriteCaches[atlasSpriteCacheCount - 1];
                atlasSpriteCaches[atlasSpriteCacheCount - 1] = AtlasSpriteCache.invalid;
                atlasSpriteCacheCount--;
                i--;
            }
        }
    }

    private void CheckTasks() {
        for (var i = 0; i < atlasSpriteTaskCount; i++) {
            var task = atlasSpriteTasks[i];

            if (ResHelper.db.IsSucceeded(task.atlasHandle)) {
                CacheTask(task);
                atlases[task.atlasUrl] = ResHelper.db.GetAssetFromCache(task.atlasHandle) as SpriteAtlas;
                atlasSpriteTasks[i] = atlasSpriteTasks[atlasSpriteTaskCount - 1];
                atlasSpriteTasks[atlasSpriteTaskCount - 1] = AtlasSpriteTask.invalid;
                atlasSpriteTaskCount--;
                i--;
            }
        }
    }

    private void CacheTask(AtlasSpriteTask task) {
        atlasSpriteCaches[atlasSpriteCacheCount++] = new AtlasSpriteCache() {
            id = task.id,
            atlasUrl = task.atlasUrl,
            spriteName = task.spriteName,
            frame = Time.frameCount,
        };
    }
}