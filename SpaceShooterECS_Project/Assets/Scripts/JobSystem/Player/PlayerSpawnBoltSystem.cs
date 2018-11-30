using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities.Editor;
using Unity.Transforms;


namespace ECS_SpaceShooterDemo
{
    [UpdateAfter(typeof(PlayerMoveSystem))]
    public class PlayerSpawnBoltSystem : GameControllerJobComponentSystem
    {
        ComponentGroup boltSpawnerEntityDataGroup;     
        ComponentGroup playerSpawnBoltDataGroup;
        
        [BurstCompile]
        struct PlayerSpawnBoltJob : IJobParallelFor
        {            
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            
            [ReadOnly] public ArchetypeChunkEntityType entityTypeRO;
            [ReadOnly] public ArchetypeChunkComponentType<PlayerInputData> playerInputDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<PlayerMoveData> playerMoveDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<Position> positionRO;
            public ArchetypeChunkComponentType<PlayerSpawnBoltData> playerSpawnBoltDataRW;
            
            public NativeQueue<Entity>.Concurrent spawnBoltEntityQueue;
            
            public float currentTime;

            public void Execute(int chunkIndex)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                int dataCount = chunk.Count;

                NativeArray<Entity> playerEntityArray = chunk.GetNativeArray(entityTypeRO);
                NativeArray<PlayerInputData> playerInputDataArray = chunk.GetNativeArray(playerInputDataRO);
                NativeArray<PlayerMoveData> playerMoveDataArray = chunk.GetNativeArray(playerMoveDataRO);
                NativeArray<Position> positionDataArray = chunk.GetNativeArray(positionRO);         
                NativeArray<PlayerSpawnBoltData> playerSpawnBoltDataArray = chunk.GetNativeArray(playerSpawnBoltDataRW);

                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    Entity playerEntity = playerEntityArray[dataIndex];
                    PlayerInputData playerInputData = playerInputDataArray[dataIndex];
                    PlayerMoveData playerMoveData = playerMoveDataArray[dataIndex];
                    Position playerPosition = positionDataArray[dataIndex];
                    PlayerSpawnBoltData playerSpawnBoltData = playerSpawnBoltDataArray[dataIndex];

                    if (playerInputData.fireButtonPressed == 1 && currentTime >= playerSpawnBoltData.nextFireTime)
                    {
                        playerSpawnBoltData.nextFireTime = currentTime + playerSpawnBoltData.fireRate;
                        spawnBoltEntityQueue.Enqueue(playerEntity);
                    }

                    playerSpawnBoltData.spawnPosition =
                        playerPosition.Value + (playerMoveData.forwardDirection * playerSpawnBoltData.offset);
                    playerSpawnBoltData.spawnDirection = playerMoveData.forwardDirection;

                    playerSpawnBoltDataArray[dataIndex] = playerSpawnBoltData;
                }
            }
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            boltSpawnerEntityDataGroup = GetComponentGroup(typeof(BoltSpawnerEntityData)); 
            playerSpawnBoltDataGroup = GetComponentGroup(typeof(PlayerInputData), typeof(PlayerMoveData), typeof(Position), typeof(PlayerSpawnBoltData));
        }        
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {                      
            EntityArray boltSpawnerEntityDataArray = boltSpawnerEntityDataGroup.GetEntityArray();
            if (boltSpawnerEntityDataArray.Length == 0)
            {
                return inputDeps;
            }
                
            BoltSpawnerEntityData boltSpawnerEntityData = GetComponentDataFromEntity<BoltSpawnerEntityData>()[boltSpawnerEntityDataArray[0]];
         
            ArchetypeChunkEntityType entityTypeRO = GetArchetypeChunkEntityType();
            ArchetypeChunkComponentType<PlayerInputData> playerInputDataRO = GetArchetypeChunkComponentType<PlayerInputData>(false);
            ArchetypeChunkComponentType<PlayerMoveData> playerMoveDataRO = GetArchetypeChunkComponentType<PlayerMoveData>(false);
            ArchetypeChunkComponentType<Position> positionRO = GetArchetypeChunkComponentType<Position>(false);
            ArchetypeChunkComponentType<PlayerSpawnBoltData> playerSpawnBoltDataRW = GetArchetypeChunkComponentType<PlayerSpawnBoltData>(false);

            
            //CreateArchetypeChunkArray runs inside a job, we can use a job handle to make dependency on that job
            //A NativeArray<ArchetypeChunk> is allocated with the correct size on the main thread and that's what is returned, we are responsible for de-allocating it (In this case using [DeallocateOnJobCompletion] in the move job)
            //The job scheduled by CreateArchetypeChunkArray fill that array with correct chunk information
            JobHandle createChunckArrayJobHandle = new JobHandle(); 
            NativeArray<ArchetypeChunk> playerSpawnBoltDataChunks = playerSpawnBoltDataGroup.CreateArchetypeChunkArray(Allocator.TempJob, out createChunckArrayJobHandle);
            
            //Special case when our query return no chunk at all
            if (playerSpawnBoltDataChunks.Length == 0)
            {
                createChunckArrayJobHandle.Complete();
                playerSpawnBoltDataChunks.Dispose();
                return inputDeps;
            }
            
            //Make sure our movejob is dependent on the job filling the array has completed
            JobHandle spawnJobDependency = JobHandle.CombineDependencies(inputDeps, createChunckArrayJobHandle);
            
            PlayerSpawnBoltJob playerSpawnBoltJob = new PlayerSpawnBoltJob
            {
                chunks = playerSpawnBoltDataChunks,
                entityTypeRO = entityTypeRO,
                playerInputDataRO = playerInputDataRO,
                playerMoveDataRO = playerMoveDataRO,
                positionRO = positionRO,
                playerSpawnBoltDataRW = playerSpawnBoltDataRW,
                spawnBoltEntityQueue = boltSpawnerEntityData.playerBoltSpawnQueueConcurrent,
                currentTime = Time.time,
            };

            return playerSpawnBoltJob.Schedule(playerSpawnBoltDataChunks.Length,
                MonoBehaviourECSBridge.Instance.GetJobBatchCount(playerSpawnBoltDataChunks.Length),
                spawnJobDependency);  
        }

    }
}
