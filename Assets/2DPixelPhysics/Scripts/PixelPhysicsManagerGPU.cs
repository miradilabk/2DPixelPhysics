using System;
using Unity.Mathematics;
using UnityEngine;

public class PixelPhysicsManagerGPU : MonoBehaviour
{
    private static readonly int DrawingPixel = Shader.PropertyToID("drawingPixelPos");
    public ComputeShader computeShader;
    public RenderTexture pixels;
    public Vector2Int groups;
    public Color[] powders;
    public Color[] solids;
    public int selectedItem;
    public bool draw = false;
    public SDFGenerator sdfGenerator;

    public void Init()
    {
        sdfGenerator.Init();
        computeShader.SetTexture(0, "Pixels", pixels);
        groups = new Vector2Int(Mathf.CeilToInt(pixels.width/8f),  Mathf.CeilToInt(pixels.height/8f));
        computeShader.SetInts("size", pixels.width, pixels.height);
        SimulatePixels();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            draw = !draw;
        }
    }

    public void SimulatePixels()
    {
        if (draw)
        {
            computeShader.SetFloat("time", Time.realtimeSinceStartup);
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                var pos = Input.mousePosition / new Vector2(Screen.width, Screen.height);
                int x = Mathf.RoundToInt(pos.x * pixels.width);
                int y = Mathf.RoundToInt(pos.y * pixels.height);
                computeShader.SetInts(DrawingPixel, x,y);
                if (Input.GetMouseButton(0))
                {
                    Color color;
                    if (selectedItem >= powders.Length)
                    {
                        color = solids[selectedItem-powders.Length];
                        color.a = -(float)(selectedItem - powders.Length+1) / (solids.Length+1);
                    }
                    else
                    {
                        color = powders[selectedItem];
                        color.a = (float)(selectedItem+1) / (powders.Length+1);
                    }
                    computeShader.SetFloats("pixelToDraw", color.r, color.g, color.b, color.a);
                }
                else
                {
                    computeShader.SetFloats("pixelToDraw", 0, 0, 0, 0);
                }
            }
            else
            {
                computeShader.SetInts(DrawingPixel, 10000,10000);
            }
        }
        
        computeShader.Dispatch(0,  groups.x, groups.y, 1);
        sdfGenerator.Generate();
    }
}
