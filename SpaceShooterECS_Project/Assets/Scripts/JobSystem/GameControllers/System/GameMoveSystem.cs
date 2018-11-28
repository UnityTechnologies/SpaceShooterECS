using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
                    typeof(Position), 
                    typeof(Rotation), 
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
            [ReadOnly] public  ArchetypeChunkComponentType<BoltMoveData>    boltMoveDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<AIMoveData>    aiMoveDataRO;
            
            //Required iComponentData
            public ArchetypeChunkComponentType<Position> positionRW;
            public ArchetypeChunkComponentType<Rotation> rotationRW;    
            public ArchetypeChunkComponentType<EntityBoundCenterData> boundCenterDataRW;
            public ArchetypeChunkComponentType<EntityBoundMinMaxData> boundMinMaxDataRW;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundOffsetData> boundOffsetDataRO;
            [ReadOnly] public ArchetypeChunkComponentType<EntityBoundExtendData> boundExtendDataRO;

            
            public float deltaTime;


            void BoltMove( NativeArray<BoltMoveData> boltMoveDataArray, 
                            NativeArray<Position> positionArray,
                            NativeArray<Rotation> rotationArray,
                            NativeArray<EntityBoundCenterData> boundCenterDataArray,
                            NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray,
                            NativeArray<EntityBoundOffsetData> boundOffsetDataArray,
                            NativeArray<EntityBoundExtendData> boundExtendDataArray)
            {
                //The array size will be equal to the amount of entity 
                int dataCount = boltMoveDataArray.Length;
                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    Position position = positionArray[dataIndex];
                    Rotation rotation = rotationArray[dataIndex];                   
                    BoltMoveData boltMoveData = boltMoveDataArray[dataIndex];

                    float3 forwardDirection = boltMoveData.forwardDirection;
                    
                    position.Value += (boltMoveData.speed * forwardDirection * deltaTime);
                    positionArray[dataIndex] = position;
                                                        
                    EntityBoundCenterData entityBoundCenterData = boundCenterDataArray[dataIndex];
                    EntityBoundMinMaxData entityBoundMinMaxData = boundMinMaxDataArray[dataIndex];

                    entityBoundCenterData.centerPosition = position.Value + boundOffsetDataArray[dataIndex].offset;
                    entityBoundMinMaxData.min = entityBoundCenterData.centerPosition - boundExtendDataArray[dataIndex].extend;
                    entityBoundMinMaxData.max = entityBoundCenterData.centerPosition + boundExtendDataArray[dataIndex].extend;

                    boundCenterDataArray[dataIndex] = entityBoundCenterData;
                    boundMinMaxDataArray[dataIndex] = entityBoundMinMaxData;
                }
            }
            
              void AIMove( NativeArray<AIMoveData> aiMoveDataArray, 
                          NativeArray<Position> positionArray,
                          NativeArray<Rotation> rotationArray,
                            NativeArray<EntityBoundCenterData> boundCenterDataArray,
                            NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray,
                            NativeArray<EntityBoundOffsetData> boundOffsetDataArray,
                            NativeArray<EntityBoundExtendData> boundExtendDataArray)
            {
                int dataCount = aiMoveDataArray.Length;
                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    Position position = positionArray[dataIndex];
                    Rotation rotation = rotationArray[dataIndex];                                  
                    AIMoveData aiMoveData = aiMoveDataArray[dataIndex];
                    
                    
                    float3 forwardDirection = math.forward(rotation.Value);
                    
                    position.Value += (aiMoveData.speed * forwardDirection * deltaTime);
                    positionArray[dataIndex] = position;
                  
                    EntityBoundCenterData entityBoundCenterData = boundCenterDataArray[dataIndex];
                    EntityBoundMinMaxData entityBoundMinMaxData = boundMinMaxDataArray[dataIndex];

                    entityBoundCenterData.centerPosition = position.Value + boundOffsetDataArray[dataIndex].offset;
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
                NativeArray<BoltMoveData> boltMoveDataArray = chunk.GetNativeArray(boltMoveDataRO);
                NativeArray<AIMoveData> aiMoveDataArray = chunk.GetNativeArray(aiMoveDataRO);
                
                //Those are required so they will always exist 
                NativeArray<Position> positionArray = chunk.GetNativeArray(positionRW);
                NativeArray<Rotation> rotationArray = chunk.GetNativeArray(rotationRW);
                NativeArray<EntityBoundCenterData> boundCenterDataArray = chunk.GetNativeArray(boundCenterDataRW);
                NativeArray<EntityBoundMinMaxData> boundMinMaxDataArray = chunk.GetNativeArray(boundMinMaxDataRW);
                NativeArray<EntityBoundOffsetData> boundOffsetDataArray = chunk.GetNativeArray(boundOffsetDataRO);
                NativeArray<EntityBoundExtendData> boundExtendDataArray = chunk.GetNativeArray(boundExtendDataRO);

                //Depending on the type of object we have in the chunk, run different code
                //This branching is per chunk, NOT per entity. The array size is the amount of entity in a chunk, each function will go over all entities in the chunk 
                if (boltMoveDataArray.Length > 0)
                {
                    BoltMove(boltMoveDataArray, positionArray, rotationArray, boundCenterDataArray, boundMinMaxDataArray, boundOffsetDataArray, boundExtendDataArray);
                }
                else if (aiMoveDataArray.Length > 0)
                {
                    AIMove(aiMoveDataArray, positionArray, rotationArray, boundCenterDataArray, boundMinMaxDataArray, boundOffsetDataArray, boundExtendDataArray);
                }
                
            }
        }
        
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {         
            ArchetypeChunkComponentType<BoltMoveData> boltMoveDataRO = GetArchetypeChunkComponentType<BoltMoveData>(true);
            ArchetypeChunkComponentType<AIMoveData> aiMoveDataRO = GetArchetypeChunkComponentType<AIMoveData>(true);
            ArchetypeChunkComponentType<Position> positionRW = GetArchetypeChunkComponentType<Position>(false);
            ArchetypeChunkComponentType<Rotation> rotationRW = GetArchetypeChunkComponentType<Rotation>(false);    
            
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
                boltMoveDataRO = boltMoveDataRO,
                aiMoveDataRO = aiMoveDataRO,
                positionRW = positionRW,
                rotationRW = rotationRW,                           
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
