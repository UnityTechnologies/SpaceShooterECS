using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;


namespace ECS_SpaceShooterDemo
{
    [UpdateBefore(typeof(EntityOutOfBoundSystem))]
    public class PlayerMoveSystem : GameControllerJobComponentSystem
    {
        private ComponentGroup playerMoveDataGroup;
        
        [BurstCompile]
        struct PlayerMoveJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            
            [ReadOnly] public ArchetypeChunkComponentType<PlayerInputData> playerInputDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<PlayerMoveData> playerMoveDataRO;
            public ArchetypeChunkComponentType<Position> positionRW;
            public ArchetypeChunkComponentType<Rotation> rotationRW;
            public ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW;
            public ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO;
            
            
            public float deltaTime;

            
            public void Execute(int chunkIndex)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                int dataCount = chunk.Count;
                
                NativeArray<PlayerInputData> playerInputDataArray = chunk.GetNativeArray(playerInputDataRO);
                NativeArray<PlayerMoveData> playerMoveDataArray = chunk.GetNativeArray(playerMoveDataRO);
                NativeArray<Position> positionDataArray = chunk.GetNativeArray(positionRW);
                NativeArray<Rotation> rotationDataArray = chunk.GetNativeArray(rotationRW);                
                NativeArray<EntityBoundCenterData> boundCenterDataArray = chunk.GetNativeArray(boundCenterDataRW);
                NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray = chunk.GetNativeArray(boundMinMaxDataRW);
                NativeArray<EntityBoundOffsetData> boundOffsetDataArray = chunk.GetNativeArray(boundOffsetDataRO);
                NativeArray<EntityBoundExtendData> boundExtendDataArray = chunk.GetNativeArray(boundExtendDataRO);

                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    PlayerInputData playerInputData = playerInputDataArray[dataIndex];
                    PlayerMoveData playerMoveData = playerMoveDataArray[dataIndex];
                    Position playerPosition = positionDataArray[dataIndex];
                    Rotation playerRotation = rotationDataArray[dataIndex];

                    float3 shipUp = new float3(0, 1, 0) +
                                    (playerMoveData.rightDirection * playerInputData.inputMovementDirection.x);

                    float3 movementVector = playerMoveData.rightDirection * playerInputData.inputMovementDirection.x
                                            + playerMoveData.forwardDirection *
                                            playerInputData.inputMovementDirection.z;

                    playerPosition.Value += (playerMoveData.speed * movementVector * deltaTime);

                    playerPosition.Value = math.clamp(playerPosition.Value, playerMoveData.minBoundary,
                        playerMoveData.maxBoundary);

                    playerRotation.Value = quaternion.LookRotation(playerMoveData.forwardDirection, shipUp);

                    positionDataArray[dataIndex] = playerPosition;
                    rotationDataArray[dataIndex] = playerRotation;


                    EntityBoundCenterData entityBoundCenterData = boundCenterDataArray[dataIndex];
                    EntityBoundMinMaxData entityBoundMinMaxData = boundMinMaxDataArray[dataIndex];

                    entityBoundCenterData.centerPosition =
                        playerPosition.Value + boundOffsetDataArray[dataIndex].offset;
                    entityBoundMinMaxData.min =
                        entityBoundCenterData.centerPosition - boundExtendDataArray[dataIndex].extend;
                    entityBoundMinMaxData.max =
                        entityBoundCenterData.centerPosition + boundExtendDataArray[dataIndex].extend;


                    boundCenterDataArray[dataIndex] = entityBoundCenterData;
                    boundMinMaxDataArray[dataIndex] = entityBoundMinMaxData;
                }
            }
        }


        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            playerMoveDataGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                Any = new ComponentType[] {}, 
                None = new ComponentType[] {},
                All = new ComponentType[]
                {
                    typeof(PlayerInputData),
                    typeof(PlayerMoveData),
                    typeof(Position),
                    typeof(Rotation),
                    typeof(EntityBoundCenterData),
                    typeof(EntityBoundMinMaxData),
                    typeof(EntityBoundOffsetData),
                    typeof(EntityBoundExtendData),
                },
            }); 
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            ArchetypeChunkComponentType<PlayerInputData> playerInputDataRO = GetArchetypeChunkComponentType<PlayerInputData>(true);
            ArchetypeChunkComponentType<PlayerMoveData> playerMoveDataRO = GetArchetypeChunkComponentType<PlayerMoveData>(true);
            ArchetypeChunkComponentType<Position> positionRW = GetArchetypeChunkComponentType<Position>(false);
            ArchetypeChunkComponentType<Rotation> rotationRW = GetArchetypeChunkComponentType<Rotation>(false);
            ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW = GetArchetypeChunkComponentType<EntityBoundCenterData>(false);
            ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW = GetArchetypeChunkComponentType<EntityBoundMinMaxData>(false);
            ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO = GetArchetypeChunkComponentType<EntityBoundOffsetData>(true);
            ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO = GetArchetypeChunkComponentType<EntityBoundExtendData>(true);
            
            //CreateArchetypeChunkArray runs inside a job, we can use a job handle to make dependency on that job
            //A NativeArray<ArchetypeChunk> is allocated with the correct size on the main thread and that's what is returned, we are responsible for de-allocating it (In this case using [DeallocateOnJobCompletion] in the move job)
            //The job scheduled by CreateArchetypeChunkArray fill that array with correct chunk information
            JobHandle createChunckArrayJobHandle; 
            NativeArray<ArchetypeChunk> playerMoveDataChunk = playerMoveDataGroup.CreateArchetypeChunkArray(Allocator.TempJob, out createChunckArrayJobHandle);
            
            //Special case when our query return no chunk at all
            if (playerMoveDataChunk.Length == 0)
            {
                createChunckArrayJobHandle.Complete();
                playerMoveDataChunk.Dispose();
                return inputDeps;
            }
            
            //Make sure our movejob is dependent on the job filling the array has completed
            JobHandle moveJobDependency = JobHandle.CombineDependencies(inputDeps, createChunckArrayJobHandle);
            
            PlayerMoveJob playerMoveJob = new PlayerMoveJob
            {
                chunks = playerMoveDataChunk,
                playerInputDataRO = playerInputDataRO,
                playerMoveDataRO = playerMoveDataRO,
                positionRW = positionRW,
                rotationRW = rotationRW,
                boundCenterDataRW = boundCenterDataRW,
                boundMinMaxDataRW = boundMinMaxDataRW,
                boundOffsetDataRO = boundOffsetDataRO,
                boundExtendDataRO = boundExtendDataRO,
                deltaTime = Time.deltaTime,
            };

            return playerMoveJob.Schedule(playerMoveDataChunk.Length,
                MonoBehaviourECSBridge.Instance.GetJobBatchCount(playerMoveDataChunk.Length),
                moveJobDependency);  
        }
    }
}
