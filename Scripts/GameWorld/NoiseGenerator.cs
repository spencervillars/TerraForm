using UnityEngine;
using System.Collections;

public static class NoiseGenerator {

    public static int octaves = 7;//Generates enough specificity
    public static float lacunarity = 3f;// Random values chosen
    public static float persistence = 0.27f;// Random good values
    public static float scale = 3500f;// Scale our map up by this amount.
    public static float roughnessScale = 2500f;

    public static float seed;
    public static float seed2;

    public static void Initialize()
    {
        seed = Random.Range(0, 1000000f);
        seed2 = Random.Range(0, 1000000f);
    }

    public static float Roughness( float x, float y )
    {
        float roughnessPosX = (x + seed2) / roughnessScale;
        float roughnessPosY = (y + seed2) / roughnessScale;

        float roughness = Mathf.PerlinNoise(roughnessPosX, roughnessPosY);
        return roughness;
    }

    public static float generateNoise( float x, float y )
    {
        float noiseValue = 0;
        float amplitude = 1f;
        float frequency = 1f;

        float roughness = Roughness(x, y);
        roughness = 1f - roughness * roughness;

        for (int i = 0; i < octaves; i++ )
        {
            float xPos = (x+seed) * frequency / scale;
            float yPos = (y+seed) * frequency / scale;

            noiseValue += (i<2 ? 1 : roughness * Mathf.Clamp(noiseValue,0,1)) * Mathf.PerlinNoise(xPos, yPos) * amplitude;

            frequency *= lacunarity;
            amplitude *= persistence;
        }

        //Curve our noise so we can get a better distribution
        noiseValue = applyCurve(noiseValue);

        return noiseValue;
    }

    // Takes in a starting point, the # of points to generate, and the step value
    public static float[] generate2DNoiseArray( float startX, float startY, int resolution, int size )
    {
        float[] noiseMap = new float[resolution * resolution];
        float step = ((float)size) / ((float)resolution - 1);

        for ( int x = 0; x < resolution; x++ )
        {
            for (int y = 0; y < resolution; y++ )
            {
                int position = x * resolution + y;

                float xPos = startX + step * x;
                float yPos = startY + step * y;

                float noiseValue = generateNoise(xPos, yPos);

                noiseMap[position] = noiseValue;
            }
        }

        return noiseMap;
    }

    public static float applyCurve(float inputValue)
    {
        return inputValue * inputValue * inputValue;
    }

}
