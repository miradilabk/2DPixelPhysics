using System;
using UnityEngine;

public class WorldPhysicsManager : MonoBehaviour
{
    public bool update = true;
    public Fluid fluid;
    public PixelPhysicsManagerGPU pixelPhysicsManagerGPU;
    public GasSimulation gasSimulation;
    private void Start()
    {
        pixelPhysicsManagerGPU.Init();
        fluid.Init();
        gasSimulation.Init();
    }

    private void FixedUpdate()
    {
        if (!update) return;
        pixelPhysicsManagerGPU.SimulatePixels();
        fluid.SimulateFluid();
        gasSimulation.SimulateGas();
    }
}
