using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS_SpaceShooterDemo
{
    [UpdateBefore(typeof(EntityToInstanceRendererTransform))]
    public class AIMoveSystem : GameControllerJobComponentSystem
    {
        struct AIMoveDataGroup
        {
            public ComponentDataArray<AIMoveData> aiMoveDataArray;
            public ComponentDataArray<EntityInstanceRenderData> entityInstanceRenderDataArray;
            public ComponentDataArray<EntityBoundCenterData> entityBoundCenterDataArray;
            public ComponentDataArray<EntityBoundMinMaxData> entityBoundMinMaxDataArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundOffsetData> entityBoundOffsetDataArray;
            [ReadOnly]
            public ComponentDataArray<EntityBoundExtendData> entityBoundExtendDataArray;

            public SubtractiveComponent<EntityPrefabData> prefabData;
            public int Length; //required variable
        }
        [Inject]
        AIMoveDataGroup aiMoveDataGroup;

        [ComputeJobOptimizationAttribute(Accuracy.Med, Support.Relaxed)]
        struct AIMoveJob : IJobParallelFor
        {
            public ComponentDataArray<AIMoveData> aiMoveDataArray;
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
                AIMoveData aiMoveData = aiMoveDataArray[index];
                aiMoveData.position += (aiMoveData.speed * aiMoveData.forwardDirection * deltaTime);
                aiMoveDataArray[index] = aiMoveData;

                EntityInstanceRenderData entityInstanceRenderData = entityInstanceRenderDataArray[index];
                entityInstanceRenderData.position = aiMoveData.position;
                entityInstanceRenderData.forward = aiMoveData.forwardDirection;
                entityInstanceRenderData.up = new float3(0, 1, 0);

                entityInstanceRenderDataArray[index] = entityInstanceRenderData;

                EntityBoundCenterData entityBoundCenterData = entityBoundCenterDataArray[index];
                EntityBoundMinMaxData entityBoundMinMaxData = entityBoundMinMaxDataArray[index];

                entityBoundCenterData.centerPosition = aiMoveData.position + entityBoundOffsetDataArray[index].offset;
                entityBoundMinMaxData.min = entityBoundCenterData.centerPosition - entityBoundExtendDataArray[index].extend;
                entityBoundMinMaxData.max = entityBoundCenterData.centerPosition + entityBoundExtendDataArray[index].extend;


                entityBoundCenterDataArray[index] = entityBoundCenterData;
                entityBoundMinMaxDataArray[index] = entityBoundMinMaxData;

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            AIMoveJob moveJob = new AIMoveJob
            {
                aiMoveDataArray = aiMoveDataGroup.aiMoveDataArray,
                entityInstanceRenderDataArray = aiMoveDataGroup.entityInstanceRenderDataArray,
                entityBoundCenterDataArray = aiMoveDataGroup.entityBoundCenterDataArray,
                entityBoundMinMaxDataArray = aiMoveDataGroup.entityBoundMinMaxDataArray,
                entityBoundOffsetDataArray = aiMoveDataGroup.entityBoundOffsetDataArray,
                entityBoundExtendDataArray = aiMoveDataGroup.entityBoundExtendDataArray,
                deltaTime = Time.deltaTime
            };

            return moveJob.Schedule(aiMoveDataGroup.Length,
                                    MonoBehaviourECSBridge.Instance.GetJobBatchCount(aiMoveDataGroup.Length),
                                    inputDeps);
        }
    }
}
