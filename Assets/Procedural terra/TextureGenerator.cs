using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColorMap(Color[] colorMap,int width,int height)
    {
        Texture2D tex = new Texture2D(width,height);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(colorMap);
        tex.Apply();
        return tex;
    }

    public static Texture2D TextureFromHeight(float[,] HightMap)
    {
        int width = HightMap.GetLength(0);
        int height = HightMap.GetLength(1);

        Color[] colorMap = new Color[width*height];
        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < height; x++)
            {
                colorMap[y*width + x] = Color.Lerp(Color.black,Color.white,HightMap[x,y]);
            }
        }
        return TextureFromColorMap(colorMap,width,height);
    }
}
