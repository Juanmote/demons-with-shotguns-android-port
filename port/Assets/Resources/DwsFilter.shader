// Port Android: filtros de imagen (scanlines, CRT) y paletas de color sobre todo el juego.
// Lo dibuja DwsFilters.cs con una cámara propia que va después de las del juego: el GrabPass
// coge la pantalla tal y como la han dejado y la vuelve a pintar filtrada.
Shader "Hidden/DwsFilter"
{
	Properties
	{
		_Mode ("Modo", Float) = 0
		_Lines ("Lineas", Float) = 270
		_R ("Fila R", Vector) = (1, 0, 0, 0)
		_G ("Fila G", Vector) = (0, 1, 0, 0)
		_B ("Fila B", Vector) = (0, 0, 1, 0)
	}
	SubShader
	{
		Tags { "Queue" = "Overlay" "RenderType" = "Overlay" "IgnoreProjector" = "True" }
		ZTest Always
		ZWrite Off
		Cull Off
		Lighting Off

		GrabPass { "_DwsGrab" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _DwsGrab;
			float4 _DwsGrab_TexelSize;
			float _Mode;
			float _Lines;
			float4 _R;
			float4 _G;
			float4 _B;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 grab : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.grab = ComputeGrabScreenPos(o.pos);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = i.grab.xy / i.grab.w;
				float3 c = tex2D(_DwsGrab, uv).rgb;
				// Paleta: matriz de color 3x3 más desplazamiento.
				c = float3(dot(c, _R.xyz) + _R.w, dot(c, _G.xyz) + _G.w, dot(c, _B.xyz) + _B.w);
				if (_Mode > 0.5)
				{
					// Scanlines: cada línea del juego se oscurece hacia sus bordes.
					float s = abs(frac(uv.y * _Lines) - 0.5) * 2.0;
					float depth = 0.45;
					if (_Mode > 1.5)
					{
						// CRT: rejilla de fósforo RGB por columnas de píxel y bordes algo más oscuros.
						float col = fmod(floor(uv.x * _DwsGrab_TexelSize.z), 3.0);
						float3 mask = float3(0.72, 0.72, 0.72);
						mask.r += step(col, 0.5) * 0.28;
						mask.g += step(0.5, col) * step(col, 1.5) * 0.28;
						mask.b += step(1.5, col) * 0.28;
						c *= mask * 1.22;
						float2 d = uv - 0.5;
						c *= 1.0 - dot(d, d) * 0.55;
						depth = 0.55;
						// Un poco de resplandor para compensar lo que se oscurece.
						c += c * c * 0.12;
					}
					c *= 1.0 - depth * s * s;
					c *= 1.1;
				}
				return fixed4(saturate(c), 1.0);
			}
			ENDCG
		}
	}
	Fallback Off
}
