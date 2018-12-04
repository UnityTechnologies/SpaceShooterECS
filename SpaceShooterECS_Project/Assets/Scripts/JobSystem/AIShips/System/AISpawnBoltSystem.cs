using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;


namespace ECS_SpaceShooterDemo
{
    [UpdateAfter(typeof(GameMoveSystem))]
    public class AISpawnBoltSystem : GameControllerJobComponentSystem
    {
        ComponentGroup boltSpawnerEntityDataGroup;
        ComponentGroup aiSpawnBoltDataGroup;

        List<EntityTypeData> uniqueEntityTypes = new List<EntityTypeData>(10);
        
        
        [BurstCompile]
        struct AISpawnBoltJob : IJobParallelFor
        {
            [ReadOnly]
            public EntityArray entityArray;
            [ReadOnly]
            public ComponentDataArray<Position> aiPositionArray;
            [ReadOnly]
            public ComponentDataArray<Rotation> aiRotationArray;

            public ComponentDataArray<AISpawnBoltData> aiSpawnBoltDataArray;
            public NativeQueue<Entity>.Concurrent spawnBoltEntityQueue;

            public float deltaTime;

            public void Execute(int index)
            {
                Position aiPosition = aiPositionArray[index];
                Rotation aiRotation = aiRotationArray[index];
                AISpawnBoltData aiSpawnBoltData = aiSpawnBoltDataArray[index];


                float3 forwardDirection = math.forward(aiRotation.Value);
                aiSpawnBoltData.spawnPosition = aiPosition.Value + ( forwardDirection * aiSpawnBoltData.offset);
                aiSpawnBoltData.spawnDirection = forwardDirection;
                aiSpawnBoltData.timeSinceFire += deltaTime;

                if (aiSpawnBoltData.timeSinceFire > aiSpawnBoltData.fireRate)
                {
                    spawnBoltEntityQueue.Enqueue(entityArray[index]);
                    aiSpawnBoltData.timeSinceFire = 0.0f;
                }
                aiSpawnBoltDataArray[index] = aiSpawnBoltData;
            }
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            boltSpawnerEntityDataGroup = GetComponentGroup(typeof(BoltSpawnerEntityData));
            aiSpawnBoltDataGroup = GetComponentGroup(typeof(Position), typeof(Rotation), typeof(AISpawnBoltData), typeof(EntityTypeData));
            
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityArray boltSpawnerEntityDataArray = boltSpawnerEntityDataGroup.GetEntityArray();
            if (boltSpawnerEntityDataArray.Length == 0)
            {
                return inputDeps;
            }
                
            BoltSpawnerEntityData boltSpawnerEntityData = GetComponentDataFromEntity<BoltSpawnerEntityData>()[boltSpawnerEntityDataArray[0]];
            
            uniqueEntityTypes.Clear();
            EntityManager.GetAllUniqueSharedComponentData(uniqueEntityTypes);
            
            JobHandle spawnJobHandle = new JobHandle();
            JobHandle spawnJobDependency = inputDeps;
            for (int i = 0; i != uniqueEntityTypes.Count; i++)
            {
                EntityTypeData entityTypeData = uniqueEntityTypes[i];
                if (entityTypeData.entityType == EntityTypeData.EntityType.EnemyShip
                    || entityTypeData.entityType == EntityTypeData.EntityType.AllyShip)
                {
                    aiSpawnBoltDataGroup.SetFilter(uniqueEntityTypes[i]);
                    
                    NativeQueue<Entity>.Concurrent spawnBoltEntityQueueToUse = boltSpawnerEntityData.enemyBoltSpawnQueueConcurrent;
                    if (entityTypeData.entityType == EntityTypeData.EntityType.AllyShip)
                    {
                        spawnBoltEntityQueueToUse = boltSpawnerEntityData.allyBoltSpawnQueueConcurrent;
                    }


                    AISpawnBoltJob aiSpawnBoltJob = new AISpawnBoltJob
                    {
                        entityArray = aiSpawnBoltDataGroup.GetEntityArray(),
                        aiPositionArray = aiSpawnBoltDataGroup.GetComponentDataArray<Position>(),
                        aiRotationArray = aiSpawnBoltDataGroup.GetComponentDataArray<Rotation>(),
                        aiSpawnBoltDataArray = aiSpawnBoltDataGroup.GetComponentDataArray<AISpawnBoltData>(),
                        spawnBoltEntityQueue = spawnBoltEntityQueueToUse,
                        deltaTime = Time.deltaTime
                    };

                    JobHandle tmpJobHandle = aiSpawnBoltJob.Schedule(aiSpawnBoltJob.aiSpawnBoltDataArray.Length,
                        MonoBehaviourECSBridge.Instance.GetJobBatchCount(aiSpawnBoltJob.aiSpawnBoltDataArray.Length),
                        spawnJobDependency);
                    
                    spawnJobHandle = JobHandle.CombineDependencies(spawnJobHandle, tmpJobHandle);
                    spawnJobDependency = JobHandle.CombineDependencies(spawnJobDependency, tmpJobHandle);
                }
            }
            aiSpawnBoltDataGroup.ResetFilter();

            return spawnJobHandle;
        }

    }
}
