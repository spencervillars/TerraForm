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

public class GrassData
{
    public int grassCount
    {
        set
        {
            positions = new Vector2[value];

            vertices = new Vector3[value * 12];
            uvs = new Vector2[vertices.Length];
            normals = new Vector3[vertices.Length];

            triangles = new int[3 * 3 * 4 * value];
            _grasscount = value;
        }

        get
        {
            return _grasscount;
        }
    }

    private int _grasscount;

    public Vector2[] positions;
    public Vector3[] vertices;
    public Vector3[] normals;
    public Vector2[] uvs;
    public int[] triangles;

    public GrassData()
    {

    }

}

public class TerrainData
{
    public TerrainInput input;
    public Vector3[] vertices;
    public Vector2[] uvs;
    public Color[] colors;
    public int[] triangles;
    public GrassData grass;

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
        uvs = new Vector2[(input.resolution + 1) * (input.resolution + 1)];
        colors = new Color[vertices.Length];// 1 color per vertex
        triangles = new int[input.resolution * input.resolution * 2 * 3];
        grass = new GrassData();

    }
}

public class TerrainGenerator {

    public static int HeightScale = 1000;

    public static Queue<CellPos> noiseInputQueue = new Queue<CellPos>();
    public static Queue<TerrainInput> inputQueue = new Queue<TerrainInput>();
    public static Dictionary<CellPos,TerrainData> outputDictionary = new Dictionary<CellPos, TerrainData>();

    public static Thread[] TerrainThreads = null;
    public static int threadCount = 4;

    public static int GrassCount = 4000;
    public static int TreeCount = 25;

    static Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(b - a, c - a);
    }

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
        try
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

                    lock (outputDictionary)
                    {
                        outputDictionary[input.cellPosition] = data;
                    }
                }

                Thread.Sleep(20);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
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
            if (noiseMap == null)
                return null;
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
                data.uvs[position] = new Vector2(xPos, zPos) / input.size;

                Color color = ColorManager.ColorPoint(xPos + input.startPosition.x, zPos + input.startPosition.z, noiseMap[noiseMapPosition]);
                data.colors[position] = color;
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

        GrassData grassData = new GrassData();
        grassData.grassCount = (input.resolution >= Cell.ResolutionLevels[2]) ? GrassCount : 0;

        System.Random random = new System.Random(new System.DateTime().Millisecond);

        for (int i = 0; i < grassData.grassCount; i++)
        {
            float xPos = (float)random.NextDouble() * input.size;
            float zPos = (float)random.NextDouble() * input.size;

            int xCoord1 = Mathf.FloorToInt(xPos * input.resolution / input.size);
            int yCoord1 = Mathf.FloorToInt(zPos * input.resolution / input.size);

            xCoord1 = Mathf.Clamp(xCoord1, 0, input.resolution-1);
            yCoord1 = Mathf.Clamp(yCoord1, 0, input.resolution-1);

            int xCoord2 = xCoord1 + 1;
            int yCoord2 = yCoord1 + 1;

            bool x_or_y = (xPos - xCoord1 * step) > (zPos - yCoord2 * step);
            int xCoord3 = x_or_y ? xCoord2 : xCoord1;
            int yCoord3 = x_or_y ? yCoord1 : yCoord2;

            float yPos1 = data.vertices[xCoord1 * (input.resolution + 1) + yCoord1].y;
            float yPos2 = data.vertices[xCoord2 * (input.resolution + 1) + yCoord2].y;
            float yPos3 = data.vertices[xCoord3 * (input.resolution + 1) + yCoord3].y;
            
            Vector3 a = new Vector3(xCoord1 * step, yPos1, yCoord1 * step);
            Vector3 b = new Vector3(xCoord2 * step, yPos2, yCoord2 * step);
            Vector3 c = new Vector3(xCoord3 * step, yPos3, yCoord3 * step);

            Vector3 normal = x_or_y ? CalculateNormal(a,b,c) : CalculateNormal(a, c, b);
            normal.Normalize();

            float yPos = yPos = -1 * (normal.x * (xPos - a.x) + normal.z * (zPos - a.z) ) / normal.y + a.y; // formula for coordinate in a plane of 3 points with normal

            Vector3 position = new Vector3(xPos, yPos, zPos);
            position -= normal * 0.5f;//shrink it into the ground a bit more

            if (data.colors[xCoord1 * (input.resolution + 1) + yCoord1] == ColorManager.plainsColor)
                GrassObject.AddGrassToData(grassData, position, normal, i);
        }

        data.grass = grassData;

        return data;
    }

    public static Mesh TerrainToMesh( TerrainData data )
    {

        if (data == null)
            return null;

        Mesh mesh = new Mesh();
        mesh.vertices = data.vertices;
        mesh.colors = data.colors;
        mesh.triangles = data.triangles;
        mesh.uv = mesh.uv2 = mesh.uv3 = mesh.uv4 = data.uvs;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    public static void GenerateTreeData( Cell cell )
    {
        if (cell.cachedNoiseMap == null || cell.treeData != null)
            return;

        System.Random random = new System.Random(new System.DateTime().Millisecond);
        TreeData[] data = new TreeData[TreeCount];

        float cellSize = CellManager.GetCellManager().cellSize;
        int resolution = Cell.ResolutionLevels[0];

        float step = cellSize / resolution;

        for (int i = 0; i < TreeCount; i++)
        {
            float xPos = (float)random.NextDouble() * cellSize;
            float zPos = (float)random.NextDouble() * cellSize;

            int xCoord1 = Mathf.FloorToInt(xPos * resolution / cellSize);
            int yCoord1 = Mathf.FloorToInt(zPos * resolution / cellSize);

            xCoord1 = Mathf.Clamp(xCoord1, 0, resolution - 1);
            yCoord1 = Mathf.Clamp(yCoord1, 0, resolution - 1);

            int xCoord2 = xCoord1 + 1;
            int yCoord2 = yCoord1 + 1;

            bool x_or_y = (xPos - xCoord1 * step) > (zPos - yCoord2 * step);
            int xCoord3 = x_or_y ? xCoord2 : xCoord1;
            int yCoord3 = x_or_y ? yCoord1 : yCoord2;

            float yPos1 = cell.cachedNoiseMap[xCoord1 * (resolution + 1) + yCoord1] * HeightScale;
            float yPos2 = cell.cachedNoiseMap[xCoord2 * (resolution + 1) + yCoord2] * HeightScale;
            float yPos3 = cell.cachedNoiseMap[xCoord3 * (resolution + 1) + yCoord3] * HeightScale;

            Vector3 a = new Vector3(xCoord1 * step, yPos1, yCoord1 * step);
            Vector3 b = new Vector3(xCoord2 * step, yPos2, yCoord2 * step);
            Vector3 c = new Vector3(xCoord3 * step, yPos3, yCoord3 * step);

            Vector3 normal = x_or_y ? CalculateNormal(a, b, c) : CalculateNormal(a, c, b);
            normal.Normalize();

            float yPos = yPos = -1 * (normal.x * (xPos - a.x) + normal.z * (zPos - a.z)) / normal.y + a.y; // formula for coordinate in a plane of 3 points with normal

            Color color = ColorManager.ColorFromNoise(yPos / HeightScale);
            if ( (color == ColorManager.plainsColor || color == ColorManager.mountainsColor) )
            {

                Vector3 position = new Vector3(xPos, yPos, zPos);
                position -= normal * 0.5f;//shrink it into the ground a bit more

                TreeData treeData = new TreeData(normal, new Vector3(xPos, yPos, zPos), 0);
                data[i] = treeData;
            }
        }

        cell.treeData = data;
    }

}
