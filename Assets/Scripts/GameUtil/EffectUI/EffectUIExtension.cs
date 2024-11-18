using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI特效的运行时功能扩展
/// </summary>
public static class EffectUIExtension
{
    /// <summary>
    /// 是否是UI默认材质
    /// </summary>
    /// <param name="material">材质</param>
    /// <returns>是否是</returns>
    public static bool IsUIDefaultMaterial(this Material material) {
        return material == Graphic.defaultGraphicMaterial || material == Image.defaultETC1GraphicMaterial;
    }
}