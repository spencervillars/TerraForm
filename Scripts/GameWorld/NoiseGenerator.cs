using UnityEngine;
using System.Collections;

public static class NoiseGenerator {

    public static int octaves = 7;// 7 Generates enough specificity
    public static float lacunarity = 2.2f;// 2.2f Random values chosen
    public static float persistence = 0.34f;// 0.34f Random good values
    public static float scale = 3500f;// 3500f Scale our map up by this amount.

    public static float seed;
    public static float seed2;

    public static void Initialize()
    {
        seed = Random.Range(0, 1000000f);
        seed2 = Random.Range(0, 1000000f);
    }

    public static float generatePerlinNoise( float x, float y )
         {
        float noiseValue = 0;
   float amplitude = 1f;
        float frequency = 1f;

        float roughnessPosX = (x + seed2) / scale;
        float roughnessPosY = (y + seed2) / scale;

        float roughness = Mathf.PerlinNoise(roughnessPosX, roughnessPosY);
        roughness = 1 - (roughness * roughness);

        for (int i = 0; i < octaves; i++ )
        {
            float xPos = (x+seed) * frequency / scale;
            float yPos = (y+seed) * frequency / scale;

            noiseValue += (i==0 ? 1 : roughness) * Mathf.PerlinNoise(xPos, yPos) * amplitude;

            frequency *= lacunarity;
            amplitude *= persistence;
        }

        //Curve our noise so we can get a better distribution
        noiseValue = applyCurve(noiseValue);

        return noiseValue;
    }

    public static void Square( float[] input, float[] noiseArray, int inputDimension, int x, int y, int size, float amplitude )
    {
        int positionMidpoint = x * inputDimension + y;

        int topLeft = (x - size / 2) * inputDimension + (y - size / 2);
        int topRight = (x - size / 2) * inputDimension + (y + size / 2);
        int botLeft = (x + size / 2) * inputDimension + (y - size / 2);
        int botRight = (x + size / 2) * inputDimension + (y + size / 2);

        input[positionMidpoint] = (input[topLeft] + input[topRight] + input[botLeft] + input[botRight]) / 4f + noiseArray[positionMidpoint] * amplitude;
    }

    public static void Diamond( float[] input, float[] noiseArray, int inputDimension, int x, int y, int size, float amplitude )
    {
        int positionMidpoint = x * inputDimension + y;

        int left, right, top, bottom;
        float sum = 0;
        int totalCount = 0;

        if (y - size / 2 >= 0)
        {
            left = (x) * inputDimension + (y - size / 2);
            sum += input[left];
            totalCount++;
        }

        if (y + size / 2 < inputDimension)
        {
            right = (x) * inputDimension + (y + size / 2);
            sum += input[right];
            totalCount++;
        }

        if (x - size / 2 >= 0)
        {
            top = (x - size / 2) * inputDimension + (y);
            sum += input[top];
            totalCount++;
        }

        if (x + size / 2 < inputDimension)
        {
            bottom = (x + size / 2) * inputDimension + (y);
            sum += input[bottom];
            totalCount++;
        }

        input[positionMidpoint] = sum / totalCount + noiseArray[positionMidpoint] * amplitude;
    }

    public static void DiamondSquare( float[] input, float[] noiseArray, int inputDimension, int size, float amplitude )
    {
        if (size == 1)
            return;

        for (int x = size / 2; x < inputDimension-1; x += size )
        {
            for (int y = size / 2; y < inputDimension-1; y += size )
            {
                Square(input, noiseArray, inputDimension, x, y, size, amplitude);
            }
        }

        for ( int x = 0; x <= inputDimension-1; x += size / 2 )
        {
            for (int y = (x + size/2)%size; y <= inputDimension-1; y += size)
            {
                Diamond(input, noiseArray, inputDimension, x, y, size, amplitude);
            }
        }

        DiamondSquare(input, noiseArray, inputDimension, size / 2, amplitude / 2 );
    }

    public static float[] generate2DNoiseArray( float startX, float startY, int resolution, int size )
    {
        return generate2DPerlinNoiseArray(startX, startY, resolution, size);
        float[] noiseMap = new float[resolution * resolution];
        float[] noiseArray = new float[resolution * resolution];
        float step = ((float)size) / ((float)resolution - 1);

        System.Random rand = new System.Random((int)System.DateTime.UtcNow.Ticks);
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int position = x * resolution + y;
                noiseArray[position] = (float)rand.NextDouble() - 0.5f;
            }
        }

        // First, set the corners.
        for (int x = 0; x < resolution; x += resolution-1)
        {
            for (int y = 0; y < resolution; y += resolution-1)
            {
                int position = x * resolution + y;

                float xPos = startX + step * x;
                float yPos = startY + step * y;

                float noiseValue = generatePerlinNoise(xPos, yPos);

                noiseMap[position] = noiseValue;
            }
        }

        DiamondSquare(noiseMap, noiseArray, resolution, resolution - 1, 0.4f );

        return noiseMap;
    }

    // Takes in a starting point, the # of points to generate, and the step value
    public static float[] generate2DPerlinNoiseArray( float startX, float startY, int resolution, int size )
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

                float noiseValue = generatePerlinNoise(xPos, yPos);

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
