//旋涡
Shader "HT.SpecialEffects/UI/Vortex"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("使用透明度裁剪", Float) = 0
		_CenterX("旋涡中心横坐标", Range(0, 1)) = 0.5
		_CenterY("旋涡中心纵坐标", Range(0, 1)) = 0.5
		_Angle("旋涡角度", Range(0, 360)) = 1
		_Intensity("旋涡强度", Range(0, 1)) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Name "Default"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"
			#include "UIEffectsLib.cginc"

			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			sampler2D _MainTex;
			fixed4 _TextureSampleAdd;
			half _CenterX;
			half _CenterY;
			half _Angle;
			half _Intensity;

			fixed4 frag(FragData IN) : SV_Target
			{
				float2 uv = ApplyVortex(IN.texcoord, float2(_CenterX, _CenterY), _Intensity * 2000, _Angle);
				half4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * IN.color;
				
				#ifdef UNITY_UI_ALPHACLIP
				clip(color.a - 0.001);
				#endif
				
				return color;
			}
			ENDCG
		}
	}
}