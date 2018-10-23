using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS_SpaceShooterDemo
{
    [UpdateBefore(typeof(EntityToInstanceRendererTransform))]
    public class BoltMoveSystem : GameControllerJobComponentSystem
    {
        private ComponentGroup  boltMoveDataComponentGroup;
           
        [BurstCompile]
        struct BoltMoveJobChunck : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            
            public ArchetypeChunkComponentType<BoltMoveData> boltMoveDataRW;
            public ArchetypeChunkComponentType<EntityInstanceRenderData> renderDataRW;
            public ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW;
            public ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO;
            
            public float deltaTime;
            public float3 renderDataForward;
            
            public void Execute(int chunkIndex)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                int dataCount = chunk.Count;
                
                NativeArray<BoltMoveData> boltMoveDataArray = chunk.GetNativeArray(boltMoveDataRW);
                NativeArray<EntityInstanceRenderData> renderDataArray = chunk.GetNativeArray(renderDataRW);
                NativeArray<EntityBoundCenterData> boundCenterDataArray = chunk.GetNativeArray(boundCenterDataRW);
                NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray = chunk.GetNativeArray(boundMinMaxDataRW);
                NativeArray<EntityBoundOffsetData> boundOffsetDataArray = chunk.GetNativeArray(boundOffsetDataRO);
                NativeArray<EntityBoundExtendData> boundExtendDataArray = chunk.GetNativeArray(boundExtendDataRO);


                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    BoltMoveData boltMoveData = boltMoveDataArray[dataIndex];
                    boltMoveData.position += (boltMoveData.speed * boltMoveData.forwardDirection * deltaTime);
                    boltMoveDataArray[dataIndex] = boltMoveData;

                    EntityInstanceRenderData entityInstanceRenderData = renderDataArray[dataIndex];
                    entityInstanceRenderData.position = boltMoveData.position;
                    entityInstanceRenderData.forward = renderDataForward;

                    entityInstanceRenderData.up = -boltMoveData.forwardDirection;

                    renderDataArray[dataIndex] = entityInstanceRenderData;

                    EntityBoundCenterData entityBoundCenterData = boundCenterDataArray[dataIndex];
                    EntityBoundMinMaxData entityBoundMinMaxData = boundMinMaxDataArray[dataIndex];

                    entityBoundCenterData.centerPosition = boltMoveData.position + boundOffsetDataArray[dataIndex].offset;
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
            boltMoveDataComponentGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                Any = new ComponentType[] {}, 
                None = new ComponentType[] {},
                All = new ComponentType[]
                {
                    typeof(BoltMoveData), 
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
            ArchetypeChunkComponentType<BoltMoveData> boltMoveDataRW = GetArchetypeChunkComponentType<BoltMoveData>(false);
            ArchetypeChunkComponentType<EntityInstanceRenderData> renderDataRW = GetArchetypeChunkComponentType<EntityInstanceRenderData>(false);
            ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW = GetArchetypeChunkComponentType<EntityBoundCenterData>(false);
            ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW = GetArchetypeChunkComponentType<EntityBoundMinMaxData>(false);
            ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO = GetArchetypeChunkComponentType<EntityBoundOffsetData>(true);
            ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO = GetArchetypeChunkComponentType<EntityBoundExtendData>(true);
            
            
            NativeArray<ArchetypeChunk> boltMoveDataChunk = boltMoveDataComponentGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            if (boltMoveDataChunk.Length == 0)
            {
                boltMoveDataChunk.Dispose();
                return inputDeps;
            }

            BoltMoveJobChunck moveJob = new BoltMoveJobChunck
            {
                chunks = boltMoveDataChunk,
                boltMoveDataRW = boltMoveDataRW,
                renderDataRW = renderDataRW,
                boundCenterDataRW = boundCenterDataRW,
                boundMinMaxDataRW = boundMinMaxDataRW,
                boundOffsetDataRO = boundOffsetDataRO,
                boundExtendDataRO = boundExtendDataRO,
                deltaTime = Time.deltaTime,
                renderDataForward = new float3(0,-1, 0),
            };

            return moveJob.Schedule(boltMoveDataChunk.Length,
                MonoBehaviourECSBridge.Instance.GetJobBatchCount(boltMoveDataChunk.Length),
                inputDeps);
        }
    }
}
