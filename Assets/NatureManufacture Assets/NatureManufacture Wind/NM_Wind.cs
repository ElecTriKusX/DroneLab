using UnityEngine;

[ExecuteInEditMode]
public class NM_Wind : MonoBehaviour
{

    [Header("General Parameters")]
    [Tooltip("Wind Speed in Kilometers per hour")]
    public float WindSpeed = 30;
    [Range(0.0f, 2.0f)]
    [Tooltip("Wind Turbulence in percentage of wind Speed")]
    public float Turbulence = 0.25f;


    [Header("Noise Parameters")]
    [Tooltip("Texture used for wind turbulence")]
    public Texture2D NoiseTexture;
    [Tooltip("Size of one world tiling patch of the Noise Texture, for bending trees")]
    public float FlexNoiseWorldSize = 175.0f;
    [Tooltip("Size of one world tiling patch of the Noise Texture, for leaf shivering")]
    public float ShiverNoiseWorldSize = 10.0f;

    [Header("Gust Parameters")]
    [Tooltip("Texture used for wind gusts")]
    public Texture2D GustMaskTexture;
    [Tooltip("Size of one world tiling patch of the Gust Texture, for leaf shivering")]
    public float GustWorldSize = 600.0f;

    [Tooltip("Wind Gust Speed in Kilometers per hour")]
    public float GustSpeed = 50;
    [Tooltip("Wind Gust Influence on trees")]
    public float GustScale = 1.0f;

    [Header("Wind Sherical")]
    [Tooltip("Wind Gust Influence on trees")]
    public WindZone point1;
    public WindZone point2;
    public WindZone point3;
    public WindZone point4;



    Vector4 pos1 = new Vector4();
    Vector4 pos2 = new Vector4();
    Vector4 pos3 = new Vector4();
    Vector4 pos4 = new Vector4();
    Vector4 radius = new Vector4();

    // Use this for initialization
    void Start()
    {
        ApplySettings();
    }

    // Update is called once per frame
    void Update()
    {
        ApplySettings();
    }

    void OnValidate()
    {
        ApplySettings();
    }

    void ApplySettings()
    {
        Shader.SetGlobalTexture("WIND_SETTINGS_TexNoise", NoiseTexture);
        Shader.SetGlobalTexture("WIND_SETTINGS_TexGust", GustMaskTexture);
        Shader.SetGlobalVector("WIND_SETTINGS_WorldDirectionAndSpeed", GetDirectionAndSpeed());
        Shader.SetGlobalFloat("WIND_SETTINGS_FlexNoiseScale", 1.0f / Mathf.Max(0.01f, FlexNoiseWorldSize));
        Shader.SetGlobalFloat("WIND_SETTINGS_ShiverNoiseScale", 1.0f / Mathf.Max(0.01f, ShiverNoiseWorldSize));
        Shader.SetGlobalFloat("WIND_SETTINGS_Turbulence", WindSpeed * Turbulence);
        Shader.SetGlobalFloat("WIND_SETTINGS_GustSpeed", GustSpeed);
        Shader.SetGlobalFloat("WIND_SETTINGS_GustScale", GustScale);
        Shader.SetGlobalFloat("WIND_SETTINGS_GustWorldScale", 1.0f / Mathf.Max(0.01f, GustWorldSize));



        if (point1 != null)
        {
            pos1 = new Vector4(point1.transform.position.x, point1.transform.position.y, point1.transform.position.z, point1.windMain * 0.2777f);
            radius[0] = point1.radius;

        }
        else
        {
            pos1 = new Vector4(0, 0, 0, 0);
            radius[0] = 0.1f;
        }

        if (point2 != null)
        {
            pos2 = new Vector4(point2.transform.position.x, point2.transform.position.y, point2.transform.position.z, point2.windMain * 0.2777f);
            radius[1] = point2.radius;

        }
        else
        {
            pos2 = new Vector4(0, 0, 0, 0);
            radius[1] = 0.1f;
        }
        if (point3 != null)
        {
            pos3 = new Vector4(point3.transform.position.x, point3.transform.position.y, point3.transform.position.z, point3.windMain * 0.2777f);
            radius[2] = point3.radius;

        }
        else
        {
            pos3 = new Vector4(0, 0, 0, 0);
            radius[2] = 0.1f;
        }
        if (point4 != null)
        {
            pos4 = new Vector4(point4.transform.position.x, point4.transform.position.y, point4.transform.position.z, point4.windMain * 0.2777f);
            radius[3] = point4.radius;

        }
        else
        {
            pos4 = new Vector4(0, 0, 0, 0);
            radius[3] = 0.1f;
        }




        Shader.SetGlobalMatrix("WIND_SETTINGS_Points", new Matrix4x4(pos1, pos2, pos3, pos4));
        Shader.SetGlobalVector("WIND_SETTINGS_Points_Radius", radius);


    }

    Vector4 GetDirectionAndSpeed()
    {
        Vector3 dir = transform.forward.normalized;
        return new Vector4(dir.x, dir.y, dir.z, WindSpeed * 0.2777f);
    }

}