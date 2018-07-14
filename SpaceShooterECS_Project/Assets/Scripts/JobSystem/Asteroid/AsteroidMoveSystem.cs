using Unity.Burst;
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
        struct AsteroidModeDataGroup
        {
            public ComponentDataArray<AsteroidMoveData> asteroidMoveDataArray;
            public ComponentDataArray<EntityInstanceRenderData> entityInstanceRenderDataArray;
            public ComponentDataArray<EntityBoundCenterData> entityBoundCenterDataArray;
            public ComponentDataArray<EntityBoundMinMaxData> entityBoundMinMaxDataArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundOffsetData> entityBoundOffsetDataArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundExtendData> entityBoundExtendDataArray;

            public SubtractiveComponent<EntityPrefabData> prefabData;
            public readonly int Length; //required variable
        }
        [Inject]
        AsteroidModeDataGroup asteroidMoveDataGroup;

        [BurstCompileAttribute(Accuracy.Med, Support.Relaxed)]
        struct AsteroidModeJob : IJobParallelFor
        {
            public ComponentDataArray<AsteroidMoveData> asteroidMoveDataArray;
            public ComponentDataArray<EntityInstanceRenderData> entityInstanceRenderDataArray;
            public ComponentDataArray<EntityBoundCenterData> entityBoundCenterDataArray;
            public ComponentDataArray<EntityBoundMinMaxData> entityBoundMinMaxDataArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundOffsetData> entityBoundOffsetDataArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundExtendData> entityBoundExtendDataArray;
            public float deltaTime;

            public void Execute(int index)
            {
                AsteroidMoveData asteroidMoveData = asteroidMoveDataArray[index];
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


                asteroidMoveDataArray[index] = asteroidMoveData;

                EntityInstanceRenderData entityInstanceRenderData = entityInstanceRenderDataArray[index];

                entityInstanceRenderData.position = asteroidMoveData.position;
                entityInstanceRenderData.forward = asteroidMoveData.renderForward;
                entityInstanceRenderData.up = new float3(0, 1, 0);

                entityInstanceRenderDataArray[index] = entityInstanceRenderData;

                EntityBoundCenterData entityBoundCenterData = entityBoundCenterDataArray[index];
                EntityBoundMinMaxData entityBoundMinMaxData = entityBoundMinMaxDataArray[index];

                entityBoundCenterData.centerPosition = asteroidMoveData.position + entityBoundOffsetDataArray[index].offset;
                entityBoundMinMaxData.min = entityBoundCenterData.centerPosition - entityBoundExtendDataArray[index].extend;
                entityBoundMinMaxData.max = entityBoundCenterData.centerPosition + entityBoundExtendDataArray[index].extend;


                entityBoundCenterDataArray[index] = entityBoundCenterData;
                entityBoundMinMaxDataArray[index] = entityBoundMinMaxData;

            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var moveJob = new AsteroidModeJob
            {
                asteroidMoveDataArray = asteroidMoveDataGroup.asteroidMoveDataArray,
                entityInstanceRenderDataArray = asteroidMoveDataGroup.entityInstanceRenderDataArray,
                entityBoundCenterDataArray = asteroidMoveDataGroup.entityBoundCenterDataArray,
                entityBoundMinMaxDataArray = asteroidMoveDataGroup.entityBoundMinMaxDataArray,
                entityBoundOffsetDataArray = asteroidMoveDataGroup.entityBoundOffsetDataArray,
                entityBoundExtendDataArray = asteroidMoveDataGroup.entityBoundExtendDataArray,
                deltaTime = Time.deltaTime
            };


            return moveJob.Schedule(asteroidMoveDataGroup.Length,
                                    MonoBehaviourECSBridge.Instance.GetJobBatchCount(asteroidMoveDataGroup.Length),
                                    inputDeps);
        }
    }
}
