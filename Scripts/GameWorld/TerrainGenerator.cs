using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

public struct TerrainInput
{
    public int resolution;
    public int size;
    public CellPos cellPosition;
    public Vector3 startPosition;
}

public class TerrainData
{
    public TerrainInput input;
    public Vector3[] vertices;
    public Vector2[] uvs;
    public Color[] colors;
    public int[] triangles;

    public TerrainData( TerrainInput input )
    {
        this.input = input;
        
        //
        //  If we want X squares per cell, we need X+1 vertices per side
        //  3 vertices = 2 squares
        //  . _ . _ .
        //  |   |   |
        //  . _ . _ .
        //  |   |   |
        //  . _ . _ .
        //

        vertices = new Vector3[(input.resolution+1)*(input.resolution+1)];
        colors = new Color[vertices.Length];// 1 color per vertex
        triangles = new int[input.resolution * input.resolution * 2 * 3];

    }
}

public class TerrainGenerator {

    public static int HeightScale = 1000;

    public static Queue<CellPos> noiseInputQueue = new Queue<CellPos>();
    public static Queue<TerrainInput> inputQueue = new Queue<TerrainInput>();
    public static Dictionary<CellPos,TerrainData> outputDictionary = new Dictionary<CellPos, TerrainData>();

    public static Thread[] TerrainThreads = null;
    public static int threadCount = 4;
    public static float textureScale = 100f;

    public static void EnsureThreads()
    {
        lock (inputQueue)
        {
            if (TerrainThreads == null)
            {
                TerrainThreads = new Thread[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    Thread thread = new Thread(new ThreadStart(MeshDataThread));
                    TerrainThreads[i] = thread;
                    thread.Start();
                }
            }
        }
    }

    public static void RequestTerrain(TerrainInput input)
    {
        lock(inputQueue)
        {
            EnsureThreads();
            inputQueue.Enqueue(input);
        }
    }

    public static void RequestNoise(CellPos pos)
    {
        lock(inputQueue)
        {
            EnsureThreads();
            noiseInputQueue.Enqueue(pos);
        }
    }

    public static void Update()
    {
        lock(outputDictionary)
        {
            if (outputDictionary.Count > 0)
            {
                List<CellPos> removables = new List<CellPos>();

                foreach(KeyValuePair<CellPos,TerrainData> pair in outputDictionary)
                {
                    removables.Add(pair.Key);

                    Cell cell = CellManager.GetCellManager().GetCellAtPosition(pair.Key);

                    if (cell == null)
                        continue;

                    cell.TerrainCallback(pair.Value);
                }

                foreach(CellPos pos in removables)
                {
                    outputDictionary.Remove(pos);
                }
            }
        }
    }

    public static void MeshDataThread()
    {
        while (true)
        {
            while (true)
            {
                CellPos input;
                lock (inputQueue)
                {
                    if (noiseInputQueue.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        input = noiseInputQueue.Dequeue();
                    }
                }

                Cell cell = CellManager.GetCellManager().GetCellAtPosition(input);
                if (cell != null)
                {
                    cell.GenerateNoiseMap();
                }
            }

            while (true)
            {
                TerrainInput input;
                lock (inputQueue)
                {
                    if (inputQueue.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        input = inputQueue.Dequeue();
                    }
                }

                TerrainData data = GenerateTerrain(input);

                lock(outputDictionary)
                {
                    outputDictionary[input.cellPosition] = data;
                }
            }

            Thread.Sleep(20);
        }
    }

    public static Mesh GenerateMesh( TerrainInput input )
    {
        TerrainData data = GenerateTerrain(input);
        return TerrainToMesh(data);
    }

    public static TerrainData GenerateTerrain( TerrainInput input )
    {
        Cell cell = CellManager.GetCellManager().GetCellAtPosition(input.cellPosition);
        if (cell == null)
        {
            // Should we output some error?
            return null;
        }

        TerrainData data = new TerrainData( input );
        float[] noiseMap;

        lock (cell.loadLock)
        {
            noiseMap = cell.cachedNoiseMap;
        }
    
        float step = ((float)input.size) / ((float)input.resolution);
        int resolutionFactor = Cell.ResolutionLevels[0]/ input.resolution; // Maximum resolution / requesting resolution

        // Let's first set our vertices.
        for (int x = 0; x <= input.resolution; x++)
        {
            for (int y = 0; y <= input.resolution; y++)
            {
                int position = x * (input.resolution + 1) + y;
                int noiseMapPosition = (x*resolutionFactor) * (input.resolution * resolutionFactor + 1) + y * resolutionFactor;

                float xPos = x * step;
                float yPos = input.startPosition.y + noiseMap[noiseMapPosition] * HeightScale;
                float zPos = y * step;

                data.vertices[position] = new Vector3(xPos, yPos, zPos);
                data.uvs[position] = textureScale * new Vector2(xPos, zPos) / input.size;
                data.colors[position] = ColorManager.ColorFromNoise(noiseMap[noiseMapPosition]);
            }
        }

        int counter = 0;
        for (int x = 0; x < input.resolution; x++ )
        {
            for (int y = 0; y < input.resolution; y++ )
            {
                //  Numbering our vertices...
                //  - - - y - - >
                //  |
                //  x
                //  |
                //  v
                //
                //  0 _ 1 _ 2
                //  |   |   |
                //  3 _ 4 _ 5
                //  |   |   |
                //  6 _ 7 _ 8
                //

                int topLeft = x * (input.resolution+1) + y;
                int topRight = x * (input.resolution + 1) + y + 1;

                int botLeft = (x+1) * (input.resolution + 1) + y;
                int botRight = (x+1) * (input.resolution + 1) + y + 1;

                // Top triangle
                // .___.
                // |  /
                // | /
                // |/
                // *

                //We want to go clockwise
                data.triangles[counter++] = topLeft;
                data.triangles[counter++] = topRight;
                data.triangles[counter++] = botLeft;

                // Bottom triangle
                //    .
                //   /|
                //  / |
                // .__.

                // We want to go clockwise
                data.triangles[counter++] = botLeft;
                data.triangles[counter++] = topRight;
                data.triangles[counter++] = botRight;
            }
        }

        return data;
    }

    public static Mesh TerrainToMesh( TerrainData data )
    {
        Mesh mesh = new Mesh();
        mesh.vertices = data.vertices;
        mesh.colors = data.colors;
        mesh.triangles = data.triangles;
        mesh.uv = mesh.uv2 = mesh.uv3 = mesh.uv4 = data.uvs;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

}
