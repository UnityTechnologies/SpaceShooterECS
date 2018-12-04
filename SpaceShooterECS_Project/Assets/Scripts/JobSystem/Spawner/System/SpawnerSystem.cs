using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

namespace ECS_SpaceShooterDemo
{
    [UpdateInGroup(typeof(EntityManagementGroup))]
    [UpdateAfter(typeof(DestroyEntitySystem))]
    public class SpawnerSystem : GameControllerComponentSystem
    {
        private ComponentGroup spawnerDataGroup;
        
        
        struct SpawnerSpawnInfo
        {
            public float spawnYPosition;
            public int hazardIndexToSpawn;
            public int isBackgroundSpawn;
        }

        private NativeList<SpawnerSpawnInfo> spawnerSpawnInfoList;

        private Unity.Mathematics.Random randomGenerator;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            spawnerDataGroup = GetComponentGroup(typeof(SpawnerPositionData), typeof(SpawnerHazardData),
                typeof(SpawnerSpawnData));
            
            //to seed our randomGenerator, we randomly fill a byte array, then convert it to uint32
            byte[] randomBytes = new byte[4];
            new System.Random().NextBytes(randomBytes);
            uint randomGeneratorSeed = System.BitConverter.ToUInt32(randomBytes, 0);
                        
            randomGenerator = new Unity.Mathematics.Random(randomGeneratorSeed);           
            spawnerSpawnInfoList = new NativeList<SpawnerSpawnInfo>(1000, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            spawnerSpawnInfoList.Dispose();

            base.OnDestroyManager();
        }

        protected override void OnUpdate()
        {
            ArchetypeChunkComponentType<SpawnerPositionData> spawnerPositionDataRO = GetArchetypeChunkComponentType<SpawnerPositionData>(true);
            ArchetypeChunkComponentType<SpawnerHazardData> spawnerHazardDataRO = GetArchetypeChunkComponentType<SpawnerHazardData>(true);
            ArchetypeChunkComponentType<SpawnerSpawnData> spawnerSpawnDataRW = GetArchetypeChunkComponentType<SpawnerSpawnData>(false);
            
            
            NativeArray<ArchetypeChunk> spawnerDataChunkArray = spawnerDataGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            if (spawnerDataChunkArray.Length == 0)
            {
                spawnerDataChunkArray.Dispose();
                return;
            }

            //Go over all our spawners and figure out if any new entity need to be spawned
            //Add the spawning info to spawnerSpawnInfoList 
            for (int chunkIndex = 0; chunkIndex < spawnerDataChunkArray.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = spawnerDataChunkArray[chunkIndex];
                int dataCount = chunk.Count;

                NativeArray<SpawnerPositionData> spawnerPositionDataArray = chunk.GetNativeArray(spawnerPositionDataRO);
                NativeArray<SpawnerHazardData> spawnerHazardDataArray = chunk.GetNativeArray(spawnerHazardDataRO);
                NativeArray<SpawnerSpawnData> spawnerSpawnDataArray = chunk.GetNativeArray(spawnerSpawnDataRW);

                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    SpawnerPositionData spawnerPositionData = spawnerPositionDataArray[dataIndex];
                    SpawnerHazardData spawnerHazardData = spawnerHazardDataArray[dataIndex];
                    SpawnerSpawnData spawnerSpawnData = spawnerSpawnDataArray[dataIndex];

                    if (spawnerHazardData.hazardIndexArrayLength == 0)
                    {
                        continue;
                    }

                    spawnerSpawnData.timeSinceLastSpawn += Time.deltaTime;

                    while (spawnerSpawnData.timeSinceLastSpawn >= spawnerSpawnData.spawnDelay)
                    {
                        spawnerSpawnData.timeSinceLastSpawn -= spawnerSpawnData.spawnDelay;
                        float yPositionSpawn = spawnerPositionData.position.y;

                        //Get a random index to spawn, the range depend on the amount of hazards we set in the editor
                        int hazardToSpawnIndex =
                            randomGenerator.NextInt(0, spawnerHazardData.hazardIndexArrayLength);

                        SpawnerSpawnInfo spawnInfo = new SpawnerSpawnInfo
                        {
                            spawnYPosition = yPositionSpawn,
                            hazardIndexToSpawn = hazardToSpawnIndex,
                            isBackgroundSpawn = spawnerHazardData.isBackgroundSpawner,
                        };

                        spawnerSpawnInfoList.Add(spawnInfo);
                    }

                    spawnerSpawnDataArray[dataIndex] = spawnerSpawnData;
                    
                }
            }
            
            spawnerDataChunkArray.Dispose();
            

            float3 cameraPosition = MonoBehaviourECSBridge.Instance.gameCamera.transform.position;


            float3 forwardDirection = new float3(0.0f, 0.0f, 1.0f);
            //We use the view frustum height/witdh at our spawner y position to figure out where to spawn the new entities
            //The calculation assume a camera pointing down (no angle)
            for (int i = 0; i < spawnerSpawnInfoList.Length; i++)
            {
                SpawnerSpawnInfo spawnInfo = spawnerSpawnInfoList[i];

                float yPosition = spawnInfo.spawnYPosition;
                float ydeltaFromCamera = Mathf.Abs(yPosition - cameraPosition.y);
                float halfFrustumHeight = ydeltaFromCamera * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);

                //Spawn outside of the view frustrum to avoid any "popup"
                halfFrustumHeight *= 1.05f;

                float halfFrustumWidth = halfFrustumHeight * MonoBehaviourECSBridge.Instance.gameCamera.aspect;

                //Enemy/Hazard are spawned from the top of the screen, Allies are spawned from the bottom
                float3 spawnPositionHazard = new Vector3(randomGenerator.NextFloat(cameraPosition.x - halfFrustumWidth, cameraPosition.x + halfFrustumWidth), 
                                                         yPosition, 
                                                         cameraPosition.z + halfFrustumHeight);
                float3 spawnPositionAlly = new Vector3(randomGenerator.NextFloat(cameraPosition.x - halfFrustumWidth, cameraPosition.x + halfFrustumWidth), 
                                                         yPosition, 
                                                         cameraPosition.z - halfFrustumHeight);

                //Spawn the hazard using the index we randomly generated
                Entity newHazardEntity = EntityManager.Instantiate(MonoBehaviourECSBridge.Instance.GetPrefabHazardEntity(spawnInfo.hazardIndexToSpawn, spawnInfo.isBackgroundSpawn == 1));
                //Make sure to remove the prefab "tag" data component
               // EntityManager.RemoveComponent<Prefab>(newHazardEntity);

                //Based on the type of entity spawned, set the data needed for it
                EntityTypeData entityTypeData = EntityManager.GetSharedComponentData<EntityTypeData>(newHazardEntity);
                switch (entityTypeData.entityType)
                {
                    case EntityTypeData.EntityType.Asteroid:
                        {
                            
                            Vector3 spawnRenderForward = new Vector3(randomGenerator.NextFloat(), randomGenerator.NextFloat(), 1.0f);
                            spawnRenderForward.Normalize();
                            Vector3 spawnRotationAxis = new Vector3(randomGenerator.NextFloat(), 1.0f, randomGenerator.NextFloat());
                            spawnRotationAxis.Normalize();

                            Position newPosition = new Position()
                            {
                                Value = spawnPositionHazard,
                            };
                            EntityManager.SetComponentData<Position>(newHazardEntity, newPosition);

                            Rotation newRotation = new Rotation()
                            {
                                Value = quaternion.LookRotation(spawnRenderForward, spawnRotationAxis),
                            };
                            EntityManager.SetComponentData<Rotation>(newHazardEntity, newRotation);
            
                            AsteroidMoveData moveData = EntityManager.GetComponentData<AsteroidMoveData>(newHazardEntity);
                            moveData.movementSpeed = -forwardDirection * 5.0f;
                            moveData.rotationAxis = spawnRotationAxis;
                            EntityManager.SetComponentData<AsteroidMoveData>(newHazardEntity, moveData);
                                              
                        }
                        break;
                    case EntityTypeData.EntityType.EnemyShip:
                        {
                            Position newPosition = new Position()
                            {
                                Value = spawnPositionHazard,
                            };
                            EntityManager.SetComponentData<Position>(newHazardEntity, newPosition);

                            Rotation newRotation = new Rotation()
                            {
                                Value = quaternion.LookRotation(-forwardDirection, new float3(0.0f, 1.0f, 0.0f)),
                            };
                            EntityManager.SetComponentData<Rotation>(newHazardEntity, newRotation);
                        }
                        break;
                    case EntityTypeData.EntityType.AllyShip:
                        {
                            Position newPosition = new Position()
                            {
                                Value = spawnPositionAlly,
                            };
                            EntityManager.SetComponentData<Position>(newHazardEntity, newPosition);

                            Rotation newRotation = new Rotation()
                            {
                                Value = quaternion.LookRotation(forwardDirection, new float3(0.0f, 1.0f, 0.0f)),
                            };
                            EntityManager.SetComponentData<Rotation>(newHazardEntity, newRotation);
                        }
                        break;
                }
            }


            spawnerSpawnInfoList.Clear();
        }
    }
}
