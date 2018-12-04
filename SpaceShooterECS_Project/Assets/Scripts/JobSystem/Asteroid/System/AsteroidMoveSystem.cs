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
    public class AsteroidMoveSystem : GameControllerJobComponentSystem
    {
        private ComponentGroup  asteroidMoveDataComponentGroup;
                
        [BurstCompile]
        struct AsteroidMoveJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            
            
            public ArchetypeChunkComponentType<Position> positionRW;
            public ArchetypeChunkComponentType<Rotation> rotationRW;
            [ReadOnly] public ArchetypeChunkComponentType<AsteroidMoveData> asteroidMoveDataRO;
            public ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW;
            public ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO;
            
            public float deltaTime;
          
            public void Execute(int chunkIndex)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                int dataCount = chunk.Count;
                
                NativeArray<Position> positionDataArray = chunk.GetNativeArray(positionRW);
                NativeArray<Rotation> rotationDataArray = chunk.GetNativeArray(rotationRW);
                NativeArray<AsteroidMoveData> asteroidMoveDataArray = chunk.GetNativeArray(asteroidMoveDataRO);
                NativeArray<EntityBoundCenterData> boundCenterDataArray = chunk.GetNativeArray(boundCenterDataRW);
                NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray = chunk.GetNativeArray(boundMinMaxDataRW);
                NativeArray<EntityBoundOffsetData> boundOffsetDataArray = chunk.GetNativeArray(boundOffsetDataRO);
                NativeArray<EntityBoundExtendData> boundExtendDataArray = chunk.GetNativeArray(boundExtendDataRO);


                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    Position position = positionDataArray[dataIndex];
                    Rotation rotation = rotationDataArray[dataIndex];                   
                    AsteroidMoveData asteroidMoveData = asteroidMoveDataArray[dataIndex];
                    
                    position.Value += (asteroidMoveData.movementSpeed * deltaTime);
    
                    rotation.Value = math.mul(rotation.Value, quaternion.AxisAngle(asteroidMoveData.rotationAxis, Mathf.Deg2Rad * asteroidMoveData.rotationSpeed * deltaTime));
                    

                    positionDataArray[dataIndex] = position;
                    rotationDataArray[dataIndex] = rotation;

    
                    EntityBoundCenterData entityBoundCenterData = boundCenterDataArray[dataIndex];
                    EntityBoundMinMaxData entityBoundMinMaxData = boundMinMaxDataArray[dataIndex];
    
                    entityBoundCenterData.centerPosition = position.Value + boundOffsetDataArray[dataIndex].offset;
                    entityBoundMinMaxData.min = entityBoundCenterData.centerPosition - boundExtendDataArray[dataIndex].extend;
                    entityBoundMinMaxData.max = entityBoundCenterData.centerPosition + boundExtendDataArray[dataIndex].extend;
    
    
                    boundCenterDataArray[dataIndex] = entityBoundCenterData;
                    boundMinMaxDataArray[dataIndex] = entityBoundMinMaxData;                    
                    
                }
            }
        }


        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            asteroidMoveDataComponentGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                Any = new ComponentType[] {}, 
                None = new ComponentType[] {},
                All = new ComponentType[]
                {
                    typeof(Position),
                    typeof(Rotation),
                    typeof(AsteroidMoveData), 
                    typeof(EntityBoundCenterData),
                    typeof(EntityBoundMinMaxData),
                    typeof(EntityBoundOffsetData),
                    typeof(EntityBoundExtendData),
                },
            }); 
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {            
            ArchetypeChunkComponentType<Position> positionRW = GetArchetypeChunkComponentType<Position>(false);
            ArchetypeChunkComponentType<Rotation> rotationRW = GetArchetypeChunkComponentType<Rotation>(false);
            ArchetypeChunkComponentType<AsteroidMoveData> asteroidMoveDataRO = GetArchetypeChunkComponentType<AsteroidMoveData>(true);
            ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW = GetArchetypeChunkComponentType<EntityBoundCenterData>(false);
            ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW = GetArchetypeChunkComponentType<EntityBoundMinMaxData>(false);
            ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO = GetArchetypeChunkComponentType<EntityBoundOffsetData>(true);
            ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO = GetArchetypeChunkComponentType<EntityBoundExtendData>(true);


            //CreateArchetypeChunkArray runs inside a job, we can use a job handle to make dependency on that job
            //A NativeArray<ArchetypeChunk> is allocated with the correct size on the main thread and that's what is returned, we are responsible for de-allocating it (In this case using [DeallocateOnJobCompletion] in the move job)
            //The job scheduled by CreateArchetypeChunkArray fill that array with correct chunk information
            JobHandle createChunckArrayJobHandle; 
            NativeArray<ArchetypeChunk> asteroidMoveDataChunk = asteroidMoveDataComponentGroup.CreateArchetypeChunkArray(Allocator.TempJob, out createChunckArrayJobHandle);
            
            //Special case when our query return no chunk at all
            if (asteroidMoveDataChunk.Length == 0)
            {
                createChunckArrayJobHandle.Complete();
                asteroidMoveDataChunk.Dispose();
                return inputDeps;
            }
            
            
            //Make sure our movejob is dependent on the job filling the array has completed
            JobHandle moveJobDependency = JobHandle.CombineDependencies(inputDeps, createChunckArrayJobHandle);
            
            AsteroidMoveJob moveJob = new AsteroidMoveJob
            {
                chunks = asteroidMoveDataChunk,
                positionRW = positionRW,
                rotationRW = rotationRW,
                asteroidMoveDataRO = asteroidMoveDataRO,
                boundCenterDataRW = boundCenterDataRW,
                boundMinMaxDataRW = boundMinMaxDataRW,
                boundOffsetDataRO = boundOffsetDataRO,
                boundExtendDataRO = boundExtendDataRO,
                deltaTime = Time.deltaTime,
            };

            return moveJob.Schedule(asteroidMoveDataChunk.Length,
                MonoBehaviourECSBridge.Instance.GetJobBatchCount(asteroidMoveDataChunk.Length),
                moveJobDependency);            
            
        }
    }
}
