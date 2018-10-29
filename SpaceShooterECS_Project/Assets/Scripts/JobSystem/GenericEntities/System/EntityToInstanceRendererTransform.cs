using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using UnityEngine.ECS.Rendering;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{
    [UpdateBefore(typeof(ECS_SpaceShooterDemo.CollisionSystem))]
    public class EntityToInstanceRendererTransform : GameControllerJobComponentSystem
    {
        //Having a IJobProcessComponentData will automatically add the iComponentData type used as a dependency to the system
        [BurstCompile]
        struct EntityInstanceRenderTransformJob : IJobProcessComponentData<EntityInstanceRenderData, EntityInstanceRendererTransform>
        {
            public void Execute(ref EntityInstanceRenderData entityInstanceRenderData, ref EntityInstanceRendererTransform transform)
            {
                transform.matrix = new float4x4(quaternion.LookRotation(entityInstanceRenderData.forward, entityInstanceRenderData.up), entityInstanceRenderData.position);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entityJob = new EntityInstanceRenderTransformJob();

            return entityJob.Schedule(this, inputDeps);
        }
    }
}
