using UnityEngine;
using System.Collections;

public struct CellPos
{
    public int x;
    public int y;
}

public enum LoadState
{
    Unloaded,
    Requesting,
    Loaded
}

public class Cell {

    // Constants... Why can't you define these at the head in C#???
    public static string terrainShaderName = "Custom/StandardVertex";
    public static int[] ResolutionLevels = { 60, 60, 30, 20, 10 }; 

    public CellPos position;
    public int size;

    public LoadState loadState;
    public Object loadLock;

    public int Lod;
    public int currentResolution;
    public int requestingResolution;

    public  TerrainData terrainData;
    public float[] cachedNoiseMap;

    private GameObject terrainObject;

    public Cell( CellPos pos, int size )
    {
        position = pos;

        currentResolution = -1;
        requestingResolution = -1;
        this.size = size;

        loadState = LoadState.Unloaded;
        loadLock = new Object();
        terrainObject = null;
    }

    public void RequestLoad()
    {
        lock (loadLock)
        {
            if (loadState != LoadState.Unloaded)
            {
                // Shouldn't ever happen??
                return;
            }

            loadState = LoadState.Requesting;

            RequestNoiseMap();
        }
    }

    public void UpdateLoadstatus()
    {
        lock (loadLock)
        {
            if (cachedNoiseMap != null && this.loadState == LoadState.Requesting)
            {
                FinishLoading();
            }
        }
    }

    public void FinishLoading()
    {
        lock (loadLock)
        {
            if (loadState != LoadState.Requesting)
            {
                // Also shouldn't happen?
                // I suppose it could happen where the cell is unloaded
                // before the load request finishes.
                return;
            }

            loadState = LoadState.Loaded;

            // Should this be in the lock?
            // It's like to take a LOT of time, but also
            // We really can't have an object unloaded while it's trying to make the gameobject....
            if (terrainObject == null)
                GenerateGameObject();
        }
    }

    public void LoadLod(int Lod)
    {
        if (Lod < 0)
            Lod = 0;
        if (Lod > ResolutionLevels.Length - 1)
            Lod = ResolutionLevels.Length - 1;
        this.Lod = Lod;

        int resolutionLevel = ResolutionLevels[Lod];

        if (currentResolution == resolutionLevel)
            return;

        currentResolution = resolutionLevel;
        requestingResolution = resolutionLevel;

        GenerateTerrainData();
    }

    public void TerrainCallback(TerrainData data)
    {
        this.terrainData = data;
        UpdateMesh();
    }

    public void Unload()
    {
        lock (loadLock)
        {
            loadState = LoadState.Unloaded;
            GameObject.Destroy(terrainObject);
            terrainObject = null;
            terrainData = null;
            cachedNoiseMap = null;
        }
    }

    public void RequestNoiseMap()
    {
        TerrainGenerator.RequestNoise(this.position);
    }

    public void GenerateNoiseMap()
    {
        lock(loadLock)
        {
            if (cachedNoiseMap != null)
                return;
        }

        float[] noisemap = NoiseGenerator.generate2DNoiseArray(position.x*size, position.y*size, ResolutionLevels[0] + 1, CellManager.GetCellManager().cellSize);

        lock(loadLock)
        {
            cachedNoiseMap = noisemap;
        }
    }

    private void GenerateGameObject()
    {
        string gameObjectName = "CellTerrain_" + position.x.ToString() + "_" + position.y.ToString();

        GameObject terrain = new GameObject(gameObjectName);
        this.terrainObject = terrain;

        Material material = new Material(Shader.Find(terrainShaderName));

        MeshFilter filter = terrain.AddComponent<MeshFilter>();
        MeshRenderer renderer = terrain.AddComponent<MeshRenderer>();
        MeshCollider collider = terrain.AddComponent<MeshCollider>();

        renderer.material = material;

        terrain.transform.position = new Vector3(position.x * size, 100, position.y * size);
        GenerateTerrainData();
    }

    private void GenerateTerrainData()
    {
        if (this.loadState != LoadState.Loaded)
            return;

        TerrainInput input;
        input.cellPosition = position;
        input.resolution = currentResolution;
        input.size = size;
        input.startPosition = new Vector3(position.x * size, 0, position.y * size);

        TerrainGenerator.RequestTerrain(input);
    }

    public void UpdateMesh()
    {
        Mesh mesh = TerrainGenerator.TerrainToMesh(terrainData);

        MeshFilter filter = terrainObject.GetComponent<MeshFilter>();
        MeshCollider collider = terrainObject.GetComponent<MeshCollider>();

        filter.mesh = mesh;

        if (currentResolution == ResolutionLevels[0])
            collider.sharedMesh = mesh;
        else
            collider.sharedMesh = null;
    }
}
