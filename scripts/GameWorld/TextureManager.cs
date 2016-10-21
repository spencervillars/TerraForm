using UnityEngine;
using System.Collections;

public class TextureManager {

    public static Texture GrassBillboard;
    public static Object singletonLock = new Object();

    public static Texture GrassBillboardTexture()
    {
        lock (singletonLock)
        {
            if (GrassBillboard == null)
            {
                GrassBillboard = Resources.Load("Textures/grasstex") as Texture;
            }
        }

        return GrassBillboard;
    }


}
