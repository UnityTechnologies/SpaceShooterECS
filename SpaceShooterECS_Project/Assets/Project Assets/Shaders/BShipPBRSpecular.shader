// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "FORGE3D/Battleships/Ship PBR (Specular)"
{
	Properties
	{
		_Tint("Tint", Color) = (0.5294118,0.5294118,0.5294118,1)
		_Albedo("Albedo", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_SpecularSmoothness("SpecularSmoothness", 2D) = "white" {}
		_Specular("Specular", Range( 0.03 , 1)) = 0.03
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_NormalScale("NormalScale", Range( 0 , 1)) = 0
		_Mask("Mask", 2D) = "black" {}
		_Windows("Windows", Color) = (1,0,0,1)
		_TeamColor("TeamColor", Color) = (1,0,0,1)
		_BlinkLights("BlinkLights", Color) = (1,0,0,1)
		_Exhaust("Exhaust", Color) = (1,0,0,1)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#pragma surface surf StandardSpecular keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float _NormalScale;
		uniform sampler2D _Normal;
		uniform float4 _Normal_ST;
		uniform float4 _Tint;
		uniform sampler2D _Albedo;
		uniform float4 _Albedo_ST;
		uniform float4 _TeamColor;
		uniform sampler2D _Mask;
		uniform float4 _Mask_ST;
		uniform float4 _Windows;
		uniform float4 _Exhaust;
		uniform float4 _BlinkLights;
		uniform sampler2D _SpecularSmoothness;
		uniform float4 _SpecularSmoothness_ST;
		uniform float _Specular;
		uniform float _Smoothness;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv_Normal = i.uv_texcoord * _Normal_ST.xy + _Normal_ST.zw;
			float3 normalizeResult16 = normalize( UnpackScaleNormal( tex2D( _Normal, uv_Normal ) ,_NormalScale ) );
			o.Normal = normalizeResult16;
			float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
			float2 uv_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			float4 tex2DNode18 = tex2D( _Mask, uv_Mask );
			float4 lerpResult40 = lerp( ( _Tint * tex2D( _Albedo, uv_Albedo ) ) , _TeamColor , tex2DNode18.g);
			o.Albedo = lerpResult40.rgb;
			o.Emission = ( ( tex2DNode18.a * _Windows ) + ( tex2DNode18.r * _Exhaust * 100.0 ) + ( tex2DNode18.b * _BlinkLights * 1.0 ) ).rgb;
			float2 uv_SpecularSmoothness = i.uv_texcoord * _SpecularSmoothness_ST.xy + _SpecularSmoothness_ST.zw;
			float4 tex2DNode11 = tex2D( _SpecularSmoothness, uv_SpecularSmoothness );
			o.Specular = ( tex2DNode11 * _Specular ).rgb;
			o.Smoothness = ( tex2DNode11.a * _Smoothness );
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Standard (Specular setup)"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=14201
1927;83;1266;827;749.7067;356.2535;1.547575;True;False
Node;AmplifyShaderEditor.SamplerNode;18;-1016.29,474.896;Float;True;Property;_Mask;Mask;7;0;Create;None;b06cd176823445348822dda5f86f52e2;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;23;-393.3332,1024.234;Float;False;Property;_Exhaust;Exhaust;11;0;Create;1,0,0,1;0.338235,0.1835249,0.06466237,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;27;-543.4996,780.8998;Float;False;1;0;FLOAT;0.0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;-302.4568,1561.192;Float;False;Constant;_3;3;12;0;Create;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;32;-455.3587,1309.528;Float;False;Property;_BlinkLights;BlinkLights;10;0;Create;1,0,0,1;1,1,1,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;26;-561.0984,948.9009;Float;False;1;0;FLOAT;0.0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;35;-641.0962,1211.298;Float;False;1;0;FLOAT;0.0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;19;-386.8,802.7;Float;False;Property;_Windows;Windows;8;0;Create;1,0,0,1;0.2720585,0.7289044,1,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;42;-316.3009,1198.5;Float;False;Constant;_Float0;Float 0;12;0;Create;100;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-101.6,781.3;Float;False;2;2;0;FLOAT;0.0;False;1;COLOR;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-132.0974,1274.2;Float;False;3;3;0;FLOAT;0,0,0,0;False;1;COLOR;0;False;2;FLOAT;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;-110.3334,1005.434;Float;False;3;3;0;FLOAT;0.0;False;1;COLOR;0;False;2;FLOAT;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;3;-364.5,-467;Float;False;Property;_Tint;Tint;0;0;Create;0.5294118,0.5294118,0.5294118,1;1,1,1,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;9;-414.5,-294;Float;True;Property;_Albedo;Albedo;1;0;Create;None;d98975c4967526c4499aa8448c1f1971;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;6;10.1,405.8;Float;False;Property;_Smoothness;Smoothness;5;0;Create;0;0.749;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;4.5,291;Float;False;Property;_Specular;Specular;4;0;Create;0.03;1;0.03;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;11;-428.5,234;Float;True;Property;_SpecularSmoothness;SpecularSmoothness;3;0;Create;None;1db1287a74b354641bd87fc454e70cfd;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;25;242.0983,1022.499;Float;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-775.5,60;Float;False;Property;_NormalScale;NormalScale;6;0;Create;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;30;-3.624031,-181.1287;Float;False;Property;_TeamColor;TeamColor;9;0;Create;1,0,0,1;0.7647059,0.2249132,0.2249132,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;425.5,328;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0.0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;27.5,-317;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode;36;614.9006,886.4995;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;1;-423.5,21;Float;True;Property;_Normal;Normal;2;0;Create;None;d5b3df48c82488e4c92acca15c235191;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;419.5,221;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode;41;-278.1286,508.0993;Float;False;1;0;FLOAT;0.0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;40;601.3465,-305.4746;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0.0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode;39;736.4996,171.2999;Float;False;1;0;FLOAT;0.0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;28;651.7001,152.0997;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.NormalizeNode;16;-0.5,26;Float;False;1;0;FLOAT3;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WireNode;38;742.9001,118.4999;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1030.6,-22;Float;False;True;2;Float;ASEMaterialInspector;0;0;StandardSpecular;FORGE3D/Battleships/Ship PBR (Specular);False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;0;False;0;0;Opaque;0.5;True;True;0;False;Opaque;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;False;0;255;255;0;0;0;0;0;0;0;0;False;0;4;10;25;False;0.5;True;0;Zero;Zero;0;Zero;Zero;Add;Add;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;Standard (Specular setup);-1;-1;-1;-1;0;0;0;False;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.0;False;5;FLOAT;0.0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0.0;False;9;FLOAT;0.0;False;10;FLOAT;0.0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;27;0;18;4
WireConnection;26;0;18;1
WireConnection;35;0;18;3
WireConnection;20;0;27;0
WireConnection;20;1;19;0
WireConnection;33;0;35;0
WireConnection;33;1;32;0
WireConnection;33;2;43;0
WireConnection;24;0;26;0
WireConnection;24;1;23;0
WireConnection;24;2;42;0
WireConnection;25;0;20;0
WireConnection;25;1;24;0
WireConnection;25;2;33;0
WireConnection;15;0;11;4
WireConnection;15;1;6;0
WireConnection;10;0;3;0
WireConnection;10;1;9;0
WireConnection;36;0;25;0
WireConnection;1;5;17;0
WireConnection;12;0;11;0
WireConnection;12;1;5;0
WireConnection;41;0;18;2
WireConnection;40;0;10;0
WireConnection;40;1;30;0
WireConnection;40;2;41;0
WireConnection;39;0;15;0
WireConnection;28;0;36;0
WireConnection;16;0;1;0
WireConnection;38;0;12;0
WireConnection;0;0;40;0
WireConnection;0;1;16;0
WireConnection;0;2;28;0
WireConnection;0;3;38;0
WireConnection;0;4;39;0
ASEEND*/
//CHKSM=BF054FB0D53FBE5AECD56054B6C91366E151E83E