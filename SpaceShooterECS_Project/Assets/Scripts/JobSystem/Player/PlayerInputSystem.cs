using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS_SpaceShooterDemo
{
    public class PlayerInputSystem : GameControllerComponentSystem
    {
        struct PlayerInputDataGroup
        {
            public ComponentDataArray<PlayerInputData> playerInputDataArray;

            public SubtractiveComponent<EntityPrefabData> prefabData;
            public readonly int Length; //required variable
        }
        [Inject]
        PlayerInputDataGroup playerInputDataGroup;

        protected override void OnUpdate()
        {
            for(int i = 0; i < playerInputDataGroup.Length; i++)
            {
                PlayerInputData playerInputData = playerInputDataGroup.playerInputDataArray[i];

                float moveHorizontal = 0.0f;
                float moveVertical = 0.0f;
                bool fireButtonPressed = false;
                switch (playerInputData.playerID)
                {
                    case 0:
                    {
                        moveHorizontal = Input.GetAxis("Horizontal");
                        moveVertical = Input.GetAxis("Vertical");
                        fireButtonPressed = Input.GetButton("Fire1");
                    }
                    break;
                    default:
                    {
                        Debug.LogError("Addional players not supported for now");
                    }
                    break;
                }

                playerInputData.inputMovementDirection.x = moveHorizontal;
                playerInputData.inputMovementDirection.z = moveVertical;
                playerInputData.fireButtonPressed = fireButtonPressed ? 1 : 0;

                playerInputDataGroup.playerInputDataArray[i] = playerInputData;
            }
        }
    }
}
