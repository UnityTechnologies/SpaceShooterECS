using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS_SpaceShooterDemo
{
    [UpdateAfter(typeof(PlayerInputSystem))]
    [UpdateBefore(typeof(EntityToInstanceRendererTransform))]
    public class PlayerMoveSystem : GameControllerJobComponentSystem
    {
        struct PlayerMoveDataGroup
        {
            public ComponentDataArray<PlayerInputData> playerInputDataArray;
            public ComponentDataArray<PlayerMoveData> playerMoveDataArray;
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
        PlayerMoveDataGroup playerMoveDataGroup;

        //Feedback: The clamp will stop working in burst after a few seconds of usage
        //[BurstCompileAttribute(Accuracy.Med, Support.Relaxed)]
        struct PlayerMoveJob : IJobParallelFor
        {
            public ComponentDataArray<PlayerInputData> playerInputDataArray;
            public ComponentDataArray<PlayerMoveData> playerMoveDataArray;
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
                PlayerInputData playerInputData = playerInputDataArray[index];
                PlayerMoveData playerMoveData = playerMoveDataArray[index];

                float3 movementVector = playerMoveData.rightDirection * playerInputData.inputMovementDirection.x
                                         + playerMoveData.forwardDirection * playerInputData.inputMovementDirection.z;

                playerMoveData.position += (playerMoveData.speed * movementVector * deltaTime);

                playerMoveData.position = math.clamp(playerMoveData.position, playerMoveData.minBoundary, playerMoveData.maxBoundary);


                playerMoveDataArray[index] = playerMoveData;

                EntityInstanceRenderData entityInstanceRenderData = entityInstanceRenderDataArray[index];
                entityInstanceRenderData.position = playerMoveData.position;
                entityInstanceRenderData.forward = playerMoveData.forwardDirection;
                entityInstanceRenderData.up = new float3(0, 1, 0) + (playerMoveData.rightDirection * playerInputData.inputMovementDirection.x);

                entityInstanceRenderDataArray[index] = entityInstanceRenderData;


                EntityBoundCenterData entityBoundCenterData = entityBoundCenterDataArray[index];
                EntityBoundMinMaxData entityBoundMinMaxData = entityBoundMinMaxDataArray[index];

                entityBoundCenterData.centerPosition = playerMoveData.position + entityBoundOffsetDataArray[index].offset;
                entityBoundMinMaxData.min = entityBoundCenterData.centerPosition - entityBoundExtendDataArray[index].extend;
                entityBoundMinMaxData.max = entityBoundCenterData.centerPosition + entityBoundExtendDataArray[index].extend;


                entityBoundCenterDataArray[index] = entityBoundCenterData;
                entityBoundMinMaxDataArray[index] = entityBoundMinMaxData;
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            PlayerMoveJob playerMoveJob = new PlayerMoveJob
            {
                playerInputDataArray = playerMoveDataGroup.playerInputDataArray,
                playerMoveDataArray = playerMoveDataGroup.playerMoveDataArray,
                entityInstanceRenderDataArray = playerMoveDataGroup.entityInstanceRenderDataArray,
                entityBoundCenterDataArray = playerMoveDataGroup.entityBoundCenterDataArray,
                entityBoundMinMaxDataArray = playerMoveDataGroup.entityBoundMinMaxDataArray,
                entityBoundOffsetDataArray = playerMoveDataGroup.entityBoundOffsetDataArray,
                entityBoundExtendDataArray = playerMoveDataGroup.entityBoundExtendDataArray,
                deltaTime = Time.deltaTime,
            };


            return playerMoveJob.Schedule(playerMoveDataGroup.Length,
                                          MonoBehaviourECSBridge.Instance.GetJobBatchCount(playerMoveDataGroup.Length),
                                          inputDeps);
        }
    }
}
