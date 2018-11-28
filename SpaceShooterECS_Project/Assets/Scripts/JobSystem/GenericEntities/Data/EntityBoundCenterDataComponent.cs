using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;


namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct EntityBoundCenterData : IComponentData
    {
        public float3 centerPosition; //world position
    }

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EntityBoundExtendDataComponent))]
    [RequireComponent(typeof(EntityBoundOffsetDataComponent))]
    [RequireComponent(typeof(EntityBoundMinMaxDataComponent))]
    public class EntityBoundCenterDataComponent : ComponentDataWrapper<EntityBoundCenterData>
    {
#if UNITY_EDITOR
        void Update()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                PositionComponent positionComponent = GetComponent<PositionComponent>();
                if (positionComponent)
                {
                    float3 offset = new float3(0.0f, 0.0f, 0.0f);
                    EntityBoundOffsetDataComponent offsetComponent = GetComponent<EntityBoundOffsetDataComponent>();
                    if (offsetComponent)
                    {
                        EntityBoundOffsetData offsetData = offsetComponent.Value;
                        offset = offsetData.offset;
                    }

                    EntityBoundCenterData centerData = Value;
                    centerData.centerPosition = positionComponent.Value.Value;
                    centerData.centerPosition += offset;
                    Value = centerData;
                }               
                

            }
        }
#endif


        void OnDrawGizmosSelected()
        {
            EntityBoundExtendDataComponent extendComponent = GetComponent<EntityBoundExtendDataComponent>();
            if (extendComponent)
            {
                EntityBoundCenterData centerData = Value;
                EntityBoundExtendData extendData = extendComponent.Value;
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(centerData.centerPosition, extendData.extend * 2.0f);
            }
        }
    }
 }
