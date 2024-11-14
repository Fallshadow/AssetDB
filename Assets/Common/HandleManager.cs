using FallShadow.Common;
using System;
using Unity.Collections;

// 从池子里获取 handle，使用过一次 version 加一
// 可以用于检验合法性，比如可以瞬间申请加载 A 资源 100 次，计数增加到了 100，前面 99 次创建的申请在使用时会被拒绝，因为不合法
// 但是一般来说，同样资源的申请，应该只存在一个，这里相当于更加保险。
public struct HandleManager<T> {
    private NativeArray<int> versions;
    private NativeList<int> freeHandles;

    public HandleManager(int capacity) {
        freeHandles = new NativeList<int>(capacity, Allocator.Persistent);
        versions = new NativeArray<int>(capacity, Allocator.Persistent);

        for(var i = capacity - 1; i > 0; i--) {
            freeHandles.Add(i);
        }
    }

    public void Dispose() {
        if(freeHandles.IsCreated) {
            freeHandles.Dispose();
        }

        if(versions.IsCreated) {
            versions.Dispose();
        }
    }

    public bool IsCreated() {
        return freeHandles.IsCreated && versions.IsCreated;
    }

    public Handle<T> Malloc() {
        if(freeHandles.Length > 0) {
            var index = freeHandles[^1];
            freeHandles.RemoveAt(freeHandles.Length - 1);

            return new Handle<T>() {
                index = index,
                version = versions[index],
            };
        }

        throw new Exception("Malloc Handle out of max");
    }

    public void Free(in Handle<T> handle) {
        if(!IsValid(handle)) {
            return;
        }

        // 释放 handle 时，增加 version
        if(handle.index >= 0 && handle.index < versions.Length) {
            versions[handle.index]++;
            freeHandles.Add(handle.index);
        }
        else {
            throw new Exception($"Free Invalid Handle {handle.index} {handle.version}");
        }
    }

    public bool IsValid(in Handle<T> handle) {
        if(handle.index > 0 && handle.index < versions.Length && versions[handle.index] == handle.version) {
            return true;
        }

        return false;
    }
}