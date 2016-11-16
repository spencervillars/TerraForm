using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GrassObject {

    static Vector3 scale = new Vector3(15, 7, 15);
    static int numQuads = 3;

    static Queue<GameObject> ReusableGrassQueue = new Queue<GameObject>();

    public static void RecycleGrass( GameObject grass )
    {
        if (grass == null)
            return;
        grass.transform.position = new Vector3(0,-999999f,0);
        ReusableGrassQueue.Enqueue(grass);
    }

    public static void AddGrassToData(GrassData data, Vector3 position, Vector3 up, int offset)
    {
        int start = numQuads * offset;
        for (int i = start; i < start + numQuads; i++)
        {

            float rotation = i * (3.1416f / (float)numQuads) + offset/(3.1415f*100);

            float xPos = Mathf.Cos(rotation) / 2;
            float yPos = Mathf.Sin(rotation) / 2;

            float xPosNormal = Mathf.Cos(rotation + 3.1416f * 0.5f);
            float yPosNormal = Mathf.Sin(rotation + 3.1416f * 0.5f);

            data.vertices[i * 4 + 0] = new Vector3(-1 * xPos * scale.x, 0, -1 * yPos * scale.z);
            data.vertices[i * 4 + 1] = new Vector3(xPos * scale.x, 0, yPos * scale.z);
            data.vertices[i * 4 + 2] = new Vector3(xPos * scale.x, 1 * scale.y, yPos * scale.z);
            data.vertices[i * 4 + 3] = new Vector3(-1 * xPos * scale.x, 1 * scale.y, -1 * yPos * scale.z);

            Quaternion q = new Quaternion();
            q.SetFromToRotation(Vector3.up, up);

            Matrix4x4 transform = Matrix4x4.TRS(Vector3.zero,q, Vector3.one);

            for (int j = 0; j < 4; j ++ )
            {
                data.vertices[i * 4 + j] = transform.MultiplyVector(data.vertices[i * 4 + j]) + position;
            }

            /*data.vertices[i * 4 + 4] = new Vector3(-1 * xPos * scale.x, 0, -1 * yPos * scale.z) + position;
            data.vertices[i * 4 + 5] = new Vector3(xPos * scale.x, 0, yPos * scale.z) + position;
            data.vertices[i * 4 + 6] = new Vector3(xPos * scale.x, 1 * scale.y, yPos * scale.z) + position;
            data.vertices[i * 4 + 7] = new Vector3(-1 * xPos * scale.x, 1 * scale.y, -1 * yPos * scale.z) + position;*/

            data.triangles[i * 12 + 0] = 0 + i * 4; data.triangles[i * 12 + 1] = 1 + i * 4; data.triangles[i * 12 + 2] = 2 + i * 4;
            data.triangles[i * 12 + 3] = 0 + i * 4; data.triangles[i * 12 + 4] = 2 + i * 4; data.triangles[i * 12 + 5] = 3 + i * 4;
            data.triangles[i * 12 + 6] = 2 + i * 4; data.triangles[i * 12 + 7] = 1 + i * 4; data.triangles[i * 12 + 8] = 0 + i * 4;
            data.triangles[i * 12 + 9] = 3 + i * 4; data.triangles[i * 12 + 10] = 2 + i * 4; data.triangles[i * 12 + 11] = 0 + i * 4;

            data.uvs[i * 4 + 0] = new Vector2(0, 0);
            data.uvs[i * 4 + 1] = new Vector2(1, 0);
            data.uvs[i * 4 + 2] = new Vector2(1, 1);
            data.uvs[i * 4 + 3] = new Vector2(0, 1);

            /*data.uvs[i * 4 + 4] = new Vector2(0, 0);
            data.uvs[i * 4 + 5] = new Vector2(1, 0);
            data.uvs[i * 4 + 6] = new Vector2(1, 1);
            data.uvs[i * 4 + 7] = new Vector2(0, 1);*/

            /*data.normals[i * 4 + 0] = new Vector3(xPosNormal, 1, yPosNormal);
            data.normals[i * 4 + 1] = new Vector3(xPosNormal, 1, yPosNormal);
            data.normals[i * 4 + 2] = new Vector3(xPosNormal, 1, yPosNormal);
            data.normals[i * 4 + 3] = new Vector3(xPosNormal, 1, yPosNormal);*/

            data.normals[i * 4 + 0] = up;// new Vector3(0, 1, 0);
            data.normals[i * 4 + 1] = up;// new Vector3(0, 1, 0);
            data.normals[i * 4 + 2] = up;// new Vector3(0, 1, 0);
            data.normals[i * 4 + 3] = up;// new Vector3(0, 1, 0);

            /*data.normals[i * 4 + 4] = new Vector3(-1 * xPosNormal, 1, -1 * yPosNormal);
            data.normals[i * 4 + 5] = new Vector3(-1 * xPosNormal, 1, -1 * yPosNormal);
            data.normals[i * 4 + 6] = new Vector3(-1 * xPosNormal, 1, -1 * yPosNormal);
            data.normals[i * 4 + 7] = new Vector3(-1 * xPosNormal, 1, -1 * yPosNormal);*/
        }
    }

    public static GameObject MakeGrassObject(GrassData data, GameObject parent)
    {
        GameObject grassObject = new GameObject("grass");

        grassObject.transform.parent = parent.transform;
        grassObject.transform.localPosition = Vector3.zero;

        Material material = (Material)Resources.Load("Materials/grass", typeof(Material));

        MeshFilter filter = grassObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = grassObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Mesh mesh = new Mesh();
        mesh.vertices = data.vertices;
        mesh.triangles = data.triangles;
        mesh.uv = data.uvs;
        mesh.normals = data.normals;

        filter.mesh = mesh;
        renderer.material = material;

        grassObject.layer = 8;

        return grassObject;
    }


    public static GameObject MakeGrassObjectSingle( Vector3 position, GameObject parent )
    {

        if (ReusableGrassQueue.Count > 0)
        {
            GameObject grass = ReusableGrassQueue.Dequeue();

            grass.transform.position = new Vector3(0, 0, 0);
            grass.transform.parent = parent.transform;
            grass.transform.localPosition = position;
            grass.transform.localScale = new Vector3(20, 10f, 20);

            return grass;
        }

        GameObject grassObject = new GameObject("grass");

        grassObject.transform.parent = parent.transform;
        grassObject.transform.localPosition = position;
        grassObject.transform.localScale = new Vector3(20,10,20);

        Material material = (Material)Resources.Load("Materials/grass", typeof(Material));

        MeshFilter filter = grassObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = grassObject.AddComponent<MeshRenderer>();

        Mesh mesh = MeshManager.GrassMesh(3);

        filter.mesh = mesh;
        renderer.material = material;

        return grassObject;
    }

}
