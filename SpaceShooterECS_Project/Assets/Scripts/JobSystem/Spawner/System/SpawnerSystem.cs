using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS_SpaceShooterDemo
{
    [UpdateInGroup(typeof(EntityManagementGroup))]
    [UpdateAfter(typeof(DestroyEntitySystem))]
    public class SpawnerSystem : GameControllerComponentSystem
    {
        struct SpawnerDataGroup
        {
            public ComponentDataArray<SpawnerPositionData> spawnerPositionDataArray;
            public ComponentDataArray<SpawnerHazardData> spawnerHazardDataArray;
            public ComponentDataArray<SpawnerSpawnData> spawnerSpawnDataArray;

            public SubtractiveComponent<EntityPrefabData> prefabData;
            public readonly int Length; //required variable
        }
        [Inject]
        SpawnerDataGroup spawnerDataGroup;

        struct SpawnerSpawnInfo
        {
            public float spawnYPosition;
            public int hazardIndexToSpawn;
            public int isBackgroundSpawn;
        }

        private NativeList<SpawnerSpawnInfo> spawnerSpawnInfoList;

        protected override void OnCreateManager(int capacity)
        {
            base.OnCreateManager(capacity);

            spawnerSpawnInfoList = new NativeList<SpawnerSpawnInfo>(1000, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            spawnerSpawnInfoList.Dispose();

            base.OnDestroyManager();
        }



        protected override void OnUpdate()
        {
            //Go over all our spawners and figure out if any new entity need to be spawned
            //Add the spawning info to spawnerSpawnInfoList 
            for (int i = 0; i < spawnerDataGroup.Length; i++)
            {
                SpawnerPositionData spawnerPositionData = spawnerDataGroup.spawnerPositionDataArray[i];
                SpawnerHazardData spawnerHazardData = spawnerDataGroup.spawnerHazardDataArray[i];
                SpawnerSpawnData spawnerSpawnData = spawnerDataGroup.spawnerSpawnDataArray[i];

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
                    int hazardToSpawnIndex = Random.Range(0, spawnerHazardData.hazardIndexArrayLength);

                    SpawnerSpawnInfo spawnInfo = new SpawnerSpawnInfo
                    {
                        spawnYPosition = yPositionSpawn,
                        hazardIndexToSpawn = hazardToSpawnIndex,
                        isBackgroundSpawn = spawnerHazardData.isBackgroundSpawner,
                    };

                    spawnerSpawnInfoList.Add(spawnInfo);
                }
                spawnerDataGroup.spawnerSpawnDataArray[i] = spawnerSpawnData;
            }

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
                float3 spawnPositionHazard = new Vector3(Random.Range(cameraPosition.x - halfFrustumWidth, cameraPosition.x + halfFrustumWidth), yPosition, cameraPosition.z + halfFrustumHeight);
                float3 spawnPositionAlly = new Vector3(Random.Range(cameraPosition.x - halfFrustumWidth, cameraPosition.x + halfFrustumWidth), yPosition, cameraPosition.z - halfFrustumHeight);

                //Spawn the hazard using the index we randomnly generated
                Entity newHazardEntity = EntityManager.Instantiate(MonoBehaviourECSBridge.Instance.GetPrefabHazardEntity(spawnInfo.hazardIndexToSpawn, spawnInfo.isBackgroundSpawn == 1));
                //Make sure to remove the prefab "tag" data component
                EntityManager.RemoveComponent<EntityPrefabData>(newHazardEntity);

                //Based on the type of entity spawned, set the data needed for it
                EntityTypeData entityTypeData = EntityManager.GetSharedComponentData<EntityTypeData>(newHazardEntity);
                switch (entityTypeData.entityType)
                {
                    case EntityTypeData.EntityType.Asteroid:
                        {
                            Vector3 spawnRenderForward = new Vector3(Random.value, Random.value, 1.0f);
                            spawnRenderForward.Normalize();
                            Vector3 spawnRotationAxis = new Vector3(Random.value, 1.0f, Random.value);
                            spawnRotationAxis.Normalize();

                            AsteroidMoveData moveData = EntityManager.GetComponentData<AsteroidMoveData>(newHazardEntity);
                            moveData.position = spawnPositionHazard;
                            moveData.forwardDirection = -forwardDirection;
                            moveData.renderForward = spawnRenderForward;
                            moveData.rotationAxis = spawnRotationAxis;
                            EntityManager.SetComponentData<AsteroidMoveData>(newHazardEntity, moveData);
                        }
                        break;
                    case EntityTypeData.EntityType.EnemyShip:
                        {
                            AIMoveData moveData = EntityManager.GetComponentData<AIMoveData>(newHazardEntity);
                            moveData.position = spawnPositionHazard;
                            moveData.forwardDirection = -forwardDirection;
                            EntityManager.SetComponentData<AIMoveData>(newHazardEntity, moveData);
                        }
                        break;
                    case EntityTypeData.EntityType.AllyShip:
                        {
                            AIMoveData moveData = EntityManager.GetComponentData<AIMoveData>(newHazardEntity);
                            moveData.position = spawnPositionAlly;
                            moveData.forwardDirection = forwardDirection;
                            EntityManager.SetComponentData<AIMoveData>(newHazardEntity, moveData);
                        }
                        break;
                }
            }


            spawnerSpawnInfoList.Clear();
        }
    }
}
