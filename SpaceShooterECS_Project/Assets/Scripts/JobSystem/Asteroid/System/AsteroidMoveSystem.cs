using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS_SpaceShooterDemo
{
    [UpdateBefore(typeof(EntityToInstanceRendererTransform))]
    public class AsteroidMoveSystem : GameControllerJobComponentSystem
    {
        private ComponentGroup  asteroidMoveDataComponentGroup;
                
        [BurstCompile]
        struct AsteroidMoveJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            
            public ArchetypeChunkComponentType<AsteroidMoveData> asteroidMoveDataRW;
            public ArchetypeChunkComponentType<EntityInstanceRenderData> renderDataRW;
            public ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW;
            public ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO;
            
            public float deltaTime;
          
            public void Execute(int chunkIndex)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                int dataCount = chunk.Count;
                
                NativeArray<AsteroidMoveData> asteroidMoveDataArray = chunk.GetNativeArray(asteroidMoveDataRW);
                NativeArray<EntityInstanceRenderData> renderDataArray = chunk.GetNativeArray(renderDataRW);
                NativeArray<EntityBoundCenterData> boundCenterDataArray = chunk.GetNativeArray(boundCenterDataRW);
                NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray = chunk.GetNativeArray(boundMinMaxDataRW);
                NativeArray<EntityBoundOffsetData> boundOffsetDataArray = chunk.GetNativeArray(boundOffsetDataRO);
                NativeArray<EntityBoundExtendData> boundExtendDataArray = chunk.GetNativeArray(boundExtendDataRO);


                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    
                    AsteroidMoveData asteroidMoveData = asteroidMoveDataArray[dataIndex];
                    asteroidMoveData.position += (asteroidMoveData.speed * asteroidMoveData.forwardDirection * deltaTime);
    
                    //https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
                    float rotationAngle = Mathf.Deg2Rad * asteroidMoveData.rotationSpeed * deltaTime;
                    float cosValue = math.cos(rotationAngle);
                    float sinValue = math.sin(rotationAngle);
                    float3 crossVector = math.cross(asteroidMoveData.rotationAxis, asteroidMoveData.renderForward);
                    float dotValue = math.dot(asteroidMoveData.rotationAxis, asteroidMoveData.renderForward);
    
    
    
                    asteroidMoveData.renderForward = (asteroidMoveData.renderForward * cosValue)
                                                        + (crossVector * sinValue)
                                                        + (asteroidMoveData.rotationAxis * dotValue * (1.0f - cosValue));
    
    
                    asteroidMoveDataArray[dataIndex] = asteroidMoveData;
    
                    EntityInstanceRenderData entityInstanceRenderData = renderDataArray[dataIndex];
    
                    entityInstanceRenderData.position = asteroidMoveData.position;
                    entityInstanceRenderData.forward = asteroidMoveData.renderForward;
                    entityInstanceRenderData.up = new float3(0, 1, 0);
    
                    renderDataArray[dataIndex] = entityInstanceRenderData;
    
                    EntityBoundCenterData entityBoundCenterData = boundCenterDataArray[dataIndex];
                    EntityBoundMinMaxData entityBoundMinMaxData = boundMinMaxDataArray[dataIndex];
    
                    entityBoundCenterData.centerPosition = asteroidMoveData.position + boundOffsetDataArray[dataIndex].offset;
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
                    typeof(AsteroidMoveData), 
                    typeof(EntityInstanceRenderData), 
                    typeof(EntityBoundCenterData),
                    typeof(EntityBoundMinMaxData),
                    typeof(EntityBoundOffsetData),
                    typeof(EntityBoundExtendData),
                },
            }); 
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {            
            ArchetypeChunkComponentType<AsteroidMoveData> asteroidMoveDataRW = GetArchetypeChunkComponentType<AsteroidMoveData>(false);
            ArchetypeChunkComponentType<EntityInstanceRenderData> renderDataRW = GetArchetypeChunkComponentType<EntityInstanceRenderData>(false);
            ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW = GetArchetypeChunkComponentType<EntityBoundCenterData>(false);
            ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW = GetArchetypeChunkComponentType<EntityBoundMinMaxData>(false);
            ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO = GetArchetypeChunkComponentType<EntityBoundOffsetData>(true);
            ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO = GetArchetypeChunkComponentType<EntityBoundExtendData>(true);


            //CreateArchetypeChunkArray runs inside a job, we can use a job handle to make dependency on that job
            //A NativeArray<ArchetypeChunk> is allocated with teh correct size on the main thread and that's what is returned, we are responsible for de-allocating it (In this case using [DeallocateOnJobCompletion] in the move job)
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
                asteroidMoveDataRW = asteroidMoveDataRW,
                renderDataRW = renderDataRW,
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
