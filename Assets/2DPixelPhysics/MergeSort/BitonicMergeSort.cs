using UnityEngine;
using System.Collections;

namespace MergeSort {
	public class BitonicMergeSort {
		private static readonly int _count = Shader.PropertyToID( "count");
		private static readonly int _dim = Shader.PropertyToID("dim");
		private static readonly int _block = Shader.PropertyToID("block");
		private static readonly int _arr = Shader.PropertyToID("arr");
		private static readonly int _keys = Shader.PropertyToID("keys");

		readonly ComputeShader _compute;
		readonly int _kernelSort;
		readonly int _kernelInit;

		public BitonicMergeSort(ComputeShader compute) {
			_compute = compute;
			_kernelSort = compute.FindKernel("BitonicSort");
			_kernelInit = compute.FindKernel("InitKeys");
		}

		public void Init(ComputeBuffer keys) {
			int x, y, z;
			ShaderUtil.CalcWorkSize(keys.count, out x, out y, out z);
			_compute.SetInt(_count, keys.count);
			_compute.SetBuffer(_kernelInit, _keys, keys);
			_compute.Dispatch(_kernelInit, x, y, z);
		}
		
		public void Sort(ComputeBuffer keys, ComputeBuffer arr) {
			var count = keys.count;
			int x, y, z;
			ShaderUtil.CalcWorkSize(count, out x, out y, out z);

			_compute.SetInt(_count, count);
			_compute.SetBuffer(_kernelSort, _arr, arr);
			_compute.SetBuffer(_kernelSort, _keys, keys);
			for (var dim = 2; dim <= count; dim <<= 1) {
				_compute.SetInt(_dim, dim);
				for (var block = dim >> 1; block > 0; block >>= 1) {
					_compute.SetInt(_block, block);
					_compute.Dispatch(_kernelSort, x, y, z);
				}
			}
		}
	}
}