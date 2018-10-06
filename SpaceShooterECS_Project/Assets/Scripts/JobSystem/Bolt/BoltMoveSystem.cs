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
        struct BoltMoveDataGroup
        {
            public ComponentDataArray<BoltMoveData> boltMoveDataArray;
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
        BoltMoveDataGroup boltMoveDataGroup;

        [BurstCompileAttribute(Accuracy.Med, Support.Relaxed)]
        struct BoltMoveJob : IJobParallelFor
        {
            public ComponentDataArray<BoltMoveData> boltMoveDataArray;
            public ComponentDataArray<EntityInstanceRenderData> entityInstanceRenderDataArray;
            public ComponentDataArray<EntityBoundCenterData> entityBoundCenterDataArray;
            public ComponentDataArray<EntityBoundMinMaxData> entityBoundMinMaxDataArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundOffsetData> entityBoundOffsetDataArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundExtendData> entityBoundExtendDataArray;
            public float deltaTime;
            public float3 renderDataForward;

            public void Execute(int index)
            {
                BoltMoveData boltMoveData = boltMoveDataArray[index];
                boltMoveData.position += (boltMoveData.speed * boltMoveData.forwardDirection * deltaTime);
                boltMoveDataArray[index] = boltMoveData;

                EntityInstanceRenderData entityInstanceRenderData = entityInstanceRenderDataArray[index];
                entityInstanceRenderData.position = boltMoveData.position;
                entityInstanceRenderData.forward = renderDataForward;

                entityInstanceRenderData.up = -boltMoveData.forwardDirection;

                entityInstanceRenderDataArray[index] = entityInstanceRenderData;

                EntityBoundCenterData entityBoundCenterData = entityBoundCenterDataArray[index];
                EntityBoundMinMaxData entityBoundMinMaxData = entityBoundMinMaxDataArray[index];

                entityBoundCenterData.centerPosition = boltMoveData.position + entityBoundOffsetDataArray[index].offset;
                entityBoundMinMaxData.min = entityBoundCenterData.centerPosition - entityBoundExtendDataArray[index].extend;
                entityBoundMinMaxData.max = entityBoundCenterData.centerPosition + entityBoundExtendDataArray[index].extend;


                entityBoundCenterDataArray[index] = entityBoundCenterData;
                entityBoundMinMaxDataArray[index] = entityBoundMinMaxData;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            BoltMoveJob moveJob = new BoltMoveJob
            {
                boltMoveDataArray = boltMoveDataGroup.boltMoveDataArray,
                entityInstanceRenderDataArray = boltMoveDataGroup.entityInstanceRenderDataArray,
                entityBoundCenterDataArray = boltMoveDataGroup.entityBoundCenterDataArray,
                entityBoundMinMaxDataArray = boltMoveDataGroup.entityBoundMinMaxDataArray,
                entityBoundOffsetDataArray = boltMoveDataGroup.entityBoundOffsetDataArray,
                entityBoundExtendDataArray = boltMoveDataGroup.entityBoundExtendDataArray,
                deltaTime = Time.deltaTime,
                renderDataForward = new float3(0,-1, 0),
            };

            return moveJob.Schedule(boltMoveDataGroup.Length,
                                    MonoBehaviourECSBridge.Instance.GetJobBatchCount(boltMoveDataGroup.Length),
                                    inputDeps);
        }
    }
}
