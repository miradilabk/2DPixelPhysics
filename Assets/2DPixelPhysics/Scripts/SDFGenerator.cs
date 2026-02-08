using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class SDFGenerator : MonoBehaviour
{
    public RenderTexture sourceTexture;
    public ComputeShader computeShader;
    public RenderTexture _result;
    public int maxSteps = 0;
    private Texture2D _sdfTexture;
    private int _width;
    private int _height;

    public void Init()
    {
        _width = sourceTexture.width;
        _height = sourceTexture.height;
        computeShader.SetTexture(0, "Texture", sourceTexture);
        computeShader.SetTexture(2, "Texture", sourceTexture);
        computeShader.SetTexture(0,"Result", _result);
        computeShader.SetTexture(1,"Result", _result);
        computeShader.SetTexture(2,"Result", _result);
        Generate();
    }

    public void Generate()
    {
        int gx = Mathf.CeilToInt(_width / 8f);
        int gy = Mathf.CeilToInt(_height / 8f);
        computeShader.SetInts("size", _width,_height);
        computeShader.Dispatch(0, gx, gy, 1);
        int n = Mathf.NextPowerOfTwo(Mathf.Max(_width, _height));
        int s = 0;
        for (int i = n; i >= 1; i/=2)
        {
            if (s >= maxSteps)
            {
                break;
            }
            computeShader.SetInt("stepSize", i);
            computeShader.Dispatch(1, gx, gy, 1);
            s++;
        }
        computeShader.Dispatch(2, gx, gy, 1);
    }
}
