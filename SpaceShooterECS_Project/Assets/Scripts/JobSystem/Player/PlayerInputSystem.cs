using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS_SpaceShooterDemo
{
    [UpdateBefore(typeof(PlayerMoveSystem))]
    public class PlayerInputSystem : GameControllerComponentSystem
    {       
        ComponentGroup playerInputDataGroup;

        
        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            playerInputDataGroup = GetComponentGroup(typeof(PlayerInputData)); 
        }
        
        
        protected override void OnUpdate()
        {
            ArchetypeChunkComponentType<PlayerInputData> playerInputDataRW = GetArchetypeChunkComponentType<PlayerInputData>(false);
            
            NativeArray<ArchetypeChunk> playerInputDataChunk = playerInputDataGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            if (playerInputDataChunk.Length == 0)
            {
                playerInputDataChunk.Dispose();
                return;
            }

            for (int chunkIndex = 0; chunkIndex < playerInputDataChunk.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = playerInputDataChunk[chunkIndex];
                int dataCount = chunk.Count;
                
                NativeArray<PlayerInputData> playerInputDataArray = chunk.GetNativeArray(playerInputDataRW);

                for (int dataIndex = 0; dataIndex < dataCount; dataIndex++)
                {
                    PlayerInputData playerInputData = playerInputDataArray[dataIndex];

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

                    playerInputDataArray[dataIndex] = playerInputData;

                }

            }
            
            playerInputDataChunk.Dispose();
        }
    }
}
