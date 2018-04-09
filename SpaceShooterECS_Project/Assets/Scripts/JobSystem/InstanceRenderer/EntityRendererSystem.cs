using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Transforms;
using UnityEngine.Experimental.PlayerLoop;
using Unity.Jobs;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.ECS.Rendering;

namespace ECS_SpaceShooterDemo
{
    //[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.PreLateUpdate.ParticleSystemBeginUpdateAll))]
    [UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.PostLateUpdate.UpdateAllRenderers))]
    [UpdateAfter(typeof(ECS_SpaceShooterDemo.EntityToInstanceRendererTransform))]
    [UpdateAfter(typeof(ECS_SpaceShooterDemo.CollisionSystem))]
    public class EntityInstanceRendererSystem : GameControllerComponentSystem
	{
	    const int m_batchSize = 1023;
	    private const int m_matrixPtrCount = 40;

        // Instance renderer takes only batches of 1024
        Matrix4x4[] m_MatricesArray = new Matrix4x4[m_batchSize];

	    List<List<Matrix4x4[]>> m_MatricesArrayList = new List<List<Matrix4x4[]>>(2);

	    List<EntityInstanceRenderer> m_CacheduniqueRendererTypes = new List<EntityInstanceRenderer>(10);
	    ComponentGroup              m_InstanceRendererGroup;

        public unsafe static void CopyMatrices(ComponentDataArray<EntityInstanceRendererTransform> transforms, int beginIndex, int length, Matrix4x4[] outMatrices)
        {
	        // @TODO: This is only unsafe because the Unity DrawInstances API takes a Matrix4x4[] instead of NativeArray.
	        ///       And we also want the code to be really fast.
            fixed (Matrix4x4* matricesPtr = outMatrices)
            {
                UnityEngine.Assertions.Assert.AreEqual(sizeof(Matrix4x4), sizeof(EntityInstanceRendererTransform));
	            var matricesSlice = Unity.Collections.LowLevel.Unsafe.NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<EntityInstanceRendererTransform>(matricesPtr, sizeof(Matrix4x4), length);
	            #if ENABLE_UNITY_COLLECTIONS_CHECKS
	            Unity.Collections.LowLevel.Unsafe.NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref matricesSlice, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
	            #endif
                transforms.CopyTo(matricesSlice, beginIndex);
            }
        }

	    protected override void OnCreateManager(int capacity)
	    {
	        for (int i = 0; i < 2; i++)
	        {
	            m_MatricesArrayList.Add(new List<Matrix4x4[]>(m_matrixPtrCount));
	            for (int j = 0; j < m_matrixPtrCount; j++)
	            {
	                m_MatricesArrayList[i].Add(new Matrix4x4[m_batchSize]);
	            }
	        }

	        m_InstanceRendererGroup = GetComponentGroup(ComponentType.Subtractive(typeof(EntityPrefabData)), typeof(EntityInstanceRenderer), typeof(EntityInstanceRendererTransform));

	        base.OnCreateManager(capacity);
	    }

	    protected override void OnDestroyManager()
	    {
	        m_MatricesArrayList.Clear();
	        m_MatricesArrayList = null;
            base.OnDestroyManager();
	    }


	    [ComputeJobOptimizationAttribute(Accuracy.Med, Support.Relaxed)]
	    unsafe struct CopyMatricesJob : IJob
	    {
	        [NativeDisableUnsafePtrRestriction]
	        public Matrix4x4* matricesPtr;

	        [ReadOnly]
	        public ComponentDataArray<EntityInstanceRendererTransform> transforms;
	        public int beginIndex;
	        public int length;

	        public void Execute()
	        {
	            UnityEngine.Assertions.Assert.AreEqual(sizeof(Matrix4x4), sizeof(EntityInstanceRendererTransform));
	            var matricesSlice = Unity.Collections.LowLevel.Unsafe.NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<EntityInstanceRendererTransform>(matricesPtr, sizeof(Matrix4x4), length);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
	            Unity.Collections.LowLevel.Unsafe.NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref matricesSlice, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
	            transforms.CopyTo(matricesSlice, beginIndex);
	        }
	    }

	    void DrawPreviousJobs(JobHandle previousMatrixJobHandle, int previousFilledMatrixJobIndex, int previousJobCount, int previousJobBeginIndex,
	                            ComponentDataArray<EntityInstanceRendererTransform> transforms, EntityInstanceRenderer renderer)
	    {
	        UnityEngine.Profiling.Profiler.BeginSample("Previous DrawMeshInstanced");

	        previousMatrixJobHandle.Complete();

	        for (int matrixJobIndex = 0; matrixJobIndex < previousJobCount; matrixJobIndex++)
	        {
	            int beginSubIndex = previousJobBeginIndex + (matrixJobIndex * m_batchSize);
	            int drawLength = math.min(m_batchSize, transforms.Length - beginSubIndex);

	            for(int subMeshIndex = 0; subMeshIndex < renderer.mesh.subMeshCount; subMeshIndex++)
	            {
	                Graphics.DrawMeshInstanced(renderer.mesh, subMeshIndex, renderer.materials[subMeshIndex], m_MatricesArrayList[previousFilledMatrixJobIndex][matrixJobIndex],
	                    drawLength, null, renderer.castShadows, renderer.receiveShadows);
	            }
	        }
	        UnityEngine.Profiling.Profiler.EndSample();
	    }

        unsafe protected override void OnUpdate()
        {
            UnityEngine.Profiling.Profiler.BeginSample("complete");
            m_InstanceRendererGroup.GetDependency().Complete();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("GetAllUniqueSharedComponentDatas");
            EntityManager.GetAllUniqueSharedComponentDatas(m_CacheduniqueRendererTypes);
            UnityEngine.Profiling.Profiler.EndSample();

            CopyMatricesJob[] copyMatricesJobArray = new CopyMatricesJob[m_matrixPtrCount];
            for (int i = 0; i < m_matrixPtrCount; i++)
            {
                copyMatricesJobArray[i] = new CopyMatricesJob();
            }

            for (int i = 0;i != m_CacheduniqueRendererTypes.Count;i++)
            {
                UnityEngine.Profiling.Profiler.BeginSample("Unique renderer");

                EntityInstanceRenderer renderer = m_CacheduniqueRendererTypes[i];

                m_InstanceRendererGroup.SetFilter(renderer);

                ComponentDataArray<EntityInstanceRendererTransform> transforms = m_InstanceRendererGroup.GetComponentDataArray<EntityInstanceRendererTransform>();



                int beginIndex = 0;

                int previousFilledMatrixJobIndex = -1;
                JobHandle previousMatrixJobHandle = new JobHandle();
                int previousJobCount = 0;
                int previousJobBeginIndex = 0;


#if !ENABLE_IL2CPP
                if (Input.GetKey(KeyCode.O))
                {
#endif
                    while (beginIndex < transforms.Length)
                    {
                        int length = math.min(m_MatricesArray.Length, transforms.Length - beginIndex);
                        UnityEngine.Profiling.Profiler.BeginSample("CopyMatrices");
                        CopyMatrices(transforms, beginIndex, length, m_MatricesArray);
                        UnityEngine.Profiling.Profiler.EndSample();

                        UnityEngine.Profiling.Profiler.BeginSample("DrawMeshInstanced");
                        for (int subMeshIndex = 0; subMeshIndex < renderer.mesh.subMeshCount; subMeshIndex++)
                        {
                            Graphics.DrawMeshInstanced(renderer.mesh, subMeshIndex, renderer.materials[subMeshIndex],
                                m_MatricesArray, length, null, renderer.castShadows, renderer.receiveShadows);
                        }

                        UnityEngine.Profiling.Profiler.EndSample();

                        beginIndex += length;
                    }
#if !ENABLE_IL2CPP
                }
                else
                {
                    while (beginIndex < transforms.Length)
                    {
                        int totalJobLength = math.min(m_batchSize * m_matrixPtrCount, transforms.Length - beginIndex);
                        int jobCount = (totalJobLength / m_batchSize);
                        if (totalJobLength % m_batchSize > 0)
                        {
                            jobCount += 1;
                        }

                        if (jobCount < 10)
                        {
                            if (previousFilledMatrixJobIndex != -1)
                            {
                                DrawPreviousJobs(previousMatrixJobHandle, previousFilledMatrixJobIndex, previousJobCount, previousJobBeginIndex,
                                    transforms, renderer);

                                //We are not filling any matrices from jobs in this loop, reset the counter
                                previousFilledMatrixJobIndex = -1;
                            }

                            int length = math.min(m_MatricesArray.Length, transforms.Length - beginIndex);
                            UnityEngine.Profiling.Profiler.BeginSample("CopyMatrices");
                            CopyMatrices(transforms, beginIndex, length, m_MatricesArray);
                            UnityEngine.Profiling.Profiler.EndSample();

                            UnityEngine.Profiling.Profiler.BeginSample("DrawMeshInstanced");
                            for (int subMeshIndex = 0; subMeshIndex < renderer.mesh.subMeshCount; subMeshIndex++)
                            {
                                Graphics.DrawMeshInstanced(renderer.mesh, subMeshIndex, renderer.materials[subMeshIndex],
                                    m_MatricesArray, length, null, renderer.castShadows, renderer.receiveShadows);
                            }

                            UnityEngine.Profiling.Profiler.EndSample();

                            beginIndex += length;
                        }
                        else
                        {
                            int matrixJobIndexToFill = previousFilledMatrixJobIndex == 0 ? 1 : 0;

                            //TODO: find a better way to create an arraym_MatricesArrayList of Ptr to the matrices
                            List<Matrix4x4[]> matrixJobIndexToFillList = m_MatricesArrayList[matrixJobIndexToFill];
                            fixed (Matrix4x4* matrixPtr_0 = matrixJobIndexToFillList[0],
                                matrixPtr_1 = matrixJobIndexToFillList[1],
                                matrixPtr_2 = matrixJobIndexToFillList[2],
                                matrixPtr_3 = matrixJobIndexToFillList[3],
                                matrixPtr_4 = matrixJobIndexToFillList[4],
                                matrixPtr_5 = matrixJobIndexToFillList[5],
                                matrixPtr_6 = matrixJobIndexToFillList[6],
                                matrixPtr_7 = matrixJobIndexToFillList[7],
                                matrixPtr_8 = matrixJobIndexToFillList[8],
                                matrixPtr_9 = matrixJobIndexToFillList[9],
                                matrixPtr_10 = matrixJobIndexToFillList[10],
                                matrixPtr_11 = matrixJobIndexToFillList[11],
                                matrixPtr_12 = matrixJobIndexToFillList[12],
                                matrixPtr_13 = matrixJobIndexToFillList[13],
                                matrixPtr_14 = matrixJobIndexToFillList[14],
                                matrixPtr_15 = matrixJobIndexToFillList[15],
                                matrixPtr_16 = matrixJobIndexToFillList[16],
                                matrixPtr_17 = matrixJobIndexToFillList[17],
                                matrixPtr_18 = matrixJobIndexToFillList[18],
                                matrixPtr_19 = matrixJobIndexToFillList[19],
                                matrixPtr_20 = matrixJobIndexToFillList[20],
                                matrixPtr_21 = matrixJobIndexToFillList[21],
                                matrixPtr_22 = matrixJobIndexToFillList[22],
                                matrixPtr_23 = matrixJobIndexToFillList[23],
                                matrixPtr_24 = matrixJobIndexToFillList[24],
                                matrixPtr_25 = matrixJobIndexToFillList[25],
                                matrixPtr_26 = matrixJobIndexToFillList[26],
                                matrixPtr_27 = matrixJobIndexToFillList[27],
                                matrixPtr_28 = matrixJobIndexToFillList[28],
                                matrixPtr_29 = matrixJobIndexToFillList[29],
                                matrixPtr_30 = matrixJobIndexToFillList[30],
                                matrixPtr_31 = matrixJobIndexToFillList[31],
                                matrixPtr_32 = matrixJobIndexToFillList[32],
                                matrixPtr_33 = matrixJobIndexToFillList[33],
                                matrixPtr_34 = matrixJobIndexToFillList[34],
                                matrixPtr_35 = matrixJobIndexToFillList[35],
                                matrixPtr_36 = matrixJobIndexToFillList[36],
                                matrixPtr_37 = matrixJobIndexToFillList[37],
                                matrixPtr_38 = matrixJobIndexToFillList[38],
                                matrixPtr_39 = matrixJobIndexToFillList[39]
                            )
                            {
                                Matrix4x4*[] matrixPtrArray =
                                {
                                    matrixPtr_0,
                                    matrixPtr_1,
                                    matrixPtr_2,
                                    matrixPtr_3,
                                    matrixPtr_4,
                                    matrixPtr_5,
                                    matrixPtr_6,
                                    matrixPtr_7,
                                    matrixPtr_8,
                                    matrixPtr_9,
                                    matrixPtr_10,
                                    matrixPtr_11,
                                    matrixPtr_12,
                                    matrixPtr_13,
                                    matrixPtr_14,
                                    matrixPtr_15,
                                    matrixPtr_16,
                                    matrixPtr_17,
                                    matrixPtr_18,
                                    matrixPtr_19,
                                    matrixPtr_20,
                                    matrixPtr_21,
                                    matrixPtr_22,
                                    matrixPtr_23,
                                    matrixPtr_24,
                                    matrixPtr_25,
                                    matrixPtr_26,
                                    matrixPtr_27,
                                    matrixPtr_28,
                                    matrixPtr_29,
                                    matrixPtr_30,
                                    matrixPtr_31,
                                    matrixPtr_32,
                                    matrixPtr_33,
                                    matrixPtr_34,
                                    matrixPtr_35,
                                    matrixPtr_36,
                                    matrixPtr_37,
                                    matrixPtr_38,
                                    matrixPtr_39,
                                };



                                UnityEngine.Profiling.Profiler.BeginSample("Setup Copy Jobs");

                                for (int matrixJobIndex = 0; matrixJobIndex < jobCount; matrixJobIndex++)
                                {
                                    copyMatricesJobArray[matrixJobIndex].matricesPtr = matrixPtrArray[matrixJobIndex];
                                    copyMatricesJobArray[matrixJobIndex].transforms = transforms;
                                    copyMatricesJobArray[matrixJobIndex].beginIndex = beginIndex + (matrixJobIndex * m_batchSize);
                                    copyMatricesJobArray[matrixJobIndex].length = math.min(m_batchSize, transforms.Length - copyMatricesJobArray[matrixJobIndex].beginIndex);
                                }

                                JobHandle allCurrentMatrixJobHandle = copyMatricesJobArray[0].Schedule();
                                for (int matrixJobIndex = 0; matrixJobIndex < jobCount; matrixJobIndex++)
                                {
                                    allCurrentMatrixJobHandle = JobHandle.CombineDependencies(allCurrentMatrixJobHandle, copyMatricesJobArray[matrixJobIndex].Schedule());
                                }

                                JobHandle.ScheduleBatchedJobs();
                                UnityEngine.Profiling.Profiler.EndSample();

                                if (previousFilledMatrixJobIndex != -1)
                                {
                                    DrawPreviousJobs(previousMatrixJobHandle, previousFilledMatrixJobIndex, previousJobCount, previousJobBeginIndex,
                                        transforms, renderer);
                                }

                                previousMatrixJobHandle = allCurrentMatrixJobHandle;
                                previousFilledMatrixJobIndex = matrixJobIndexToFill;
                                previousJobCount = jobCount;
                                previousJobBeginIndex = beginIndex;

                            }


                            beginIndex += totalJobLength;
                        }
                    }

                    //draw anything left
                    if (previousFilledMatrixJobIndex != -1)
                    {
                        DrawPreviousJobs(previousMatrixJobHandle, previousFilledMatrixJobIndex, previousJobCount, previousJobBeginIndex,
                            transforms, renderer);
                    }
                }
#endif



                UnityEngine.Profiling.Profiler.EndSample();
            }

            m_CacheduniqueRendererTypes.Clear();
		}
	}
}
