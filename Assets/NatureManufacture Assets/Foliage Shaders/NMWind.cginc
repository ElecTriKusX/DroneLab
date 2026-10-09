#define FLT_EPSILON 1.192092896e-07 

#include "NMWindTouchRect.cginc"

sampler2D WIND_SETTINGS_TexNoise;
sampler2D WIND_SETTINGS_TexGust;

float _InitialBend;
float _Stiffness;
float _Drag;
float _ShiverDrag;
float _ShiverDirectionality;
float _WindNormalInfluence;
float4 _NewNormal;

float4 WIND_SETTINGS_WorldDirectionAndSpeed;
float WIND_SETTINGS_FlexNoiseScale;
float WIND_SETTINGS_ShiverNoiseScale;
float WIND_SETTINGS_Turbulence;
float WIND_SETTINGS_GustSpeed;
float WIND_SETTINGS_GustScale;
float WIND_SETTINGS_GustWorldScale;
float4x4 WIND_SETTINGS_Points;
float4 WIND_SETTINGS_Points_Radius;

uniform half _CullFarStart = -1;
uniform half _CullFarDistance = -1;

float PositivePow(float base, float power)
{
    return pow(max(abs(base), float(FLT_EPSILON)), power);
}

float AttenuateTrunk(float x, float s)
{
    float r = (x / s);
    return PositivePow(r, 1 / s);
}


float3 Rotate(float3 pivot, float3 position, float3 rotationAxis, float angle)
{
    rotationAxis = normalize(rotationAxis);
    float3 cpa = pivot + rotationAxis * dot(rotationAxis, position - pivot);
    return cpa + ((position - cpa) * cos(angle) + cross(rotationAxis, (position - cpa)) * sin(angle));
}

struct WindData
{
    float3 Direction;
    float Strength;
    float3 ShiverStrength;
    float3 ShiverDirection;
    float Gust;
};


float3 texNoise(float3 worldPos, float LOD)
{
    return tex2Dlod(WIND_SETTINGS_TexNoise, float4(worldPos.xz, 0, LOD)).xyz - 0.5;
}

float texGust(float3 worldPos, float LOD)
{
    return tex2Dlod(WIND_SETTINGS_TexGust, float4(worldPos.xz, 0, LOD)).x;
}

float4 PointDirection(float4 collumn, float radius)
{
    
    float3 position = UNITY_MATRIX_M._m03_m13_m23;
    
    float3 direction = position - collumn.rgb;
    float3 norm = normalize(direction);
    float leng = length(direction);
    leng = clamp(leng / radius, 0, 1);
    leng = lerp(collumn.a, 0, leng);
    norm = norm * leng;
    
    return float4(norm.rgb, leng);

}

float4 MatrixSplit(float4x4 mat, float column)
{
    return float4(mat[0][column], mat[1][column], mat[2][column], mat[3][column]);
    
}


WindData GetAnalyticalWind(float3 WorldPosition, float3 PivotPosition, float drag, float shiverDrag, float initialBend, float4 time)
{
    WindData result;
    
    
    float4 newDirection = PointDirection(MatrixSplit(WIND_SETTINGS_Points, 0), WIND_SETTINGS_Points_Radius[0]);
    newDirection += PointDirection(MatrixSplit(WIND_SETTINGS_Points, 1), WIND_SETTINGS_Points_Radius[1]);
    newDirection += PointDirection(MatrixSplit(WIND_SETTINGS_Points, 2), WIND_SETTINGS_Points_Radius[2]);
    newDirection += PointDirection(MatrixSplit(WIND_SETTINGS_Points, 3), WIND_SETTINGS_Points_Radius[3]);
   
    
    float4 WIND_SETTINGS_WorldDirectionAndSpeednew = WIND_SETTINGS_WorldDirectionAndSpeed + newDirection;
    
    float3 normalizedDir = normalize(WIND_SETTINGS_WorldDirectionAndSpeednew.xyz);
    

    float3 worldOffset = float3(1, 0, 0) * WIND_SETTINGS_WorldDirectionAndSpeednew.w * time.y;
    float3 gustWorldOffset = float3(1, 0, 0) * WIND_SETTINGS_GustSpeed * time.y;

	// Trunk noise is base wind + gusts + noise

    float3 trunk = float3(0, 0, 0);

    if (WIND_SETTINGS_WorldDirectionAndSpeednew.w > 0.0 || WIND_SETTINGS_Turbulence > 0.0)
    {
        trunk = texNoise((PivotPosition - worldOffset) * WIND_SETTINGS_FlexNoiseScale, 3);
    }

    float gust = 0.0;

    if (WIND_SETTINGS_GustSpeed > 0.0)
    {
        gust = texGust((PivotPosition - gustWorldOffset) * WIND_SETTINGS_GustWorldScale, 3);
        gust = pow(gust, 2) * WIND_SETTINGS_GustScale;
    }

    float3 trunkNoise =
		(
		(normalizedDir * WIND_SETTINGS_WorldDirectionAndSpeednew.w)
			+ (gust * normalizedDir * WIND_SETTINGS_GustSpeed)
			+ (trunk * WIND_SETTINGS_Turbulence)
			) * drag;

	// Shiver Noise
    float3 shiverNoise = texNoise((WorldPosition - worldOffset) * WIND_SETTINGS_ShiverNoiseScale, 0) * shiverDrag * WIND_SETTINGS_Turbulence;

    float3 dir = trunkNoise;
    float flex = length(trunkNoise) + initialBend;
    float shiver = length(shiverNoise);

    result.Direction = dir;
    result.ShiverDirection = shiverNoise;
    result.Strength = flex;
    result.ShiverStrength = shiver + shiver * gust;
    result.Gust = (gust * normalizedDir * WIND_SETTINGS_GustSpeed)
		+ (trunk * WIND_SETTINGS_Turbulence);

    return result;
}



void ApplyWindDisplacement(inout float3 positionWS,
	inout WindData windData,
	float3 normalWS,
	float3 rootWP,
	float stiffness,
	float drag,
	float shiverDrag,
	float shiverDirectionality,
	float initialBend,
	float shiverMask,
	float4 time)
{
    WindData wind = GetAnalyticalWind(positionWS, rootWP, drag, shiverDrag, initialBend, time);

    if (wind.Strength > 0.0)
    {
        float att = AttenuateTrunk(distance(positionWS, rootWP), stiffness);
        float3 rotAxis = cross(float3(0, 1, 0), wind.Direction);

        positionWS = Rotate(rootWP, positionWS, rotAxis, (wind.Strength) * 0.001 * att);

        float3 shiverDirection = normalize(lerp(normalWS, normalize(wind.Direction + wind.ShiverDirection), shiverDirectionality));
        positionWS += wind.ShiverStrength * shiverDirection * shiverMask;
    }
    windData = wind;

}


float4x4 GetObjectToWorldMatrix()
{
    return unity_ObjectToWorld;
}

float4x4 GetWorldToObjectMatrix()
{
    return unity_WorldToObject;
}

float3 TransformObjectToWorld(float3 positionOS)
{
    return mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)).xyz;
}


float3 TransformObjectToWorldNormal(float3 normalOS)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
	return UnityObjectToWorldDir(normalOS);
#else
	// Normal need to be multiply by inverse transpose
	// mul(IT_M, norm) => mul(norm, I_M) => {dot(norm, I_M.col0), dot(norm, I_M.col1), dot(norm, I_M.col2)}
    return normalize(mul(normalOS, (float3x3) GetWorldToObjectMatrix()));
#endif
}

float3 TransformWorldToObject(float3 positionWS)
{
    return mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;
}






void vert(inout appdata_full v)
{
 

    float3 positionWS = TransformObjectToWorld(v.vertex.xyz);

    
    float distanceToCamera = length(positionWS - _WorldSpaceCameraPos.xyz);
    
    float cull = 1;
    if(_CullFarStart>0)
        cull = 1 - saturate((distanceToCamera -_CullFarStart) / _CullFarDistance);

    float3 rootWP = mul(GetObjectToWorldMatrix(), float4(0, 0, 0, 1)).xyz;



    float3 normalWS = TransformObjectToWorldNormal(v.normal);

    WindData windData;

    ApplyWindDisplacement(positionWS, windData, normalWS, rootWP, _Stiffness, _Drag, _ShiverDrag, _ShiverDirectionality, _InitialBend, v.color.a, _Time);

    v.vertex.xyz = TransformWorldToObject(positionWS).xyz * cull;
    

    if (_NewNormal.x != 0 || _NewNormal.y != 0 || _NewNormal.z != 0)
        v.normal *= _NewNormal;

    if (_WindNormalInfluence != 0)
        v.normal.y += -_WindNormalInfluence + windData.ShiverStrength * (_WindNormalInfluence + _WindNormalInfluence);

    v.color.r = windData.ShiverStrength;
    //v.color.g = cull;

    if (_TouchReactActive > 0)
        v.vertex.xyz += TouchReactAdjustVertex(half4(v.vertex.xyz, 0.0).xyz);


    

}

void AdditionalWind(inout appdata_full v)
{
    vert(v);
}