
Shader "Debug Terrain Carve"
{

	SubShader
	{
		Pass
		{
	
			Tags{  "Queue" = "Geometry-10"  }
			ZTest Always
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#include "UnityCG.cginc"


			struct v2g
			{
				half3 wNormal : TEXCOORD0;
				float4 pos : SV_POSITION;
				float4 wpos : TEXCOORD1;
				float4 color : COLOR;
			};

			struct g2f
			{
				float4 color : COLOR;
				half3 wNormal : TEXCOORD2;
				float4 pos : SV_POSITION;
				float4 wpos : TEXCOORD0;
				float4 dist : TEXCOORD1;
			};


			v2g  vert(float4 vertex : POSITION, float3 normal : NORMAL, float4 color : COLOR)
			{
				v2g o;
				o.pos = UnityObjectToClipPos(vertex);
				o.wpos = mul(unity_ObjectToWorld, vertex);
				o.wNormal = UnityObjectToWorldNormal(normal);
				o.color = color;
				return o;
			}

			[maxvertexcount(3)]
			void geom(triangle v2g i[3], inout TriangleStream<g2f> outStream)
			{
				float2 p0 = i[0].pos.xy / i[0].pos.w;
				float2 p1 = i[1].pos.xy / i[1].pos.w;
				float2 p2 = i[2].pos.xy / i[2].pos.w;

				float2 edge0 = p2 - p1;
				float2 edge1 = p2 - p0;
				float2 edge2 = p1 - p0;

				float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);

				g2f o;
				o.wpos = i[0].wpos;
				o.pos = i[0].pos;
				o.dist.xyz = float3((area / length(edge0)), 0.0, 0.0) * o.pos.w * 750;
				o.dist.w = 1.0 / o.pos.w;
				o.wNormal = i[0].wNormal;
				o.color = i[0].color;
				outStream.Append(o);

				o.wpos = i[1].wpos;
				o.pos = i[1].pos;
				o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.pos.w * 750;
				o.dist.w = 1.0 / o.pos.w;
				o.wNormal = i[1].wNormal;
				o.color = i[1].color;
				outStream.Append(o);

				o.wpos = i[2].wpos;
				o.pos = i[2].pos;
				o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.pos.w * 750;
				o.dist.w = 1.0 / o.pos.w;
				o.wNormal = i[2].wNormal;
				o.color = i[2].color;			

				outStream.Append(o);
			}

			fixed4 frag(g2f i) : SV_Target
			{

				float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];

				if (minDistanceToEdge > 0.9)
				{				
					fixed4 c = 0;
					c.g = i.wNormal*0.5 + 0.5;
					c.a = i.color.a;
					return c;
				}else
					return fixed4(1,1,1,i.color.a);
							   			


			}
			ENDCG
		}
	}
}