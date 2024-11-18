using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public static class RandomUtil {

    /// <summary>
    /// 实现目标选中后的概率递减
    /// </summary>
    /// <param name="targets"> 目标 ID 列表 </param>
    /// <param name="probDict"> 目标 ID 到随机次数的字典 </param>
    /// <param name="decreaseProp"> 递减概率 </param>
    /// <param name="minProp"> 最小概率，最小也要有值，默认 0.01f </param>
    /// <param name="maxProp"> 最大概率，递减的依据值，默认 1.0f </param>
    /// <returns> 返回最后 targets 的 Index </returns>
    public static int GetRandomIndex(List<int> targets, Dictionary<int, int> probDict, float decreaseProp = 0.33f, float minProp = 0.01f, float maxProp = 1.0f) {
        int result = 0;
        List<float> targetRandomCounts = new List<float>();

        float totalProp = 0;

        #region 如果 Dict 中存在 targets 都没有的元素，说明这次随机和这个元素无关，需要移除刷新体验

        List<int> removeKeys = new List<int>();
        foreach (var keyPair in probDict) {
            bool contain = false;
            foreach (var target in targets) {
                if (keyPair.Key == target) {
                    contain = true;
                }
            }

            if (!contain) {
                removeKeys.Add(keyPair.Key);
            }
        }

        foreach (var removeKey in removeKeys) {
            probDict.Remove(removeKey);
        }

        #endregion

        foreach (var item in targets) {
            if (!probDict.ContainsKey(item)) {
                probDict[item] = 1;
            }

            // 只有一个随机，不降概率
            if (probDict.Count == 1) {
                probDict[item] = 1;
            }

            float prop = maxProp;
            for (int i = 0; i < probDict[item] - 1; i++) {
                prop = Mathf.Clamp(prop - decreaseProp, minProp, maxProp);
            }
            Debug.Log($"[RandomUtil] ID ：{item} Prop : {prop}");
            targetRandomCounts.Add(prop);
            totalProp += prop;
        }

        float RandomPropNum = UnityEngine.Random.Range(0, totalProp);

        float curPropNum = 0;
        for (int i = 0; i < targetRandomCounts.Count; i++) {
            curPropNum += targetRandomCounts[i];
            if (RandomPropNum < curPropNum) {
                result = i;
                break;
            }
        }
        Debug.Log($"[RandomUtil] totalProp: {totalProp}; RandomPropNum: {RandomPropNum}; result: {result}");
        probDict[targets[result]] += 1;
        return result;
    }
}