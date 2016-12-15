using UnityEngine;
using System.Collections;
using System.Threading;

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

public class TreeData
{
    public float rotation;
    public Vector3 up;
    public Vector3 position;
    public int treeType;

    public TreeData( Vector3 up, Vector3 position, float rotation, int type )
    {
        this.rotation = rotation;
        this.position = position;
        this.up = up;
        this.treeType = type;
    }

}

public class Cell {

    // Constants... Why can't you define these at the head in C#???
    public static string terrainShaderName = "Custom/StandardVertex";
    public static string terrainMaterialName = "Materials/Terrain";
    public static int[] ResolutionLevels = { 80,80,40,20,10 }; 

    public CellPos position;
    public int size;

    public LoadState loadState;
    public Object loadLock;

    public int Lod;
    public int currentResolution;
    public int requestingResolution;

    public TerrainData terrainData;
    public TreeData[] treeData;
    public float[] cachedNoiseMap;

    private GameObject terrainObject;
    private GameObject grassObject;
    private GameObject[] treeObjects;

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
        if ( Monitor.TryEnter(loadLock) )
        {
            try
            {
                if (cachedNoiseMap != null && this.loadState == LoadState.Requesting)
                {
                    FinishLoading();
                }
            }
            finally
            {
                Monitor.Exit(loadLock);
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
            GenerateGameObjects();
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

        lock (loadLock)
        {
            if (terrainObject != null)
            {
                MeshCollider collider = terrainObject.GetComponent<MeshCollider>();
                if (Lod == 0)
                    collider.sharedMesh = terrainObject.GetComponent<MeshFilter>().mesh;
                else
                    collider.sharedMesh = null;
            }
        }

        if (Lod <= 2)
        {
            GenerateGrassObject();
            GenerateTrees();
        }
        else
        {
            DestroyGrassObjects();
            DestroyTreeObjects();
        }

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
        GenerateGrassObject();
    }

    public void Unload()
    {
        lock (loadLock)
        {
            DestroyGameObjects();
            loadState = LoadState.Unloaded;
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

            float[] noisemap = NoiseGenerator.generate2DNoiseArray(position.x*size, position.y*size, ResolutionLevels[0] + 1, CellManager.GetCellManager().cellSize);

            cachedNoiseMap = noisemap;

            TerrainGenerator.GenerateTreeData(this);
        }
    }

    private void GenerateGameObjects()
    {
        GenerateTerrainObject();
        //GenerateTrees();
    }

    private void DestroyGameObjects()
    {
        lock (loadLock)
        {
            GameObject.Destroy(terrainObject);
            terrainObject = null;
            DestroyGrassObjects();
            DestroyTreeObjects();
        }
    }

    private void DestroyGrassObjects()
    {
        if (grassObject == null)
            return;

        GameObject.Destroy(grassObject);

        grassObject = null;
    }

    private void DestroyTreeObjects()
    {
        if (treeObjects == null)
            return;

        for (int i = 0; i < treeObjects.Length; i++)
            GameObject.Destroy(treeObjects[i]);

        treeObjects = null;
    }

    private void GenerateGrassObject()
    {
        lock (loadLock)
        {
            if (terrainData == null || grassObject != null || terrainObject == null || Lod > 1)
                return;
            grassObject = GrassObject.MakeGrassObject(terrainData.grass, terrainObject);
        }

    }

    private void GenerateTrees()
    {
        lock (loadLock)
        {
            if (treeData == null || treeObjects != null || terrainObject == null)
                return;
            treeObjects = new GameObject[treeData.Length];
            for ( int i = 0; i < treeData.Length; i++ )
            {
                MakeTreeObject(treeData[i], i);
            }
        }
    }

    private void MakeTreeObject(TreeData data, int offset)
    {
        if (treeObjects[offset] != null || data == null)
            return;

        string treename = "Tree " + data.treeType.ToString();
        GameObject tree = (GameObject)GameObject.Instantiate(Resources.Load(treename));
        //tree.GetComponent<Material>().color = new Color(0.64f,0.16f,0.16f);

        tree.transform.parent = terrainObject.transform;
        tree.transform.localPosition = data.position;
        tree.transform.up = (Vector3.up*2 + data.up)/3;
        tree.transform.Rotate(new Vector3(0, Random.Range(0, 180), 0));
        tree.transform.localScale = new Vector3(5,6,5);

        treeObjects[offset] = tree;
    }

    private void GenerateTerrainObject()
    {
        lock (loadLock)
        {
            if (terrainObject != null)
                return;
        }

        string gameObjectName = "CellTerrain_" + position.x.ToString() + "_" + position.y.ToString();

        GameObject terrain = new GameObject(gameObjectName);

        lock (loadLock)
        {
            if (terrainObject != null)
            {
                GameObject.Destroy(terrain);
                return;
            }
            this.terrainObject = terrain;
        }

        Material material = (Material)Resources.Load(terrainMaterialName, typeof(Material));

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
        MeshFilter filter;
        lock (loadLock)
        {
            if (terrainObject == null)
                return;
            filter = terrainObject.GetComponent<MeshFilter>();
        }

        Mesh mesh = TerrainGenerator.TerrainToMesh(terrainData);
        filter.mesh = mesh;

        MeshCollider collider = terrainObject.GetComponent<MeshCollider>();
        if (Lod == 0)
            collider.sharedMesh = terrainObject.GetComponent<MeshFilter>().mesh;
        else
            collider.sharedMesh = null;
    }
}
