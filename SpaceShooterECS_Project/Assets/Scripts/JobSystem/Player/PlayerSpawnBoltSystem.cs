using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.ECS.Rendering;

namespace ECS_SpaceShooterDemo
{
    [UpdateAfter(typeof(PlayerMoveSystem))]
    [UpdateBefore(typeof(ECS_SpaceShooterDemo.EntityToInstanceRendererTransform))]
    public class PlayerSpawnBoltSystem : GameControllerJobComponentSystem
    {
        [Inject]
        BoltSpawnerEntityDataGroup boltSpawnerEntityDataGroup;


        struct PlayerMoveSpawnBoltDataGroup
        {
            public EntityArray entityArray;
            public ComponentDataArray<PlayerInputData> playerInputDataArray;
            public ComponentDataArray<PlayerMoveData> playerMoveDataArray;
            public ComponentDataArray<PlayerSpawnBoltData> playerSpawnBoltDataArray;

            public SubtractiveComponent<EntityPrefabData> prefabData;
            public int Length; //required variable
        }
        [Inject]
        PlayerMoveSpawnBoltDataGroup playerMoveSpawnBoltDataGroup;

        [ComputeJobOptimizationAttribute(Accuracy.Med, Support.Relaxed)]
        struct PlayerSpawnBoltJob : IJobParallelFor
        {
            [ReadOnly]
            public EntityArray entityArray;
            [ReadOnly]
            public ComponentDataArray<PlayerInputData> playerInputDataArray;
            [ReadOnly]
            public ComponentDataArray<PlayerMoveData> playerMoveDataArray;

            public ComponentDataArray<PlayerSpawnBoltData> playerSpawnBoltDataArray;
            public NativeQueue<Entity>.Concurrent spawnBoltEntityQueue;

            public float currentTime;

            public void Execute(int index)
            {
                PlayerInputData playerInputData = playerInputDataArray[index];
                PlayerMoveData playerMoveData = playerMoveDataArray[index];
                PlayerSpawnBoltData playerSpawnBoltData = playerSpawnBoltDataArray[index];

                if(playerInputData.fireButtonPressed == 1 && currentTime >= playerSpawnBoltData.nextFireTime)
                {
                    playerSpawnBoltData.nextFireTime = currentTime + playerSpawnBoltData.fireRate;
                    spawnBoltEntityQueue.Enqueue(entityArray[index]);
                }

                playerSpawnBoltData.spawnPosition = playerMoveData.position + (playerMoveData.forwardDirection * playerSpawnBoltData.offset);
                playerSpawnBoltData.spawnDirection = playerMoveData.forwardDirection;

                playerSpawnBoltDataArray[index] = playerSpawnBoltData;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            PlayerSpawnBoltJob playerSpawnBoltJob = new PlayerSpawnBoltJob
            {
                entityArray = playerMoveSpawnBoltDataGroup.entityArray,
                playerInputDataArray = playerMoveSpawnBoltDataGroup.playerInputDataArray,
                playerMoveDataArray = playerMoveSpawnBoltDataGroup.playerMoveDataArray,
                playerSpawnBoltDataArray = playerMoveSpawnBoltDataGroup.playerSpawnBoltDataArray,
                spawnBoltEntityQueue = boltSpawnerEntityDataGroup.boltSpawnerEntityData[0].playerBoltSpawnQueueConcurrent,
                currentTime = Time.time,
            };


            return playerSpawnBoltJob.Schedule(playerMoveSpawnBoltDataGroup.Length,
                                               MonoBehaviourECSBridge.Instance.GetJobBatchCount(playerMoveSpawnBoltDataGroup.Length),
                                               inputDeps);
        }

    }
}
