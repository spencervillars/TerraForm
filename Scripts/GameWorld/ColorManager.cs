using UnityEngine;
using System.Collections;

public class ColorManager {

    public static float seaFloor = 0.1f;
    public static float sandHeight = seaFloor + 0.07f;
    public static float plainsHeight = 0.75f;
    public static float mountainHeight = plainsHeight + 0.2f;

    public static float blendDistance = 0.05f;

    static Color seaColor = new Color(1,0,0, 0.0f);//new Color(0,0.4f,0.8f);//
    static Color sandColor = new Color(1, 0, 0, 0.0f);//new Color(1,0.97f,0.61f);
    public static Color plainsColor = new Color(0, 1, 0, 0.0f);//new Color(0.199f, 0.597f, 0.199f);
    public static Color mountainsColor = new Color(0, 0, 1.0f, 0.0f);//new Color(0.5f, 0.398f, 0.3f);
    static Color peakColor = new Color(0, 0.0f, 0.0f, 1.0f);//new Color(1, 1, 1f);

    public static Color ColorFromNoise(float noise)
    {
        if (noise <= seaFloor)
            return seaColor;
        if (noise < sandHeight)
            return sandColor;
        if (noise < plainsHeight)
            return plainsColor;
        if (noise < mountainHeight)
            return mountainsColor;
        return peakColor;
    }

    public static Color ColorPoint( float x, float y, float noise)
    {

        if (noise > sandHeight + blendDistance)
            noise += Mathf.PerlinNoise(x / 100f, y / 100f) * 0.2f;

        Color color = ColorFromNoise(noise);

        return color;
    }

}
