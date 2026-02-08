using Unity.Mathematics;
using UnityEngine;

public class ParticlesRenderer : MonoBehaviour
{
    public Gradient gradient;
    public uint particleCnt = 1000;
    private static readonly int _positionsBuffer = Shader.PropertyToID("PositionsBuffer");
    private static readonly int _velocitiesBuffer = Shader.PropertyToID("VelocitiesBuffer");
    private static readonly int _gradient = Shader.PropertyToID("gradient");
    public Material material;
    public Mesh mesh;

    GraphicsBuffer commandBuf;
    ComputeBuffer positionBuffer;
    ComputeBuffer velocityBuffer;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    private RenderParams rp;
    Texture2D gradientTexture;
    public void Init(ComputeBuffer pos, ComputeBuffer vel, int cnt, ComputeBuffer foamBuffer)
    {
        particleCnt = (uint)cnt;
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        positionBuffer = pos;
        velocityBuffer = vel;
        rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000*Vector3.one); // use tighter bounds for better FOV culling
        rp.matProps = new MaterialPropertyBlock();
        TextureFromGradient(ref gradientTexture, 128, gradient);
        rp.matProps.SetTexture(_gradient, gradientTexture);
        rp.matProps.SetBuffer(_positionsBuffer, positionBuffer);
        rp.matProps.SetBuffer(_velocitiesBuffer, velocityBuffer);
        rp.matProps.SetBuffer("FoamFactorsBuffer", foamBuffer);
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].instanceCount = particleCnt;
        commandBuf.SetData(commandData);
    }

    public static void TextureFromGradient(ref Texture2D texture, int width, Gradient gradient, FilterMode filterMode = FilterMode.Bilinear)
    {
        if (texture == null)
        {
            texture = new Texture2D(width, 1);
        }
        else if (texture.width != width)
        {
            texture.Reinitialize(width, 1);
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = filterMode;

        Color[] cols = new Color[width];
        for (int i = 0; i < cols.Length; i++)
        {
            float t = i / (cols.Length - 1f);
            cols[i] = gradient.Evaluate(t);
        }
        texture.SetPixels(cols);
        texture.Apply();
    }

    void OnDestroy()
    {
        commandBuf?.Release();
        commandBuf = null;
        positionBuffer?.Release();
        positionBuffer = null;
        velocityBuffer?.Release();
        velocityBuffer = null;
    }

    void Update()
    {
        if (particleCnt == 0) return;
        Graphics.RenderMeshIndirect(rp, mesh, commandBuf);
    }
}