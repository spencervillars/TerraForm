using UnityEngine;
using System.Collections;

public class ColorManager {

    static public float seaFloor = 0.1f;
    static public float sandHeight = seaFloor + 0.07f;
    static public float plainsHeight = 0.75f;
    static public float mountainHeight = plainsHeight + 0.2f;

    static Color seaColor = new Color(0,0.4f,0.8f);
    static Color sandColor = new Color(1,0.97f,0.61f);
    static Color plainsColor = new Color(0.199f, 0.597f, 0.199f);
    static Color mountainsColor = new Color(0.5f, 0.398f, 0.3f);
    static Color peakColor = new Color(1, 1, 1f);

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

}
