using FallShadow.Common;
using System;
using Unity.Collections;

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

        if(handle.index >= 0 && handle.index < versions.Length) {
            versions[handle.index]++;
            freeHandles.Add(handle.index);
        }
        else {
            throw new Exception($"Free Invalid Handle {handle.index} {handle.version}");
        }
    }

    public bool IsValid(in Handle<T> handle) {
        if(handle.index > 0 && handle.index < versions.Length
                             && versions[handle.index] == handle.version) {
            return true;
        }

        return false;
    }
}