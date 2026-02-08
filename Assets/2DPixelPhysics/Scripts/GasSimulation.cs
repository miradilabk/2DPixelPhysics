using System;
using Unity.Mathematics;
using UnityEngine;

public class GasSimulation : MonoBehaviour
{
    private static readonly int RedOrBlack = Shader.PropertyToID("redOrBlack");
    public ComputeShader compute;
    public RenderTexture gas;
    public RenderTexture bufferGas;
    public RenderTexture fluidMap;
    public RenderTexture pixelsMap;
    public RenderTexture boundaryMap;
    public RenderTexture smokeMap;
    public float gridSize;
    public int2 size;
    public float overRelaxFactor;
    public int numIter = 30;
    private int2 group;
    private int2 halfGroup;
    public bool solve = true;
    public bool advect = true;
    public float smokeRadius = 5;
    public Color smokeColor;
    public bool draw;
    
    public void Init()
    {
        compute.SetFloat("dt", Time.fixedDeltaTime);
        compute.SetFloat("overRelaxFactor", overRelaxFactor);
        compute.SetFloat("gridSize", gridSize);
        compute.SetInts("size", size.x, size.y);
        for (int i = 0; i < 8; i++)
        {
            compute.SetTexture(i, "Gas", gas);
            compute.SetTexture(i, "BufferGas", bufferGas);
            compute.SetTexture(i, "BoundaryMap", boundaryMap);
        }
        compute.SetTexture(4, "SmokeMap", smokeMap);
        compute.SetTexture(6, "SmokeMap", smokeMap);
        compute.SetTexture(8, "fluidMap", fluidMap);
        compute.SetTexture(8, "pixelsMap", pixelsMap);
        compute.SetTexture(8, "BoundaryMap", boundaryMap);
        compute.SetTexture(8, "SmokeMap", smokeMap);
        
        group = new int2(Mathf.CeilToInt(size.x/8f), Mathf.CeilToInt(size.y/8f));

        halfGroup = new int2(Mathf.CeilToInt(size.x/16f), Mathf.CeilToInt(size.y/16f));

        compute.SetFloat("smokeRadius", smokeRadius);
        compute.Dispatch(0, group.x, group.y, 1);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O)) draw = !draw;
        if (draw)
        {
            if (Input.GetMouseButton(0))
            {
                var pos = Input.mousePosition / new Vector2(Screen.width, Screen.height);
                int x = Mathf.RoundToInt(pos.x * size.x);
                int y = Mathf.RoundToInt(pos.y * size.y);
                compute.SetFloats("smokeColor", smokeColor.r, smokeColor.g, smokeColor.b, smokeColor.a);
                compute.SetInts("smokePos", x,y);
            }
            else
            {
                compute.SetInts("smokePos", 10000,10000);
            }
        }
    }

    public void SimulateGas()
    {
        if (solve)
        {
            compute.Dispatch(8, group.x, group.y, 1);
            compute.Dispatch(6, group.x, group.y, 1);
            for (int i = 0; i < numIter; i++)
            {
                compute.SetInt(RedOrBlack, i%2);
                compute.Dispatch(1, halfGroup.x, group.y,1);
            }
            compute.Dispatch(2, group.x, group.y, 1);
            compute.Dispatch(7, group.x, group.y, 1);
        }

        if (advect)
        {
            compute.Dispatch(3, group.x, group.y, 1);
            compute.Dispatch(4, group.x, group.y, 1);
            compute.Dispatch(7, group.x, group.y, 1);
        }
    }
}
