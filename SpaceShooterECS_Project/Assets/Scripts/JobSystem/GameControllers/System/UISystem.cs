using System.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

using System.Collections.Generic;

namespace ECS_SpaceShooterDemo
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(EntityManagementGroup))]
    [UpdateAfter(typeof(DestroyEntitySystem))]
    public class UISystem : GameControllerComponentSystem
    {
        private Entity dataEntity;

        ComponentGroup entityDataGroup = null;
        List<EntityTypeData> uniqueEntityTypes = new List<EntityTypeData>(10);

        float deltaTime = 0;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            entityDataGroup = GetComponentGroup(typeof(EntityTypeData));


            dataEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(dataEntity, new UIData { score = 0.0f });
        }

 

        protected override void OnDestroyManager()
        {
            EntityManager.DestroyEntity(dataEntity);

            base.OnDestroyManager();
        }




        protected override void OnUpdate()
        {
            NativeArray<Entity> allEntities = EntityManager.GetAllEntities();
            UIData uiData = EntityManager.GetComponentData<UIData>(dataEntity);

            MonoBehaviourECSBridge.Instance.entitiesCountText.text = "Entities Count: " + allEntities.Length.ToString();

            MonoBehaviourECSBridge.Instance.jobBatchCountText.text = "Job Batch Count (/10000): "
                                                               + MonoBehaviourECSBridge.Instance.jobBatchCountPerTenThousand.ToString() + " ( [ / ] ) ";

            MonoBehaviourECSBridge.Instance.scoreText.text = "Score: " + uiData.score.ToString();


            uniqueEntityTypes.Clear();
            EntityManager.GetAllUniqueSharedComponentData(uniqueEntityTypes);
            for (int i = 0; i != uniqueEntityTypes.Count; i++)
            {
                switch (uniqueEntityTypes[i].entityType)
                {
                    case EntityTypeData.EntityType.BackgroundSpawner:
                        entityDataGroup.SetFilter(uniqueEntityTypes[i]);
                        MonoBehaviourECSBridge.Instance.backgroundSpawnerCountText.text = "Spawners: "
                                                                                    + entityDataGroup.GetEntityArray().Length.ToString()
                                                                                    + " ( + / - )";
                        break;
                }
            }


            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            MonoBehaviourECSBridge.Instance.fpsText.text = string.Format("FPS: {0:00.} ({1:00.0} ms)", fps, msec);

            if (uiData.gameOver != 0)
            {
                MonoBehaviourECSBridge.Instance.gameOverText.text = "Game Over!";
            }
            else
            {
                MonoBehaviourECSBridge.Instance.gameOverText.text = "";
            }

            if (uiData.restart != 0 )
            {
                MonoBehaviourECSBridge.Instance.restartText.text = "Press 'R' for Restart";
            }
            else
            {
                MonoBehaviourECSBridge.Instance.restartText.text = "";
            }

        }
    }
}
