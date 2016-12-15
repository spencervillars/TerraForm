using UnityEngine;
using System.Collections;

public class MeshManager {

    static Mesh twoSidedQuad = null;
    static Mesh grassMesh = null;
    static Object meshLock = new Object();

    public static Mesh TwoSidedQuad()
    {

        lock (meshLock)
        {
            if (twoSidedQuad != null)
                return twoSidedQuad;
        }

        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f,0,0),
            new Vector3(0.5f,0,0),
            new Vector3(0.5f,1,0),
            new Vector3(-0.5f,1,0)
        };

        mesh.triangles = new int[]
        {
            0,1,2,
            0,2,3,
            2,1,0,
            3,2,0
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(1,1),
            new Vector2(0,1)
        };

        lock (meshLock)
        {
            twoSidedQuad = mesh;
        }

        return mesh;
    }

    public static Mesh GrassMesh( int numQuads )
    {

        lock (meshLock)
        {
            if (grassMesh != null)
                return grassMesh;
        }

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4 * numQuads];
        int[] triangles = new int[3 * 4 * numQuads];
        Vector2[] uv = new Vector2[4 * numQuads];

        int triangleCounter = 0;

        for (int i = 0; i < numQuads; i++ )
        {

            float rotation = i * (3.1416f / numQuads);
            float xPos = Mathf.Cos(rotation) / 2;
            float yPos = Mathf.Sin(rotation) / 2;

            vertices[i * 4 + 0] = new Vector3(-1 * xPos, 0, -1 * yPos);
            vertices[i * 4 + 1] = new Vector3(xPos, 0, yPos);
            vertices[i * 4 + 2] = new Vector3(xPos, 1, yPos);
            vertices[i * 4 + 3] = new Vector3(-1 * xPos, 1, -1 * yPos);

            triangles[triangleCounter++] = 0 + i*4; triangles[triangleCounter++] = 1 + i*4; triangles[triangleCounter++] = 2 + i*4;
            triangles[triangleCounter++] = 0 + i*4; triangles[triangleCounter++] = 2 + i*4; triangles[triangleCounter++] = 3 + i*4;
            triangles[triangleCounter++] = 2 + i*4; triangles[triangleCounter++] = 1 + i*4; triangles[triangleCounter++] = 0 + i*4;
            triangles[triangleCounter++] = 3 + i*4; triangles[triangleCounter++] = 2 + i*4; triangles[triangleCounter++] = 0 + i*4;

            uv[i * 4 + 0] = new Vector2(0, 0);
            uv[i * 4 + 1] = new Vector2(1, 0);
            uv[i * 4 + 2] = new Vector2(1, 1);
            uv[i * 4 + 3] = new Vector2(0, 1);

        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        mesh.RecalculateBounds();

        lock (meshLock)
        {
            grassMesh = mesh;
        }

        return mesh;
    }

}
