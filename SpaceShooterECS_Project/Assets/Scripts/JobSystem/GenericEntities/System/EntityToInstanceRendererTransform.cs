using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using UnityEngine.ECS.Rendering;
using UnityEngine.ECS.MathUtils;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{
    [UpdateBefore(typeof(ECS_SpaceShooterDemo.CollisionSystem))]
    public class EntityToInstanceRendererTransform : GameControllerJobComponentSystem
    {
        struct EntityInstanceRenderDataGroup
        {
            public ComponentDataArray<EntityInstanceRenderData> entityInstanceRenderData;
            public ComponentDataArray<EntityInstanceRendererTransform> rendererTransforms;
            public int Length;
        }

        [Inject]
        EntityInstanceRenderDataGroup entityInstanceRenderDataGroup;

        [ComputeJobOptimizationAttribute(Accuracy.Med, Support.Relaxed)]
        struct EntityInstanceRenderTransformJob : IJobProcessComponentData<EntityInstanceRenderData, EntityInstanceRendererTransform>
        {
            public void Execute(ref EntityInstanceRenderData entityInstanceRenderData, ref EntityInstanceRendererTransform transform)
            {
                transform.matrix = matrix_math_util.LookRotationToMatrix(entityInstanceRenderData.position, entityInstanceRenderData.forward, entityInstanceRenderData.up);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entityJob = new EntityInstanceRenderTransformJob();

            return entityJob.Schedule(this,
                                      MonoBehaviourECSBridge.Instance.GetJobBatchCount(entityInstanceRenderDataGroup.Length),
                                      inputDeps);
        }
    }
}
