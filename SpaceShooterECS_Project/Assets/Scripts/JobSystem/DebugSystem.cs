using System;
using System.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


namespace ECS_SpaceShooterDemo
{
    [UpdateInGroup(typeof(EntityManagementGroup))]
    [UpdateAfter(typeof(DestroyEntitySystem))]
    public class DebugSystem : GameControllerComponentSystem
    {
        ComponentGroup uiEntityDataGroup;        
        ComponentGroup destroyEntityDataGroup;
        
        [Inject]
        BoltSpawnerEntityDataGroup boltSpawnerEntityDataGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            uiEntityDataGroup = GetComponentGroup(typeof(UIData));
            destroyEntityDataGroup = GetComponentGroup(typeof(DestroyEntityData));
        }
        
        
        protected override void OnUpdate()
        {            
            TestSingletonCount();

            UpdateDebugJobBatchCount();

        }


        void UpdateDebugJobBatchCount()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }


            const int batchCountIncrement = 100;
            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                MonoBehaviourECSBridge.Instance.jobBatchCountPerTenThousand += batchCountIncrement;
            }
            else if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                MonoBehaviourECSBridge.Instance.jobBatchCountPerTenThousand -= batchCountIncrement;
            }

            MonoBehaviourECSBridge.Instance.jobBatchCountPerTenThousand = Mathf.Max(MonoBehaviourECSBridge.Instance.jobBatchcountMin, batchCountIncrement * (int)(MonoBehaviourECSBridge.Instance.jobBatchCountPerTenThousand / batchCountIncrement));
        }

        void TestSingletonCount()
        {
            EntityArray uiEntityDataArray = uiEntityDataGroup.GetEntityArray();
            EntityArray destroyEntityArray = destroyEntityDataGroup.GetEntityArray();
            
            if(uiEntityDataArray.Length != 1)
            {
                Debug.LogError("Only one entity with uiEntityData is supported");
            }

            if(destroyEntityArray.Length != 1)
            {
                Debug.LogError("Only one entity with destroyEntityData is supported");
            }

            if(boltSpawnerEntityDataGroup.Length != 1)
            {
                Debug.LogError("Only one entity with boltSpawnerEntityData is supported");
            }

        }

    }
}
