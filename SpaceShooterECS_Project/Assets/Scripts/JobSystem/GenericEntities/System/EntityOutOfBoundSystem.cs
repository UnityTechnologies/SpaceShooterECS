using System;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS_SpaceShooterDemo
{
    [UpdateAfter(typeof(GameMoveSystem))]
    public class EntityOutOfBoundSystem : GameControllerJobComponentSystem
    {
        ComponentGroup destroyEntityDataGroup;
        
        //This job will add to a queue any entity outside of the view frustum (+ a safe zone)
        //The calculation assume a camera pointing down (no angle)
        [RequireSubtractiveComponent(typeof(PlayerMoveData))] //don't get our players
        [BurstCompile]
        struct EntityOutOfBoundJob : IJobProcessComponentDataWithEntity<EntityBoundCenterData>
        {
            public NativeQueue<Entity>.Concurrent outOfBoundEntityQueue;
            public float3 cameraPosition;
            public float halfFrustumHeightPreCalculation;

            public void Execute(Entity entity, int index, ref EntityBoundCenterData entityBoundCenterData)
            {
                float ydeltaFromCamera = math.abs(entityBoundCenterData.centerPosition.y - cameraPosition.y);
                float halfFrustumHeight = ydeltaFromCamera * halfFrustumHeightPreCalculation;

                //We spawn outside of the view frustrum, this is a safe zone for despawning
                //TODO: Don't hardcode those number (here and in SpawnerSystem)
                halfFrustumHeight *= 1.20f;

                if (entityBoundCenterData.centerPosition.z < cameraPosition.z - halfFrustumHeight
                    || entityBoundCenterData.centerPosition.z > cameraPosition.z + halfFrustumHeight)
                {
                    outOfBoundEntityQueue.Enqueue(entity);
                }
            }
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            destroyEntityDataGroup = GetComponentGroup(typeof(DestroyEntityData));

        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            JobHandle outputHandle = inputDeps;
            
            EntityArray destroyEntityArray = destroyEntityDataGroup.GetEntityArray();
            
            if (destroyEntityArray.Length > 0)
            {
                Entity destroyEntity = destroyEntityArray[0];
                ComponentDataFromEntity<DestroyEntityData> destroyEntityDataFromEntity = GetComponentDataFromEntity<DestroyEntityData>();
                
                DestroyEntityData destroyEntityData = destroyEntityDataFromEntity[destroyEntity];
            
                var outOfBoundJob = new EntityOutOfBoundJob
                {
                    outOfBoundEntityQueue = destroyEntityData.entityOutOfBoundQueueConcurrent,
                    cameraPosition = MonoBehaviourECSBridge.Instance.gameCamera.transform.position,
                    halfFrustumHeightPreCalculation = Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad),
                };

                outputHandle = outOfBoundJob.Schedule(this, inputDeps);
            }

            return outputHandle;
        }

    }
}
