using System;
using System.Threading.Tasks;
using MergeSort;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Fluid : MonoBehaviour
{
    public Camera cam;
    private static readonly int _positions1 = Shader.PropertyToID("positions");
    private static readonly int _velocities1 = Shader.PropertyToID("velocities");
    private static readonly int _predictions1 = Shader.PropertyToID("predictions");
    private static readonly int _forceStrength = Shader.PropertyToID("mouseForceStrength");
    private static readonly int _pos = Shader.PropertyToID("inputPos");
    private static readonly int FoamVelocity = Shader.PropertyToID("foamVelocity");
    private static readonly int InteractState = Shader.PropertyToID("interactState");
    public ComputeShader compute;
    public ComputeShader sortCompute;
    private ComputeBuffer _positionsBuffer;
    private ComputeBuffer _velocitiesBuffer;
    private ComputeBuffer _densitiesBuffer;
    private ComputeBuffer _predictionsBuffer;
    private ComputeBuffer _startIndicesBuffer;
    private ComputeBuffer _spacialHashBuffer;
    private ComputeBuffer _spacialHashKeysBuffer;
    private ComputeBuffer _isLiquidHiddenBuffer;
    private ComputeBuffer _foamFactorsBuffer;
    private ComputeBuffer _spawnBudgetBuffer;
    public float gravity = -9.81f;
    public float width = 30;
    public float height = 30;
    public float damping = 0.8f;
    public float smoothRadius = 0.2f;
    public float targetDensity;
    public float pressureMultiplier = 1;
    public int particleCnt = 100;
    public float mouseForceStrength = 20;
    public float mouseForceRadius = 3;
    public float viscosity = 1;
    public float nearDensityMultiplier = 2;
    public float2 obstacleSize;
    public float2 obstaclePos;
    public float boundaryParticleDist = 0.2f;
    public RenderTexture boundaryTexture;
    public RenderTexture densityMap;
    public RenderTexture fluidDensityMap;

    public bool randomPos = false;
    public bool move = true;
    public ParticlesRenderer particlesRenderer;
    public float foamVelocity = 10;
    public int interactState;

    private int2[] _spacialHash;
    private int[] _startIndices;
    private int _gridCount;
    private Vector2Int _gridSize;
    private const int Prime1 = 12289;
    private float2[] _densities;
    public float delta=0.01667f;
    private float _sKernel;
    private float _sKernelSpiky;
    private float _sKernelDerivative;
    private float _sKernelSpikyDerivative;
    private float _vKernel;
    private float2[] _positions;
    private float2[] _velocities;
    private float2[] _predictions;
    private uint[] _hiddenStates;
    public float predictionStep = 0.4f;
    private Vector2 _inputPos;
    private BitonicMergeSort _sort;
    private float _mouseForceStrength;
    private int _boundaryParticleCnt;
    private uint2 _boundaryTextureSize;
    public uint[] _spawnBudget = new uint[]{1};
    
    private void OnDestroy()
    {
        _densitiesBuffer?.Release();
        _positionsBuffer?.Release();
        _velocitiesBuffer?.Release();
        _predictionsBuffer?.Release();
        _startIndicesBuffer?.Release();
        _spacialHashBuffer?.Release();
        _spacialHashKeysBuffer?.Release();
    }

    private void OnValidate()
    {
        /*if (isActiveAndEnabled && Application.isPlaying)
            Init();*/
    }

    public void Init()
    {
        _sort = new BitonicMergeSort(sortCompute);
        _positionsBuffer = new ComputeBuffer(particleCnt, sizeof(float) * 2);
        _velocitiesBuffer = new ComputeBuffer(particleCnt, sizeof(float) * 2);
        _densitiesBuffer = new ComputeBuffer(particleCnt, sizeof(float) * 2);
        _predictionsBuffer = new ComputeBuffer(particleCnt, sizeof(float) * 2);
        _startIndicesBuffer = new ComputeBuffer(particleCnt, sizeof(int));
        _spacialHashBuffer = new ComputeBuffer(particleCnt, sizeof(int)*2);
        _spacialHashKeysBuffer = new ComputeBuffer(particleCnt, sizeof(int));
        _foamFactorsBuffer = new ComputeBuffer(particleCnt, sizeof(float));
        _isLiquidHiddenBuffer = new ComputeBuffer(particleCnt, sizeof(int));
        _spawnBudgetBuffer = new ComputeBuffer(1, sizeof(int));
        
        _gridSize = new Vector2Int(Mathf.CeilToInt(width / smoothRadius), Mathf.CeilToInt(height / smoothRadius));
        _gridCount = _gridSize.x * _gridSize.y;
        _spacialHash = new int2[particleCnt];
        _startIndices = new int[particleCnt];
        Time.fixedDeltaTime = delta;
        _positions = new float2[particleCnt];
        _velocities = new float2[particleCnt];
        _predictions = new float2[particleCnt];
        _densities = new float2[particleCnt];
        _hiddenStates = new uint[particleCnt];
        float x = width/2-width/8, y = height/5;
        float step = Mathf.Sqrt((width/4)*(height/2) / (particleCnt));
        _boundaryTextureSize = new uint2((uint)boundaryTexture.width, (uint)boundaryTexture.height);

        for (int i = _boundaryParticleCnt; i < particleCnt; i++)
        {
            if (randomPos)
            {
                x = Random.Range(0, width);
                y = Random.Range(0, height);
            }
            _positions[i] = new float2(x, y);
            _velocities[i] = new float2(0, 0);
            _predictions[i] = new float2(x,y);
            _hiddenStates[i] = 0;
            x += step;
            if (x > width / 2+width/8)
            {
                x = width/2-width/8;
                y += step;
            }
        }
        for (int i = 0; i < 8; i++)
        {
            compute.SetBuffer(i, _positions1, _positionsBuffer);
            compute.SetBuffer(i, _velocities1, _velocitiesBuffer);
            compute.SetBuffer(i, _predictions1, _predictionsBuffer);
            compute.SetBuffer(i, "densities", _densitiesBuffer);
            compute.SetBuffer(i,"startIndices", _startIndicesBuffer);
            compute.SetBuffer(i,"spacialHash", _spacialHashBuffer);
            compute.SetBuffer(i,"spacialHashKeys", _spacialHashKeysBuffer);
            compute.SetBuffer(i,"isLiquidHidden", _isLiquidHiddenBuffer);
        }
        _sKernel = 6 / (Mathf.PI * Mathf.Pow(smoothRadius, 4));
        _sKernelSpiky = 10 / (Mathf.PI * Mathf.Pow(smoothRadius, 5));
        _sKernelDerivative = 12 / (Mathf.PI * Mathf.Pow(smoothRadius, 4));
        _sKernelSpikyDerivative = 30 / (Mathf.PI * Mathf.Pow(smoothRadius, 5));
        _vKernel =  4/(Mathf.PI * Mathf.Pow(smoothRadius, 8));
        compute.SetInt("gridCount", _gridCount);
        compute.SetInts("gridSize", _gridSize.x, _gridSize.y);
        compute.SetFloat("_sKernel", _sKernel);
        compute.SetFloat("mouseForceRadius", mouseForceRadius);
        compute.SetFloat("_sKernelSpiky", _sKernelSpiky);
        compute.SetFloat("_sKernelDerivative", _sKernelDerivative);
        compute.SetFloat("_sKernelSpikyDerivative", _sKernelSpikyDerivative);
        compute.SetFloat("_vKernel", _vKernel);
        compute.SetFloat("delta",delta);
        compute.SetFloat("predictionStep",predictionStep);
        compute.SetFloat("smoothRadius",smoothRadius);
        compute.SetInt("Prime1",Prime1);
        compute.SetInt("particleCnt",particleCnt);
        compute.SetFloat("targetDensity",targetDensity);
        compute.SetFloat("pressureMultiplier",pressureMultiplier);
        compute.SetFloat("nearDensityMultiplier",nearDensityMultiplier);
        compute.SetFloat("viscosity",viscosity);
        compute.SetFloat("width",width);
        compute.SetFloat("height",height);
        compute.SetFloat("damping",damping);
        compute.SetFloat("gravity", gravity);
        compute.SetFloats("obstacleSize", obstacleSize.x, obstacleSize.y);
        compute.SetFloats("obstaclePos", obstaclePos.x, obstaclePos.y);
        compute.SetInt("boundaryParticleCnt", _boundaryParticleCnt);
        compute.SetTexture(1,"boundaryTexture", boundaryTexture);
        compute.SetTexture(2,"boundaryTexture", boundaryTexture);
        compute.SetTexture(3,"boundaryTexture", boundaryTexture);
        compute.SetTexture(7,"boundaryTexture", boundaryTexture);
        compute.SetInts("boundaryTextureSize", (int)_boundaryTextureSize.x, (int)_boundaryTextureSize.y);
        compute.SetTexture(1,"densityMap", densityMap);
        compute.SetTexture(2,"densityMap", densityMap);
        compute.SetTexture(1,"densityMapTex", densityMap);
        compute.SetTexture(2,"densityMapTex", densityMap);
        compute.SetTexture(7,"densityMapTex", densityMap);
        compute.SetTexture(7,"densityMap", densityMap);
        compute.SetTexture(7,"fluidDensityMap", fluidDensityMap);
        compute.SetFloat("boundaryParticleDist",boundaryParticleDist);
        compute.SetBuffer(2,"foamFactors", _foamFactorsBuffer);
        compute.SetBuffer(7,"foamFactors", _foamFactorsBuffer);
        compute.SetBuffer(0, "spawnBudget", _spawnBudgetBuffer);
        _positionsBuffer.SetData(_positions);
        _velocitiesBuffer.SetData(_velocities);
        _densitiesBuffer.SetData(_densities);
        _predictionsBuffer.SetData(_predictions);
        _isLiquidHiddenBuffer.SetData(_hiddenStates);
        _spawnBudgetBuffer.SetData(_spawnBudget);
        particlesRenderer.Init(_positionsBuffer, _velocitiesBuffer, particleCnt, _foamFactorsBuffer);
    }

    private void Update()
    {
        _inputPos = cam.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButton(0))
        {
            _mouseForceStrength = mouseForceStrength;
        }
        else if (Input.GetMouseButton(1))
        {
            _mouseForceStrength = -mouseForceStrength;
        }
        else
        {
            _mouseForceStrength = 0;
        }
    }

    public void SimulateFluid()
    {
        if (!move) return;
        compute.SetFloat(FoamVelocity, foamVelocity);
        compute.SetInt(InteractState, interactState);
        compute.Dispatch(7, 
            Mathf.CeilToInt(width/boundaryParticleDist/8), 
            Mathf.CeilToInt(height/boundaryParticleDist/8), 1);
        compute.SetFloat(_forceStrength, _mouseForceStrength);
        if (_mouseForceStrength != 0)
        {
            compute.SetFloats(_pos, _inputPos.x, _inputPos.y);
            _spawnBudgetBuffer.SetData(_spawnBudget);
        }
        compute.Dispatch(0, particleCnt/64, 1, 1);
        //sort
        UpdateSpacialHashOnGpu();
        compute.Dispatch(1, particleCnt/64, 1, 1);
        compute.Dispatch(2, particleCnt/64, 1, 1);
        compute.Dispatch(3, particleCnt/64, 1, 1);
    }

    private void UpdateSpacialHashOnGpu()
    {
        compute.Dispatch(4, particleCnt/64, 1, 1);
        _sort.Sort(_spacialHashKeysBuffer, _spacialHashBuffer);
        compute.Dispatch(5, particleCnt/64, 1, 1);
        compute.Dispatch(6, particleCnt/64, 1, 1);
    }
}
