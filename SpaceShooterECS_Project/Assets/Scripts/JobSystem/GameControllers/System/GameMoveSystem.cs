using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.ECS.Rendering;
using Random = Unity.Mathematics.Random;


namespace ECS_SpaceShooterDemo
{
    [UpdateBefore(typeof(EntityOutOfBoundSystem))]
    public class GameMoveSystem : GameControllerJobComponentSystem
    {
        private ComponentGroup  moveDataComponentGroup;


        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            moveDataComponentGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                Any = new ComponentType[] {typeof(BoltMoveData), typeof(AIMoveData)}, 
                None = new ComponentType[] {},
                All = new ComponentType[]
                {
                    typeof(EntityInstanceRendererTransform), 
                    typeof(EntityBoundCenterData),
                    typeof(EntityBoundMinMaxData),
                    typeof(EntityBoundOffsetData),
                    typeof(EntityBoundExtendData),
                },
            });  
        }
        
        [BurstCompile]
        struct GameMoveJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            
            //Optional iComponentData
            public ArchetypeChunkComponentType<BoltMoveData>    boltMoveDataRW;
            public ArchetypeChunkComponentType<AIMoveData>    aiMoveDataRW;
            
            //Required iComponentData
            public ArchetypeChunkComponentType<EntityInstanceRendererTransform> renderTransformRW;
            public ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW;
            public ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO;

            
            public float deltaTime;


            void BoltMove( NativeArray<BoltMoveData> boltMoveDataArray, 
                            NativeArray<EntityInstanceRendererTransform> renderTransformArray,
                            NativeArray<EntityBoundCenterData> boundCenterDataArray,
                            NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray,
                            NativeArray<EntityBoundOffsetData> boundOffsetDataArray,
                            NativeArray<EntityBoundExtendData> boundExtendDataArray)
            {
                //The array size will be equal to the amount of entity 
                int dataCount = boltMoveDataArray.Length;
                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    BoltMoveData boltMoveData = boltMoveDataArray[dataIndex];
                    boltMoveData.position += (boltMoveData.speed * boltMoveData.forwardDirection * deltaTime);
                    boltMoveDataArray[dataIndex] = boltMoveData;

                  
                    EntityInstanceRendererTransform entityInstanceRenderTransform = renderTransformArray[dataIndex];
                    entityInstanceRenderTransform.matrix = new float4x4(quaternion.LookRotation(new float3(0, -1, 0), new float3(0, 0, 1)), boltMoveData.position);
                    renderTransformArray[dataIndex] = entityInstanceRenderTransform;
                    

                    EntityBoundCenterData entityBoundCenterData = boundCenterDataArray[dataIndex];
                    EntityBoundMinMaxData entityBoundMinMaxData = boundMinMaxDataArray[dataIndex];

                    entityBoundCenterData.centerPosition = boltMoveData.position + boundOffsetDataArray[dataIndex].offset;
                    entityBoundMinMaxData.min = entityBoundCenterData.centerPosition - boundExtendDataArray[dataIndex].extend;
                    entityBoundMinMaxData.max = entityBoundCenterData.centerPosition + boundExtendDataArray[dataIndex].extend;


                    boundCenterDataArray[dataIndex] = entityBoundCenterData;
                    boundMinMaxDataArray[dataIndex] = entityBoundMinMaxData;
                }
            }
            
              void AIMove( NativeArray<AIMoveData> aiMoveDataArray, 
                            NativeArray<EntityInstanceRendererTransform> renderTransformArray,
                            NativeArray<EntityBoundCenterData> boundCenterDataArray,
                            NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray,
                            NativeArray<EntityBoundOffsetData> boundOffsetDataArray,
                            NativeArray<EntityBoundExtendData> boundExtendDataArray)
            {
                int dataCount = aiMoveDataArray.Length;
                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    AIMoveData aiMoveData = aiMoveDataArray[dataIndex];
                    aiMoveData.position += (aiMoveData.speed * aiMoveData.forwardDirection * deltaTime);
                    aiMoveDataArray[dataIndex] = aiMoveData;
  
                    EntityInstanceRendererTransform entityInstanceRenderTransform = renderTransformArray[dataIndex];
                    entityInstanceRenderTransform.matrix = new float4x4(quaternion.LookRotation(aiMoveData.forwardDirection, new float3(0, 1, 0)), aiMoveData.position);
                    renderTransformArray[dataIndex] = entityInstanceRenderTransform;
                    
                    EntityBoundCenterData entityBoundCenterData = boundCenterDataArray[dataIndex];
                    EntityBoundMinMaxData entityBoundMinMaxData = boundMinMaxDataArray[dataIndex];

                    entityBoundCenterData.centerPosition = aiMoveData.position + boundOffsetDataArray[dataIndex].offset;
                    entityBoundMinMaxData.min = entityBoundCenterData.centerPosition - boundExtendDataArray[dataIndex].extend;
                    entityBoundMinMaxData.max = entityBoundCenterData.centerPosition + boundExtendDataArray[dataIndex].extend;


                    boundCenterDataArray[dataIndex] = entityBoundCenterData;
                    boundMinMaxDataArray[dataIndex] = entityBoundMinMaxData;
                }
            }          
            
            
            public void Execute(int chunkIndex)
            {
                //Each job looks at one chunk only, based on the chunkIndex passed as a parameter              
                ArchetypeChunk chunk = chunks[chunkIndex];

                if (chunk.Count == 0)
                {
                    return;
                }

                //Those are optional in our query, the array could be empty
                NativeArray<BoltMoveData> boltMoveDataArray = chunk.GetNativeArray(boltMoveDataRW);
                NativeArray<AIMoveData> aiMoveDataArray = chunk.GetNativeArray(aiMoveDataRW);
                
                //Those are required so they will always exist 
                NativeArray<EntityInstanceRendererTransform> renderTransformArray = chunk.GetNativeArray(renderTransformRW);
                NativeArray<EntityBoundCenterData> boundCenterDataArray = chunk.GetNativeArray(boundCenterDataRW);
                NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray = chunk.GetNativeArray(boundMinMaxDataRW);
                NativeArray<EntityBoundOffsetData> boundOffsetDataArray = chunk.GetNativeArray(boundOffsetDataRO);
                NativeArray<EntityBoundExtendData> boundExtendDataArray = chunk.GetNativeArray(boundExtendDataRO);

                //Depending on the type of object we have in the chunk, run different code
                //This branching is per chunk, NOT per entity. The array size is the amount of entity in a chunk, each function will go over all entities in the chunk 
                if (boltMoveDataArray.Length > 0)
                {
                    BoltMove(boltMoveDataArray, renderTransformArray, boundCenterDataArray, boundMinMaxDataArray, boundOffsetDataArray, boundExtendDataArray);
                }
                else if (aiMoveDataArray.Length > 0)
                {
                    AIMove(aiMoveDataArray, renderTransformArray, boundCenterDataArray, boundMinMaxDataArray, boundOffsetDataArray, boundExtendDataArray);
                }
                
            }
        }
        
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {         
            ArchetypeChunkComponentType<BoltMoveData> boltMoveDataRW = GetArchetypeChunkComponentType<BoltMoveData>(false);
            ArchetypeChunkComponentType<AIMoveData> aiMoveDataRW = GetArchetypeChunkComponentType<AIMoveData>(false);
            ArchetypeChunkComponentType<EntityInstanceRendererTransform> renderTransformRW = GetArchetypeChunkComponentType<EntityInstanceRendererTransform>(false);
            ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW = GetArchetypeChunkComponentType<EntityBoundCenterData>(false);
            ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW = GetArchetypeChunkComponentType<EntityBoundMinMaxData>(false);
            ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO = GetArchetypeChunkComponentType<EntityBoundOffsetData>(true);
            ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO = GetArchetypeChunkComponentType<EntityBoundExtendData>(true);
            
            
            NativeArray<ArchetypeChunk> moveDataChunk = moveDataComponentGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            if (moveDataChunk.Length == 0)
            {
                moveDataChunk.Dispose();
                return inputDeps;
            }

            GameMoveJob moveJob = new GameMoveJob
            {
                chunks = moveDataChunk,
                boltMoveDataRW = boltMoveDataRW,
                aiMoveDataRW = aiMoveDataRW,
                renderTransformRW = renderTransformRW,
                boundCenterDataRW = boundCenterDataRW,
                boundMinMaxDataRW = boundMinMaxDataRW,
                boundOffsetDataRO = boundOffsetDataRO,
                boundExtendDataRO = boundExtendDataRO,
                deltaTime = Time.deltaTime,
            };

            return moveJob.Schedule(moveDataChunk.Length,
                                    MonoBehaviourECSBridge.Instance.GetJobBatchCount(moveDataChunk.Length),
                                    inputDeps);
        }
        
    }
    
    
}
