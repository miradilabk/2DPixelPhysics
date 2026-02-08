using System;
using MergeSort;
using Unity.Mathematics;
using UnityEngine;

public class BitonicSort : MonoBehaviour
{
    public int[] arr = { 3, 7, 4, 8, 6, 2, 1, 5 };

    private BitonicMergeSort _sort;

    public ComputeBuffer arrayBuffer;
    public ComputeBuffer keysBuffer;
    public ComputeShader compute;
    
    void Start()
    {
        _sort = new BitonicMergeSort(compute);
        arrayBuffer = new ComputeBuffer(arr.Length, sizeof(int)*2);
        keysBuffer = new ComputeBuffer(arr.Length, sizeof(int));
        var array = new uint2[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            array[i] = new uint2((uint)arr[i], (uint)i);
        }
        _sort.Init(keysBuffer);
        arrayBuffer.SetData(array);
        
        _sort.Sort(keysBuffer, arrayBuffer);
        keysBuffer.GetData(arr);
        for (int i = 0; i < arr.Length; i++)
        {
            Debug.Log($"{array[arr[i]]}");
        }
    }

    private void OnDestroy()
    {
        if (arrayBuffer != null)
        {
            arrayBuffer.Dispose();
        }
    }
}
