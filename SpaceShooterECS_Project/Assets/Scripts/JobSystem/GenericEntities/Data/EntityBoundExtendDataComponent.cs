using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct EntityBoundExtendData : IComponentData
    {
        public float3 extend;
    }

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EntityBoundCenterDataComponent))]
    [RequireComponent(typeof(EntityBoundOffsetDataComponent))]
    [RequireComponent(typeof(EntityBoundMinMaxDataComponent))]
    public class EntityBoundExtendDataComponent : ComponentDataWrapper<EntityBoundExtendData>
    {

    }
}
