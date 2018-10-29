using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.ECS.Rendering;

namespace ECS_SpaceShooterDemo
{
    [UpdateAfter(typeof(GameMoveSystem))]
    [UpdateBefore(typeof(ECS_SpaceShooterDemo.EntityOutOfBoundSystem))]
    public class AISpawnBoltSystem : GameControllerJobComponentSystem
    {
        [Inject]
        BoltSpawnerEntityDataGroup boltSpawnerEntityDataGroup;


        struct AIMoveSpawnBoltDataGroup
        {
            public EntityArray entityArray;
            public ComponentDataArray<AIMoveData> aiMoveDataArray;
            public ComponentDataArray<AISpawnBoltData> aiSpawnBoltDataArray;

            public readonly int Length; //required variable
        }
        [Inject]
        AIMoveSpawnBoltDataGroup aiMoveSpawnBoltDataGroup;

        [BurstCompileAttribute(Accuracy.Med, Support.Relaxed)]
        struct AISpawnBoltJob : IJobParallelFor
        {
            [ReadOnly]
            public EntityArray entityArray;
            [ReadOnly]
            public ComponentDataArray<AIMoveData> aiMoveDataArray;

            public ComponentDataArray<AISpawnBoltData> aiSpawnBoltDataArray;
            public NativeQueue<Entity>.Concurrent spawnBoltEntityQueue;

            public float deltaTime;

            public void Execute(int index)
            {
                AIMoveData aiMoveData = aiMoveDataArray[index];
                AISpawnBoltData aiSpawnBoltData = aiSpawnBoltDataArray[index];

                aiSpawnBoltData.spawnPosition = aiMoveData.position + (aiMoveData.forwardDirection * aiSpawnBoltData.offset);
                aiSpawnBoltData.spawnDirection = aiMoveData.forwardDirection;
                aiSpawnBoltData.timeSinceFire += deltaTime;

                if (aiSpawnBoltData.timeSinceFire > aiSpawnBoltData.fireRate)
                {
                    spawnBoltEntityQueue.Enqueue(entityArray[index]);
                    aiSpawnBoltData.timeSinceFire = 0.0f;
                }
                aiSpawnBoltDataArray[index] = aiSpawnBoltData;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            AISpawnBoltJob aiSpawnBoltJob = new AISpawnBoltJob
            {
                entityArray = aiMoveSpawnBoltDataGroup.entityArray,
                aiMoveDataArray = aiMoveSpawnBoltDataGroup.aiMoveDataArray,
                aiSpawnBoltDataArray = aiMoveSpawnBoltDataGroup.aiSpawnBoltDataArray,
                spawnBoltEntityQueue = boltSpawnerEntityDataGroup.boltSpawnerEntityData[0].aiBoltSpawnQueueConcurrent,
                deltaTime = Time.deltaTime
            };

            return aiSpawnBoltJob.Schedule(aiMoveSpawnBoltDataGroup.Length,
                                           MonoBehaviourECSBridge.Instance.GetJobBatchCount(aiMoveSpawnBoltDataGroup.Length),
                                           inputDeps);
        }

    }
}
