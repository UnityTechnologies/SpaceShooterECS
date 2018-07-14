using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct EntityBoundMinMaxData : IComponentData
    {
        public float3 min;
        public float3 max;
    }

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EntityBoundCenterDataComponent))]
    [RequireComponent(typeof(EntityBoundExtendDataComponent))]
    [RequireComponent(typeof(EntityBoundOffsetDataComponent))]
    public class EntityBoundMinMaxDataComponent : ComponentDataWrapper<EntityBoundMinMaxData>
    {

    }
}
