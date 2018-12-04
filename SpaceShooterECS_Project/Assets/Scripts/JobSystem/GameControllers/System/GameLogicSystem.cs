using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;


namespace ECS_SpaceShooterDemo
{
    [UpdateInGroup(typeof(EntityManagementGroup))]
    [UpdateAfter(typeof(DestroyEntitySystem))]
    public class GameLogicSystem : GameControllerComponentSystem
    {
        private ComponentGroup playerGroup;
        ComponentGroup uiDataGroup;

        private Entity gameplaySpawnerEntity;
        private NativeList<Entity> backGroundSpawnerNativeList = new NativeList<Entity>(100, Allocator.Persistent);

        private bool gameOver;
        private float timeSinceGameOver = 0.0f;
        private bool restart;


        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            playerGroup = GetComponentGroup(typeof(PlayerMoveData)); //Use PlayerMoveData as a tag, only player entities have it
            uiDataGroup = GetComponentGroup(typeof(UIData));
            
            //This function will call MonoBehaviourECSBridge.Instance, this will work because this system is created during the OnEnable of the MonoBehaviourECSBridge gameobject component
            RestartGame();
        }

        protected override void OnDestroyManager()
        {
            for (int i = 0; i < backGroundSpawnerNativeList.Length; i++)
            {
                EntityManager.DestroyEntity(backGroundSpawnerNativeList[i]);
            }
            backGroundSpawnerNativeList.Dispose();


            base.OnDestroyManager();
        }

        public Entity CreateBackgroundSpawner(int spawnerIndex)
        {
            Entity backgroundSpawnerEntity = EntityManager.Instantiate(MonoBehaviourECSBridge.Instance.backgroundSpawnerPrefab);

            SpawnerPositionData positionData = EntityManager.GetComponentData<SpawnerPositionData>(backgroundSpawnerEntity);
            positionData.position.y = -20.0f - (spawnerIndex * 3.0f);
            EntityManager.SetComponentData<SpawnerPositionData>(backgroundSpawnerEntity, positionData);

            SpawnerHazardData hazardData = EntityManager.GetComponentData<SpawnerHazardData>(backgroundSpawnerEntity);
            hazardData.hazardIndexArrayLength = MonoBehaviourECSBridge.Instance.hazardsBackground.Length;
            hazardData.isBackgroundSpawner = 1;
            EntityManager.SetComponentData<SpawnerHazardData>(backgroundSpawnerEntity, hazardData);

            return backgroundSpawnerEntity;
        }

        void RestartGame()
        {
            gameOver = false;
            restart = false;
            timeSinceGameOver = 0.0f;

            EntityArray currentPlayerEntities = playerGroup.GetEntityArray();
            
            Entity newPlayer = EntityManager.Instantiate(MonoBehaviourECSBridge.Instance.playerEntityPrefab);

            float3 forwardDirection = new float3(0.0f, 0.0f, 1.0f);
            
            Position newPosition = new Position()
            {
                Value = MonoBehaviourECSBridge.Instance.playerStartPosition[currentPlayerEntities.Length].position,
            };
            EntityManager.SetComponentData<Position>(newPlayer, newPosition);

            Rotation newRotation = new Rotation()
            {
                Value = quaternion.LookRotation(forwardDirection, new float3(0.0f, 1.0f, 0.0f)),
            };
            EntityManager.SetComponentData<Rotation>(newPlayer, newRotation);
            
            PlayerMoveData playerMoveData = EntityManager.GetComponentData<PlayerMoveData>(newPlayer);
            playerMoveData.forwardDirection = forwardDirection;
            playerMoveData.rightDirection = new float3(1.0f, 0.0f, 0.0f);
                   
            EntityManager.SetComponentData<PlayerMoveData>(newPlayer, playerMoveData);

            //Recreate the enemy spawner for the player
            if(!EntityManager.Exists(gameplaySpawnerEntity))
            {
                gameplaySpawnerEntity = EntityManager.Instantiate(MonoBehaviourECSBridge.Instance.gameplaySpawnerPrefab);

                SpawnerPositionData positionData = EntityManager.GetComponentData<SpawnerPositionData>(gameplaySpawnerEntity);
                positionData.position.y = newPosition.Value.y;
                EntityManager.SetComponentData<SpawnerPositionData>(gameplaySpawnerEntity, positionData);

                SpawnerHazardData hazardData = EntityManager.GetComponentData<SpawnerHazardData>(gameplaySpawnerEntity);
                hazardData.hazardIndexArrayLength = MonoBehaviourECSBridge.Instance.hazards.Length;
                hazardData.isBackgroundSpawner = 0;
                EntityManager.SetComponentData<SpawnerHazardData>(gameplaySpawnerEntity, hazardData);
            }
        }

        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                MonoBehaviourECSBridge.Instance.amountOfBackgroundSpawner++;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                MonoBehaviourECSBridge.Instance.amountOfBackgroundSpawner--;
            }
            MonoBehaviourECSBridge.Instance.amountOfBackgroundSpawner = math.max(0, MonoBehaviourECSBridge.Instance.amountOfBackgroundSpawner);

            //Create or destroy background spawners
            //This should only happen on the first frame and any frame we change the background spawner count
            if (backGroundSpawnerNativeList.Length < MonoBehaviourECSBridge.Instance.amountOfBackgroundSpawner)
            {
                while (backGroundSpawnerNativeList.Length != MonoBehaviourECSBridge.Instance.amountOfBackgroundSpawner)
                {
                    backGroundSpawnerNativeList.Add(CreateBackgroundSpawner(backGroundSpawnerNativeList.Length));
                }
            }
            else if (backGroundSpawnerNativeList.Length > MonoBehaviourECSBridge.Instance.amountOfBackgroundSpawner)
            {
                while (backGroundSpawnerNativeList.Length != MonoBehaviourECSBridge.Instance.amountOfBackgroundSpawner)
                {
                    Entity backgroundSpawnerToRemove = backGroundSpawnerNativeList[backGroundSpawnerNativeList.Length - 1];
                    backGroundSpawnerNativeList.RemoveAtSwapBack(backGroundSpawnerNativeList.Length - 1);
                    EntityManager.DestroyEntity(backgroundSpawnerToRemove);
                }
            }

            //If we deleted or created entities we will need to update our injected component group
            UpdateInjectedComponentGroups();

            //gameplay logic
            EntityArray currentPlayerEntities = playerGroup.GetEntityArray();
            if (currentPlayerEntities.Length == 0)
            {
                if (!gameOver)
                {
                    gameOver = true;
                    timeSinceGameOver = 0.0f;
                    if (EntityManager.Exists(gameplaySpawnerEntity))
                    {
                        EntityManager.DestroyEntity(gameplaySpawnerEntity);
                    }
                }
                else if (!restart)
                {
                    timeSinceGameOver += Time.deltaTime;
                    if (timeSinceGameOver > 2.0f)
                    {
                        restart = true;
                    }
                }

                if (restart)
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        RestartGame();
                    }
                }
            }

            //If we deleted or created entities we will need to update our injected component group
            UpdateInjectedComponentGroups();

            EntityArray uiEntityDataArray = uiDataGroup.GetEntityArray();
            
            UIData tmpUIData = GetComponentDataFromEntity<UIData>()[uiEntityDataArray[0]];
            tmpUIData.gameOver = gameOver ? 1 : 0;
            tmpUIData.restart = restart ? 1 : 0;
           
            EntityManager.SetComponentData(uiEntityDataArray[0], tmpUIData);

        }
    }
}
