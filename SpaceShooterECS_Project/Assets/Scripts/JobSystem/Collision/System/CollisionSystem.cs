using System;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;


namespace ECS_SpaceShooterDemo
{
    [UpdateAfter(typeof(EntityOutOfBoundSystem))]
    public class CollisionSystem : GameControllerJobComponentSystem
    {
        struct HashMapData
        {
            public Entity entityStored;
            public EntityBoundMinMaxData minMaxData;
        }

        ComponentGroup destroyEntityDataGroup;

        float3 collisionHashMapBigCellSizes = new float3(4.0f, 4.0f, 4.0f);
        float3 collisionHashMapSmallCellSizes = new float3(2.0f, 1.0f, 2.0f);

        ComponentGroup boundDataGroup = null;

        List<EntityTypeData.EntityType> entityTypeList = new List<EntityTypeData.EntityType>((int)EntityTypeData.EntityType.EntityTypeCount);
        Dictionary<EntityTypeData.EntityType, NativeArray<Entity>> subsetEntityDictionary = new Dictionary<EntityTypeData.EntityType, NativeArray<Entity>>((int)EntityTypeData.EntityType.EntityTypeCount);
        Dictionary<EntityTypeData.EntityType, NativeArray<EntityBoundMinMaxData>> subsetMinMaxDataDictionary = new Dictionary<EntityTypeData.EntityType, NativeArray<EntityBoundMinMaxData>>((int)EntityTypeData.EntityType.EntityTypeCount);
        Dictionary<EntityTypeData.EntityType, JobHandle> fillCellJobHandleDictionary = new Dictionary<EntityTypeData.EntityType, JobHandle>((int)EntityTypeData.EntityType.EntityTypeCount);

        //Because clearing our hash maps can take a long time we will double buffer them
        int currentCellDictionary = 0;
        Dictionary<EntityTypeData.EntityType, NativeMultiHashMap<int, HashMapData>>[] cellEntityTypeDictionaryArray = new Dictionary<EntityTypeData.EntityType, NativeMultiHashMap<int, HashMapData>>[2];
        Dictionary<EntityTypeData.EntityType, NativeMultiHashMap<int, HashMapData>> cellEntityTypeDictionary
        {
            get
            {
                return cellEntityTypeDictionaryArray[currentCellDictionary];
            }
        }
        Dictionary<EntityTypeData.EntityType, float3> cellSizeEntityDictionary = new Dictionary<EntityTypeData.EntityType, float3>((int)EntityTypeData.EntityType.EntityTypeCount);

        List<EntityTypeData> uniqueEntityTypes = new List<EntityTypeData>(10);


        JobHandle[] allClearCellsJobHandleArray;
        JobHandle allClearCellsJobHandle
        {
            get
            {
                return allClearCellsJobHandleArray[currentCellDictionary];
            }
            set
            {
                allClearCellsJobHandleArray[currentCellDictionary] = value;
            }
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            
            destroyEntityDataGroup = GetComponentGroup(typeof(DestroyEntityData));
            
            
            allClearCellsJobHandleArray = new JobHandle[cellEntityTypeDictionaryArray.Length];

            for (int i = 0; i < cellEntityTypeDictionaryArray.Length; i++)
            {
                cellEntityTypeDictionaryArray[i] = new Dictionary<EntityTypeData.EntityType, NativeMultiHashMap<int, HashMapData>>((int)EntityTypeData.EntityType.EntityTypeCount);
            }

            boundDataGroup = GetComponentGroup(typeof(CollisionData), typeof(EntityTypeData), typeof(EntityBoundMinMaxData));
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            for(int cellDictionaryIndex = 0; cellDictionaryIndex < cellEntityTypeDictionaryArray.Length; cellDictionaryIndex++)
            {
                currentCellDictionary = cellDictionaryIndex;

                allClearCellsJobHandle.Complete();

                for (int i = 0; i < (int)EntityTypeData.EntityType.EntityTypeCount; i++)
                {
                    EntityTypeData.EntityType entityTypeToRemove = (EntityTypeData.EntityType)i;
                    if (cellEntityTypeDictionary.ContainsKey(entityTypeToRemove))
                    {
                        cellEntityTypeDictionary[entityTypeToRemove].Dispose();
                    }
                }
            }
        }

        void CreateCellHashMap(EntityTypeData.EntityType entityType)
        {
            if(cellEntityTypeDictionary.ContainsKey(entityType))
            {
                return;
            }

            switch (entityType)
            {
                case EntityTypeData.EntityType.Asteroid:
                case EntityTypeData.EntityType.EnemyShip:
                case EntityTypeData.EntityType.AllyShip:
                case EntityTypeData.EntityType.PlayerShip:
                    cellEntityTypeDictionary.Add(entityType, new NativeMultiHashMap<int, HashMapData>(50000, Allocator.Persistent));
                    if(!cellSizeEntityDictionary.ContainsKey(entityType))
                    {
                        cellSizeEntityDictionary.Add(entityType, collisionHashMapSmallCellSizes);
                    }
                    break;
                case EntityTypeData.EntityType.EnemyBolt:
                case EntityTypeData.EntityType.AllyBolt:
                case EntityTypeData.EntityType.PlayerBolt:
                    cellEntityTypeDictionary.Add(entityType, new NativeMultiHashMap<int, HashMapData>(600000, Allocator.Persistent));
                    if (!cellSizeEntityDictionary.ContainsKey(entityType))
                    {
                        cellSizeEntityDictionary.Add(entityType, collisionHashMapBigCellSizes);
                    }
                    break;
                case EntityTypeData.EntityType.EntityTypeCount:
                    break;
                default:
                    {
                        Debug.LogError("Unknown entity type");
                        cellEntityTypeDictionary.Add(entityType, new NativeMultiHashMap<int, HashMapData>(50000, Allocator.Persistent));
                        if (!cellSizeEntityDictionary.ContainsKey(entityType))
                        {
                            cellSizeEntityDictionary.Add(entityType, collisionHashMapBigCellSizes);
                        }
                    }
                    break;
            }
        }


        public static int Hash(float3 v, float3 cellSizes)
        {
            return Hash(Quantize(v, cellSizes));
        }

        static int3 Quantize(float3 v, float3 cellSizes)
        {
            return new int3(math.floor(v / cellSizes));
        }

        static int Hash(int3 grid)
        {
            // Simple int3 hash based on a pseudo mix of :
            // 1) https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
            // 2) https://en.wikipedia.org/wiki/Jenkins_hash_function
            int hash = grid.x;
            hash = (hash * 397) ^ grid.y;
            hash = (hash * 397) ^ grid.z;
            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return hash;
        }

        static void AddHashData( HashMapData hashMapDataToAdd, float3 cellSizes, NativeMultiHashMap<int, HashMapData>.Concurrent cells)
        {
            int3 minQuanta = Quantize(hashMapDataToAdd.minMaxData.min, cellSizes);
            int3 maxQuanta = Quantize(hashMapDataToAdd.minMaxData.max, cellSizes);

            int3 deltaQuanta = maxQuanta - minQuanta;


            for (int i = 0; i <= deltaQuanta.x; i++)
            {
                for (int j = 0; j <= deltaQuanta.y; j++)
                {
                    for (int k = 0; k <= deltaQuanta.z; k++)
                    {
                        int hash = Hash( minQuanta + new int3(i, j, k));
                        cells.Add(hash, hashMapDataToAdd);
                    }
                }
            }
        }


        [BurstCompile]
        struct FillCellJob : IJobParallelFor
        {
            [ReadOnly]
            public EntityArray entityArray;

            [ReadOnly]
            public ComponentDataArray<EntityBoundMinMaxData> entityBoundMinMaxDataArray;

            [WriteOnly]
            public NativeArray<Entity> entityArrayOutput;
            [WriteOnly]
            public NativeArray<EntityBoundMinMaxData> entityBoundMinMaxDataArrayOutput;

            public NativeMultiHashMap<int, HashMapData>.Concurrent outputCells;
            public float3 cellSizes;


            public void Execute(int index)
            {
                Entity entityToAdd = entityArray[index];
                EntityBoundMinMaxData boundMinMaxData = entityBoundMinMaxDataArray[index];

                entityArrayOutput[index] = entityToAdd;
                entityBoundMinMaxDataArrayOutput[index] = boundMinMaxData;
                AddHashData(new HashMapData{entityStored = entityToAdd, minMaxData = boundMinMaxData}, cellSizes, outputCells);
            }
        }

        [BurstCompile]
        struct CollisionDetectJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Entity> entityArray;
            [ReadOnly]
            public NativeArray<EntityBoundMinMaxData> entityBoundMinMaxData;

            [ReadOnly]
            public NativeMultiHashMap<int, HashMapData> boundCells;
            public float3 cellSizes;
            [WriteOnly]
            public NativeQueue<Entity>.Concurrent collidedEntityQueue;

            public void Execute(int index)
            {
                Entity currentEntity = entityArray[index];
                EntityBoundMinMaxData boundMinMaxData = entityBoundMinMaxData[index];
                int3 minQuanta = Quantize(boundMinMaxData.min, cellSizes);
                int3 maxQuanta = Quantize(boundMinMaxData.max, cellSizes);
                int3 deltaQuanta = maxQuanta - minQuanta;

                bool collided = false;
                Entity collidedEntityFound = new Entity();
                int hash;
                for (int i = 0; i <= deltaQuanta.x && !collided; i++)
                {
                    for (int j = 0; j <= deltaQuanta.y && !collided; j++)
                    {
                        for (int k = 0; k <= deltaQuanta.z && !collided; k++)
                        {
                            hash = Hash( minQuanta + new int3(i, j, k));

                            HashMapData hashMapDataFound;
                            NativeMultiHashMapIterator<int> iterator;

                            bool found = boundCells.TryGetFirstValue(hash, out hashMapDataFound, out iterator);
                            while(found && !collided)
                            {
                                //This test is not needed given that we have separate hash map for each entity type
                               /* if(currentEntity == collidedEntity)
                                {
                                    found = boundCells.TryGetNextValue(out collidedEntity, ref iterator);
                                    continue;
                                }*/

                                EntityBoundMinMaxData boundMinMaxDataOther = hashMapDataFound.minMaxData;

                                //axis aligned test

                                float3 minSelf = boundMinMaxData.min;
                                float3 minOther = boundMinMaxDataOther.min;
                                float3 maxSelf = boundMinMaxData.max;
                                float3 maxOther = boundMinMaxDataOther.max;

                                if (math.all(maxSelf > minOther)
                                    && math.all(maxOther > minSelf))
                                {
                                    collided = true;
                                    collidedEntityFound = hashMapDataFound.entityStored;
                                }
                                else
                                {
                                    found = boundCells.TryGetNextValue(out hashMapDataFound, ref iterator);
                                }
                            }
                        }
                    }
                }

                if(collided)
                {
                    collidedEntityQueue.Enqueue(currentEntity);
                    collidedEntityQueue.Enqueue(collidedEntityFound);
                }
            }
        }


        //TODO: When using burst this job never complete when resizing the hash map 
        [BurstCompile]
        struct AllocateCellsJob : IJob
        {
            public NativeMultiHashMap<int, HashMapData> outputCells;

            public int capacityWanted;

            public void Execute()
            {
                if (outputCells.Capacity < capacityWanted)
                {
                    outputCells.Capacity = capacityWanted;
                }
            }
        }


        [BurstCompile]
        struct ClearCellsJob : IJob
        {
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> entityArray;

            [DeallocateOnJobCompletion]
            public NativeArray<EntityBoundMinMaxData> entityBoundMinMaxDataArray;

            [WriteOnly]
            public NativeMultiHashMap<int, HashMapData> outputCells;

            public void Execute()
            {
                outputCells.Clear();
            }
        }



        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            UnityEngine.Profiling.Profiler.BeginSample("System Init");


            EntityArray destroyEntityArray = destroyEntityDataGroup.GetEntityArray();
         
            if (destroyEntityArray.Length == 0)
            {
                return inputDeps;
            }
            
            Entity destroyEntity = destroyEntityArray[0];
            ComponentDataFromEntity<DestroyEntityData> destroyEntityDataFromEntity = GetComponentDataFromEntity<DestroyEntityData>();        
            DestroyEntityData destroyEntityData = destroyEntityDataFromEntity[destroyEntity];
            
            currentCellDictionary++;
            if(currentCellDictionary >= cellEntityTypeDictionaryArray.Length)
            {
                currentCellDictionary = 0;
            }

            //Make sure we cleared all the hash maps
            allClearCellsJobHandle.Complete();

            uniqueEntityTypes.Clear();
            EntityManager.GetAllUniqueSharedComponentData(uniqueEntityTypes);

            entityTypeList.Clear();
            subsetEntityDictionary.Clear();
            subsetMinMaxDataDictionary.Clear();
            fillCellJobHandleDictionary.Clear();

            JobHandle allBoundGroupDependencies = boundDataGroup.GetDependency();

            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("FillJobSetup");

            JobHandle allocateCellJobDependency = allClearCellsJobHandle;
            JobHandle allocateCellJobHandle = new JobHandle();

            EntityArray[] subsetEntityArrayArray = new EntityArray[uniqueEntityTypes.Count];
            ComponentDataArray<EntityBoundMinMaxData>[] subsetMinMaxDataArrayArray = new ComponentDataArray<EntityBoundMinMaxData>[uniqueEntityTypes.Count];

            //create the hashMaps if needed and get the subset arrays we will use
            UnityEngine.Profiling.Profiler.BeginSample("GetEntityArray");
            
            
            for (int i = 0; i != uniqueEntityTypes.Count; i++)
            {
                boundDataGroup.SetFilter(uniqueEntityTypes[i]);
                subsetEntityArrayArray[i] = boundDataGroup.GetEntityArray(); 
                subsetMinMaxDataArrayArray[i] = boundDataGroup.GetComponentDataArray<EntityBoundMinMaxData>(); 

                if (subsetEntityArrayArray[i].Length != 0)
                {
                    CreateCellHashMap(uniqueEntityTypes[i].entityType);
                }

            }
            
            boundDataGroup.ResetFilter();
            UnityEngine.Profiling.Profiler.EndSample();


            //set the cells capacity now
            UnityEngine.Profiling.Profiler.BeginSample("Resize Hash Map");
            for (int i = 0; i != uniqueEntityTypes.Count; i++)
            {
                EntityTypeData entityTypeData = uniqueEntityTypes[i];
                EntityArray subsetEntityArray = subsetEntityArrayArray[i];

                if(subsetEntityArray.Length == 0)
                {
                    continue;
                }

                NativeMultiHashMap<int, HashMapData> tmpOutputCell = cellEntityTypeDictionary[entityTypeData.entityType];

                //TODO: Test the memory usage
                //We are setting the capacity really high to not run out of space while running our jobs
                if (tmpOutputCell.Capacity < subsetEntityArray.Length * 10)
                {
                    AllocateCellsJob allocateCellJob = new AllocateCellsJob
                    {
                        outputCells = tmpOutputCell,
                        capacityWanted = subsetEntityArray.Length * 20,
                    };

                    allocateCellJobHandle = JobHandle.CombineDependencies(allocateCellJob.Schedule(allocateCellJobDependency), allocateCellJobHandle);
                }


            }
            UnityEngine.Profiling.Profiler.EndSample();

            JobHandle fillCellJobDependency = JobHandle.CombineDependencies(inputDeps, allBoundGroupDependencies, allocateCellJobHandle);

            for (int i = 0; i != uniqueEntityTypes.Count; i++)
            {
                EntityTypeData entityTypeData = uniqueEntityTypes[i];
                EntityArray subsetEntityArray = subsetEntityArrayArray[i];
                ComponentDataArray<EntityBoundMinMaxData> subsetMinMaxDataArray = subsetMinMaxDataArrayArray[i];

                if(subsetEntityArray.Length == 0)
                {
                    continue;
                }

                NativeMultiHashMap<int, HashMapData>.Concurrent tmpOutputCell = cellEntityTypeDictionary[entityTypeData.entityType].ToConcurrent();
                float3 tmpOutputCellSize = cellSizeEntityDictionary[entityTypeData.entityType];

                UnityEngine.Profiling.Profiler.BeginSample("Allocate tmp Array");
                NativeArray<Entity> subsetEntityArrayOutput = new NativeArray<Entity>(subsetEntityArray.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                NativeArray<EntityBoundMinMaxData> subsetMinMaxDataArrayOutput = new NativeArray<EntityBoundMinMaxData>(subsetEntityArray.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                UnityEngine.Profiling.Profiler.EndSample();

                FillCellJob fillCellJob = new FillCellJob
                {
                    entityArray = subsetEntityArray,
                    entityBoundMinMaxDataArray = subsetMinMaxDataArray,
                    entityArrayOutput = subsetEntityArrayOutput,
                    entityBoundMinMaxDataArrayOutput = subsetMinMaxDataArrayOutput,
                    outputCells = tmpOutputCell,
                    cellSizes = tmpOutputCellSize,
                };

                JobHandle previousFillJobDependency;

                boundDataGroup.SetFilter(entityTypeData);
                JobHandle jobDependency = JobHandle.CombineDependencies(fillCellJobDependency, boundDataGroup.GetDependency());
                if (fillCellJobHandleDictionary.TryGetValue(entityTypeData.entityType, out previousFillJobDependency))
                {
                    jobDependency = JobHandle.CombineDependencies(jobDependency, previousFillJobDependency);
                }


                JobHandle fillCellJobHandle = fillCellJob.Schedule(subsetEntityArray.Length,
                                                                   MonoBehaviourECSBridge.Instance.GetJobBatchCount(subsetEntityArray.Length),
                                                                   jobDependency);


                entityTypeList.Add(entityTypeData.entityType);
                subsetEntityDictionary.Add(entityTypeData.entityType, subsetEntityArrayOutput);
                subsetMinMaxDataDictionary.Add(entityTypeData.entityType, subsetMinMaxDataArrayOutput);
                fillCellJobHandleDictionary.Add(entityTypeData.entityType, fillCellJobHandle);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            if (fillCellJobHandleDictionary.Count == 0)
            {
                return inputDeps;
            }

            UnityEngine.Profiling.Profiler.BeginSample("CollisionJobSetup");

            JobHandle previousCollisionJobHandle = new JobHandle();

            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.Asteroid, EntityTypeData.EntityType.PlayerBolt, destroyEntityData, previousCollisionJobHandle);            
            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.Asteroid, EntityTypeData.EntityType.EnemyShip, destroyEntityData, previousCollisionJobHandle);
            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.Asteroid, EntityTypeData.EntityType.AllyShip, destroyEntityData, previousCollisionJobHandle);
            
            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.EnemyShip, EntityTypeData.EntityType.AllyBolt, destroyEntityData, previousCollisionJobHandle);
            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.EnemyShip, EntityTypeData.EntityType.PlayerBolt, destroyEntityData, previousCollisionJobHandle);
            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.EnemyShip, EntityTypeData.EntityType.AllyShip, destroyEntityData, previousCollisionJobHandle);
            
            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.AllyShip, EntityTypeData.EntityType.EnemyBolt, destroyEntityData, previousCollisionJobHandle);

            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.PlayerShip, EntityTypeData.EntityType.Asteroid, destroyEntityData, previousCollisionJobHandle);
            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.PlayerShip, EntityTypeData.EntityType.EnemyBolt, destroyEntityData, previousCollisionJobHandle);
            previousCollisionJobHandle = scheduleCollisionJob(EntityTypeData.EntityType.PlayerShip, EntityTypeData.EntityType.EnemyShip, destroyEntityData, previousCollisionJobHandle);


            UnityEngine.Profiling.Profiler.EndSample();

            JobHandle.ScheduleBatchedJobs();

            UnityEngine.Profiling.Profiler.BeginSample("DisposeJobSetup");

            JobHandle jobHandleToReturn = new JobHandle();

            List<JobHandle> clearCellJobHandleList = new List<JobHandle>(entityTypeList.Count);
            for(int i = 0; i < entityTypeList.Count; i++)
            {
                if (subsetEntityDictionary.ContainsKey(entityTypeList[i]))
                {
                    jobHandleToReturn = JobHandle.CombineDependencies(fillCellJobHandleDictionary[entityTypeList[i]], jobHandleToReturn);

                    ClearCellsJob clearCellsJob = new ClearCellsJob
                    {
                        entityArray = subsetEntityDictionary[entityTypeList[i]],
                        entityBoundMinMaxDataArray = subsetMinMaxDataDictionary[entityTypeList[i]],
                        outputCells = cellEntityTypeDictionary[entityTypeList[i]],
                    };

                    JobHandle clearCellsJobHandle = clearCellsJob.Schedule(JobHandle.CombineDependencies(fillCellJobHandleDictionary[entityTypeList[i]], previousCollisionJobHandle));
                    clearCellJobHandleList.Add(clearCellsJobHandle);
                }

            }

            jobHandleToReturn = JobHandle.CombineDependencies(previousCollisionJobHandle, jobHandleToReturn);

            UnityEngine.Profiling.Profiler.EndSample();

            NativeArray<JobHandle> clearCellsJobHandleArray = new NativeArray<JobHandle>(clearCellJobHandleList.ToArray(), Allocator.Temp);
            allClearCellsJobHandle = JobHandle.CombineDependencies(clearCellsJobHandleArray);
            clearCellsJobHandleArray.Dispose();


            return jobHandleToReturn;


        }

        JobHandle scheduleCollisionJob(EntityTypeData.EntityType entityTypeToCheck,
                                  EntityTypeData.EntityType entityTypeToCheckWith,
                                  DestroyEntityData destroyEntityData,
                                  JobHandle jobDependencies)
        {
            JobHandle collisionDetectJobHandle = new JobHandle();

            if (entityTypeList.Contains(entityTypeToCheck)
                && entityTypeList.Contains(entityTypeToCheckWith))
            {
                CollisionDetectJob collisionDetectJob = new CollisionDetectJob
                {
                    entityArray = subsetEntityDictionary[entityTypeToCheck],
                    entityBoundMinMaxData = subsetMinMaxDataDictionary[entityTypeToCheck],
                    boundCells = cellEntityTypeDictionary[entityTypeToCheckWith],
                    cellSizes = cellSizeEntityDictionary[entityTypeToCheckWith],
                    collidedEntityQueue = destroyEntityData.entityCollisionQueueConcurrent,
                };


                JobHandle jobDependenciesHandles = JobHandle.CombineDependencies(fillCellJobHandleDictionary[entityTypeToCheck],
                                                                         fillCellJobHandleDictionary[entityTypeToCheckWith],
                                                                         jobDependencies);


                collisionDetectJobHandle =  collisionDetectJob.Schedule(collisionDetectJob.entityArray.Length,
                                                                        MonoBehaviourECSBridge.Instance.GetJobBatchCount(collisionDetectJob.entityArray.Length),
                                                                        jobDependenciesHandles);
            }

            return JobHandle.CombineDependencies(collisionDetectJobHandle, jobDependencies);
        }

    }
}




