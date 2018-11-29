using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;


namespace ECS_SpaceShooterDemo
{
    [UpdateAfter(typeof(PlayerMoveSystem))]
    public class PlayerSpawnBoltSystem : GameControllerJobComponentSystem
    {
        ComponentGroup boltSpawnerEntityDataGroup;


        struct PlayerMoveSpawnBoltDataGroup
        {
            public EntityArray entityArray;
            public ComponentDataArray<PlayerInputData> playerInputDataArray;
            public ComponentDataArray<PlayerMoveData> playerMoveDataArray;
            public ComponentDataArray<Position> playerPositionArray;
            public ComponentDataArray<PlayerSpawnBoltData> playerSpawnBoltDataArray;

            public readonly int Length; //required variable
        }
        [Inject]
        PlayerMoveSpawnBoltDataGroup playerMoveSpawnBoltDataGroup;

        [BurstCompile]
        struct PlayerSpawnBoltJob : IJobParallelFor
        {
            [ReadOnly]
            public EntityArray entityArray;
            [ReadOnly]
            public ComponentDataArray<PlayerInputData> playerInputDataArray;
            [ReadOnly]
            public ComponentDataArray<PlayerMoveData> playerMoveDataArray;

            [ReadOnly] 
            public ComponentDataArray<Position> playerPositionArray;

            public ComponentDataArray<PlayerSpawnBoltData> playerSpawnBoltDataArray;
            public NativeQueue<Entity>.Concurrent spawnBoltEntityQueue;

            public float currentTime;

            public void Execute(int index)
            {
                PlayerInputData playerInputData = playerInputDataArray[index];
                PlayerMoveData playerMoveData = playerMoveDataArray[index];
                Position playerPosition = playerPositionArray[index];
                PlayerSpawnBoltData playerSpawnBoltData = playerSpawnBoltDataArray[index];

                if(playerInputData.fireButtonPressed == 1 && currentTime >= playerSpawnBoltData.nextFireTime)
                {
                    playerSpawnBoltData.nextFireTime = currentTime + playerSpawnBoltData.fireRate;
                    spawnBoltEntityQueue.Enqueue(entityArray[index]);
                }

                playerSpawnBoltData.spawnPosition = playerPosition.Value + (playerMoveData.forwardDirection * playerSpawnBoltData.offset);
                playerSpawnBoltData.spawnDirection = playerMoveData.forwardDirection;

                playerSpawnBoltDataArray[index] = playerSpawnBoltData;
            }
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            boltSpawnerEntityDataGroup = GetComponentGroup(typeof(BoltSpawnerEntityData));        
        }        
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityArray boltSpawnerEntityDataArray = boltSpawnerEntityDataGroup.GetEntityArray();
            if (boltSpawnerEntityDataArray.Length == 0)
            {
                return inputDeps;
            }
                
            BoltSpawnerEntityData boltSpawnerEntityData = GetComponentDataFromEntity<BoltSpawnerEntityData>()[boltSpawnerEntityDataArray[0]];
            
            PlayerSpawnBoltJob playerSpawnBoltJob = new PlayerSpawnBoltJob
            {
                entityArray = playerMoveSpawnBoltDataGroup.entityArray,
                playerInputDataArray = playerMoveSpawnBoltDataGroup.playerInputDataArray,
                playerMoveDataArray = playerMoveSpawnBoltDataGroup.playerMoveDataArray,
                playerPositionArray = playerMoveSpawnBoltDataGroup.playerPositionArray,
                playerSpawnBoltDataArray = playerMoveSpawnBoltDataGroup.playerSpawnBoltDataArray,
                spawnBoltEntityQueue = boltSpawnerEntityData.playerBoltSpawnQueueConcurrent,
                currentTime = Time.time,
            };


            return playerSpawnBoltJob.Schedule(playerMoveSpawnBoltDataGroup.Length,
                                               MonoBehaviourECSBridge.Instance.GetJobBatchCount(playerMoveSpawnBoltDataGroup.Length),
                                               inputDeps);
        }

    }
}
