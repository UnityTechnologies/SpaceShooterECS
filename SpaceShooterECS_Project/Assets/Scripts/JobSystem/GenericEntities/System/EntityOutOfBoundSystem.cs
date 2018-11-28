using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{
    [UpdateAfter(typeof(AsteroidMoveSystem))]
    [UpdateAfter(typeof(GameMoveSystem))]
    [UpdateAfter(typeof(AISpawnBoltSystem))]
    [UpdateAfter(typeof(PlayerSpawnBoltSystem))]
    public class EntityOutOfBoundSystem : GameControllerJobComponentSystem
    {
        [Inject]
        DestroyEntityDataGroup destroyEntityDataGroup;

        //This will get all EntityBoundCenterData from entities (excluding player and prefab entities)
        struct EntityBoundDataGroup
        {
            public EntityArray entityArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundCenterData> entityBoundCenterDataArray;

            public SubtractiveComponent<PlayerMoveData> playerMoveData;  //don't get our players
            public readonly int Length; //required variable
        }
        [Inject]
        EntityBoundDataGroup entityBoundDataGroup;

        //This job will add to a queue any entity outside of the view frustum (+ a safe zone)
        //The calculation assume a camera pointing down (no angle)
        [BurstCompile]
        struct EntityOutOfBoundJob : IJobParallelFor
        {
            [ReadOnly]
            public EntityArray entityArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundCenterData> entityBoundCenterDataArray;

            public NativeQueue<Entity>.Concurrent outOfBoundEntityQueue;

            public float3 cameraPosition;

            public float halfFrustumHeightPreCalculation;


            public void Execute(int index)
            {
                EntityBoundCenterData entityBoundCenterData = entityBoundCenterDataArray[index];

                float ydeltaFromCamera = math.abs(entityBoundCenterData.centerPosition.y - cameraPosition.y);
                float halfFrustumHeight = ydeltaFromCamera * halfFrustumHeightPreCalculation;

                //We spawn outside of the view frustrum, this is a safe zone for despawning
                //TODO: Don't hardcode those number (here and in SpawnerSystem)
                halfFrustumHeight *= 1.20f;

                if (entityBoundCenterData.centerPosition.z < cameraPosition.z - halfFrustumHeight
                    || entityBoundCenterData.centerPosition.z > cameraPosition.z + halfFrustumHeight)
                {
                    outOfBoundEntityQueue.Enqueue(entityArray[index]);
                }
            }
        }



        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var outOfBoundJob = new EntityOutOfBoundJob
            {
                entityArray = entityBoundDataGroup.entityArray,
                entityBoundCenterDataArray = entityBoundDataGroup.entityBoundCenterDataArray,
                outOfBoundEntityQueue = destroyEntityDataGroup.destroyEntityData[0].entityOutOfBoundQueueConcurrent,
                cameraPosition = MonoBehaviourECSBridge.Instance.gameCamera.transform.position,
                halfFrustumHeightPreCalculation = Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad),
            };

            return outOfBoundJob.Schedule(entityBoundDataGroup.Length,
                                          MonoBehaviourECSBridge.Instance.GetJobBatchCount(entityBoundDataGroup.Length),
                                          inputDeps);
        }

    }
}
