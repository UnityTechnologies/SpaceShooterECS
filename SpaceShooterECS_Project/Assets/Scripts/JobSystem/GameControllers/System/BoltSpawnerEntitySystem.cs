using System;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

namespace ECS_SpaceShooterDemo
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(EntityManagementGroup))]
    [UpdateAfter(typeof(DestroyEntitySystem))]
    public class BoltSpawnerEntitySystem : GameControllerJobComponentSystem
    {
        //queues that will be used by other system to tell this system to spawn new bolts
        public NativeQueue<Entity> enemyBoltSpawnQueue;
        public NativeQueue<Entity> allyBoltSpawnQueue;
        public NativeQueue<Entity> playerBoltSpawnQueue;

        //List used to store entities we need to spawn bolts from
        //Filled each frame from the previous queues after testing if the entities are still valid
        private NativeList<Entity> enemyBoltSpawnList;
        private NativeList<Entity> allyBoltSpawnList;
        private NativeList<Entity> playerBoltSpawnList;

        //entity used by other systems to find the previous queues
        private Entity dataEntity;

        //entities that we will use as "prefab" for our bolts
        Entity prefabEnemyBolt;
        Entity prefabAllyBolt;
        Entity prefabPlayerBolt;

        //Jobs that will go over all newly spawned bolt and set their BoltMoveData values
        [BurstCompile]
        struct SetAIBoltMoveDataJob : IJobParallelFor
        {
            //List of entities we spawned from
            [ReadOnly]
            public NativeList<Entity> spawningFromEntityList;

            //All the newly spawned bolt entities,
            //the previous list and this array index are aligned
            //meaning spawningFromEntityList[0] is the entity that spawnedBoltEntityArray[0] spawned from
            [ReadOnly]
            [DeallocateOnJobCompletionAttribute]
            public NativeArray<Entity> spawnedBoltEntityArray;

            //ComponentDataFromEntity is used to get component data from specific entities inside job
            [ReadOnly]
            public ComponentDataFromEntity<AISpawnBoltData> aiSpawnBoltDataFromEntity;

            //We need to tell the safety system to allow us to write in a parallel for job
            //This is safe in this case because we are accessing unique entity in each execute call (newly spawned entities)
            [NativeDisableParallelForRestriction] 
            public ComponentDataFromEntity<BoltMoveData> boltMoveDataFromEntity;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Position> boltPositionFromEntity;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Rotation> boltRotationFromEntity;
            
            public void Execute(int index)
            {
                //Get the spawning information from the entity we spawned from
                AISpawnBoltData spawnBoltData = aiSpawnBoltDataFromEntity[spawningFromEntityList[index]];
                //Get our Bolt position/rotation
                BoltMoveData boltMoveData = boltMoveDataFromEntity[spawnedBoltEntityArray[index]];
                Position boltPosition = boltPositionFromEntity[spawnedBoltEntityArray[index]];
                Rotation boltRotation = boltRotationFromEntity[spawnedBoltEntityArray[index]];

                //Set our initial values
                boltMoveData.forwardDirection = spawnBoltData.spawnDirection;
                boltPosition.Value = spawnBoltData.spawnPosition;
                boltRotation.Value = quaternion.LookRotation(new float3(0, -1, 0), new float3(0, 0, 1));

                boltMoveDataFromEntity[spawnedBoltEntityArray[index]] = boltMoveData;
                boltPositionFromEntity[spawnedBoltEntityArray[index]] = boltPosition;
                boltRotationFromEntity[spawnedBoltEntityArray[index]] = boltRotation;
            }
        }
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            //Create our queues to hold entities to spawn bolt from
            enemyBoltSpawnQueue = new NativeQueue<Entity>(Allocator.Persistent);
            allyBoltSpawnQueue = new NativeQueue<Entity>(Allocator.Persistent);
            playerBoltSpawnQueue = new NativeQueue<Entity>(Allocator.Persistent);

            enemyBoltSpawnList = new NativeList<Entity>(100000, Allocator.Persistent);
            allyBoltSpawnList = new NativeList<Entity>(100000, Allocator.Persistent);
            playerBoltSpawnList = new NativeList<Entity>(100000, Allocator.Persistent);

            //Create the entitie that holds our queue, one way of making them accessible to other systems 
            BoltSpawnerEntityData data = new BoltSpawnerEntityData();
            data.enemyBoltSpawnQueueConcurrent = enemyBoltSpawnQueue.ToConcurrent();
            data.allyBoltSpawnQueueConcurrent = allyBoltSpawnQueue.ToConcurrent();
            data.playerBoltSpawnQueueConcurrent = playerBoltSpawnQueue.ToConcurrent();

            dataEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(dataEntity, data);

            //Create entities that we will use as "prefab" for our bolts
            //Add the Prefab IComponentData to make sure those entities are not picked up by systems
            prefabEnemyBolt = EntityManager.Instantiate(MonoBehaviourECSBridge.Instance.enemyBolt);
            EntityManager.AddComponentData<Prefab>(prefabEnemyBolt, new Prefab());
            
            prefabAllyBolt = EntityManager.Instantiate(MonoBehaviourECSBridge.Instance.allyBolt);
            EntityManager.AddComponentData<Prefab>(prefabAllyBolt, new Prefab());

            prefabPlayerBolt = EntityManager.Instantiate(MonoBehaviourECSBridge.Instance.playerBolt);
            EntityManager.AddComponentData<Prefab>(prefabPlayerBolt, new Prefab());
        }

        protected override void OnDestroyManager()
        {
            EntityManager.CompleteAllJobs();

            //Dispose of queues and lists we allocated
            enemyBoltSpawnQueue.Dispose();
            allyBoltSpawnQueue.Dispose();
            playerBoltSpawnQueue.Dispose();

            enemyBoltSpawnList.Dispose();
            allyBoltSpawnList.Dispose();
            playerBoltSpawnList.Dispose();

            //Make sure we destroy entities we are managing
            EntityManager.DestroyEntity(dataEntity);

            EntityManager.DestroyEntity(prefabEnemyBolt);
            EntityManager.DestroyEntity(prefabAllyBolt);
            EntityManager.DestroyEntity(prefabPlayerBolt);

            base.OnDestroyManager();
        }



        JobHandle SpawnBoltFromEntityList(NativeList<Entity> entityList, 
                                          NativeArray<Entity> newSpawnedBoltEntityArray,
                                          bool isboltFromPlayerList, 
                                          JobHandle jobDepency)
        {
            JobHandle jobDepencyToReturn = jobDepency;

            if (entityList.Length == 0)
            {
                return jobDepencyToReturn;
            }

            UnityEngine.Profiling.Profiler.BeginSample("SpawnBoltFromEntityList");
         
            //If the bolts are from players we just set the BoltMoveData directly, they are not enough generated to warrant creating a job
            if (isboltFromPlayerList)
            {
                //For players bolt just set the new bolt data directly
                //(the cost of starting a job is not work the low amount of data to set)
                for (int i = 0; i < entityList.Length; i++)
                {
                    PlayerSpawnBoltData spawnBoltData = EntityManager.GetComponentData<PlayerSpawnBoltData>(entityList[i]);
                    
                    Position newPosition = new Position()
                    {
                        Value = spawnBoltData.spawnPosition,
                    };
                    EntityManager.SetComponentData<Position>(newSpawnedBoltEntityArray[i], newPosition);

                    Rotation newRotation = new Rotation()
                    {
                        Value = quaternion.LookRotation(new float3(0, -1, 0), new float3(0, 0, 1)),
                    };
                    EntityManager.SetComponentData<Rotation>(newSpawnedBoltEntityArray[i], newRotation);
                    
                    BoltMoveData boltMoveData = EntityManager.GetComponentData<BoltMoveData>(newSpawnedBoltEntityArray[i]);
                    boltMoveData.forwardDirection = spawnBoltData.spawnDirection;
                    EntityManager.SetComponentData<BoltMoveData>(newSpawnedBoltEntityArray[i], boltMoveData);

                }

                newSpawnedBoltEntityArray.Dispose();
            }
            else
            {
                //For AI bolts, create a job to set the boltMoveData of the new entity
                //Use GetComponentDataFromEntity the get the components we need
                SetAIBoltMoveDataJob setAiBoldMoveDataJob = new SetAIBoltMoveDataJob
                {
                    spawningFromEntityList = entityList,
                    spawnedBoltEntityArray = newSpawnedBoltEntityArray,
                    aiSpawnBoltDataFromEntity = GetComponentDataFromEntity<AISpawnBoltData>(),
                    boltMoveDataFromEntity = GetComponentDataFromEntity<BoltMoveData>(),
                    boltPositionFromEntity = GetComponentDataFromEntity<Position>(),
                    boltRotationFromEntity = GetComponentDataFromEntity<Rotation>(),
                };

                jobDepencyToReturn = setAiBoldMoveDataJob.Schedule(newSpawnedBoltEntityArray.Length,
                                                                   MonoBehaviourECSBridge.Instance.GetJobBatchCount(newSpawnedBoltEntityArray.Length),
                                                                   jobDepency);
            }


            UnityEngine.Profiling.Profiler.EndSample();

            return jobDepencyToReturn;
        }


        void MoveEntityinQueueToList(NativeQueue<Entity> entityQueue, NativeList<Entity> boltSpawnListToUse)
        {
            int entityQueueSize = entityQueue.Count;

            if (entityQueueSize == 0)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("SpawnBoltFromEntityinQueue");

            //Resize our list if needed
            if (entityQueueSize > boltSpawnListToUse.Capacity)
            {
                boltSpawnListToUse.Capacity *= 2;
            }

            //Add entities to our list if they still exist
            //The DestroySystem might have destroyed this entity before this system
            while (entityQueue.Count > 0)
            {
                Entity entityToSpawnFrom = entityQueue.Dequeue();
                if (!EntityManager.Exists(entityToSpawnFrom))
                {
                    continue;
                }

                boltSpawnListToUse.Add(entityToSpawnFrom);
            }


            UnityEngine.Profiling.Profiler.EndSample();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            playerBoltSpawnList.Clear();
            allyBoltSpawnList.Clear();
            enemyBoltSpawnList.Clear();
            
            
            //Move our entities from a queue to a list after testing if they still exist
            MoveEntityinQueueToList(enemyBoltSpawnQueue, enemyBoltSpawnList);
            MoveEntityinQueueToList(allyBoltSpawnQueue, allyBoltSpawnList);
            MoveEntityinQueueToList(playerBoltSpawnQueue, playerBoltSpawnList);

            //Spawn the bolts from the lists, return a jobHandle (if no job are spawned, return the dependecy passed in parameter)
            JobHandle spawnBoltJobHandle = new JobHandle();
            
            //Allocate the amount of entities we need in one shot
            NativeArray<Entity> playerEntityArray = new NativeArray<Entity>();
            NativeArray<Entity> enemyEntityArray = new NativeArray<Entity>();
            NativeArray<Entity> allyEntityArray = new NativeArray<Entity>();
            
            if (playerBoltSpawnList.Length > 0)
            {
                playerEntityArray = new NativeArray<Entity>(playerBoltSpawnList.Length, Allocator.TempJob);
                EntityManager.Instantiate(prefabPlayerBolt, playerEntityArray);
            }

            if (enemyBoltSpawnList.Length > 0)
            {
                enemyEntityArray = new NativeArray<Entity>(enemyBoltSpawnList.Length, Allocator.TempJob);
                EntityManager.Instantiate(prefabEnemyBolt, enemyEntityArray);    
            }

            if (allyBoltSpawnList.Length > 0)
            {
                allyEntityArray = new NativeArray<Entity>(allyBoltSpawnList.Length, Allocator.TempJob);
                EntityManager.Instantiate(prefabAllyBolt, allyEntityArray);                   
            }

            JobHandle spawnJobDependency = inputDeps;
            
            if (playerEntityArray.IsCreated)
            {
                spawnBoltJobHandle = SpawnBoltFromEntityList(playerBoltSpawnList, playerEntityArray, true, spawnJobDependency);
                spawnJobDependency = JobHandle.CombineDependencies(spawnBoltJobHandle, spawnJobDependency);
            }

            if (enemyEntityArray.IsCreated)
            {
                spawnBoltJobHandle = SpawnBoltFromEntityList(enemyBoltSpawnList, enemyEntityArray, false, spawnJobDependency);
                spawnJobDependency = JobHandle.CombineDependencies(spawnBoltJobHandle, spawnJobDependency);
            }

            if (allyEntityArray.IsCreated)
            {
                spawnBoltJobHandle = SpawnBoltFromEntityList(allyBoltSpawnList, allyEntityArray, false, spawnJobDependency);
                spawnJobDependency = JobHandle.CombineDependencies(spawnBoltJobHandle, spawnJobDependency);
            }
            
            return spawnBoltJobHandle;
        }
    }

}


