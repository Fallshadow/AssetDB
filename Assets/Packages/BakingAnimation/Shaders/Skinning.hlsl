#ifndef SKINNING_INCLUDED
#define SKINNING_INCLUDED

uniform sampler2D _GPUSkinning_TextureMatrix;
uniform float3 _GPUSkinning_TextureSize_NumPixelsPerFrame;

UNITY_INSTANCING_BUFFER_START(GPUSkinningProperties0)
UNITY_DEFINE_INSTANCED_PROP(float4, _GPUSkinning_Extra_Property)
UNITY_DEFINE_INSTANCED_PROP(float2, _GPUSkinning_FrameIndex_PixelSegmentation)
#define _GPUSkinning_FrameIndex_PixelSegmentation_arr GPUSkinningProperties0
#if !defined(ROOTON_BLENDOFF) && !defined(ROOTOFF_BLENDOFF)
    UNITY_DEFINE_INSTANCED_PROP(float3, _GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade)
    #define _GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade_arr GPUSkinningProperties0
#endif
UNITY_INSTANCING_BUFFER_END(GPUSkinningProperties0)

inline float4 indexToUV(float index)
{
    int row = (int) (index / _GPUSkinning_TextureSize_NumPixelsPerFrame.x);
    float col = index - row * _GPUSkinning_TextureSize_NumPixelsPerFrame.x;
    return float4(col / _GPUSkinning_TextureSize_NumPixelsPerFrame.x, row / _GPUSkinning_TextureSize_NumPixelsPerFrame.y, 0, 0);
}

inline float getFrameStartIndex()
{
    float2 frameIndex_segment = UNITY_ACCESS_INSTANCED_PROP(_GPUSkinning_FrameIndex_PixelSegmentation_arr, _GPUSkinning_FrameIndex_PixelSegmentation);
    float segment = frameIndex_segment.y;
    float frameIndex = frameIndex_segment.x;
    float frameStartIndex = segment + frameIndex * _GPUSkinning_TextureSize_NumPixelsPerFrame.z;
    return frameStartIndex;
}

inline float4x4 getMatrix(int frameStartIndex, float boneIndex)
{
    float matStartIndex = frameStartIndex + boneIndex * 3;
    float4 row0 = tex2Dlod(_GPUSkinning_TextureMatrix, indexToUV(matStartIndex));
    float4 row1 = tex2Dlod(_GPUSkinning_TextureMatrix, indexToUV(matStartIndex + 1));
    float4 row2 = tex2Dlod(_GPUSkinning_TextureMatrix, indexToUV(matStartIndex + 2));
    float4 row3 = float4(0, 0, 0, 1);
    float4x4 mat = float4x4(row0, row1, row2, row3);
    return mat;
}
inline float4 Skinning(float4 positionOS, float4 texcoord1, float4 texcoord2)
{

    float frameStartIndex = getFrameStartIndex();
    float4x4 mat0 = getMatrix(frameStartIndex, texcoord1.x);
    float4x4 mat1 = getMatrix(frameStartIndex, texcoord1.z);
    float4x4 mat2 = getMatrix(frameStartIndex, texcoord2.x);
    float4x4 mat3 = getMatrix(frameStartIndex, texcoord2.z);

    return mul(mat0, positionOS) * texcoord1.y + mul(mat1, positionOS) * texcoord1.w;
}

#endif