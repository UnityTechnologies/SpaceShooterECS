using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct EntityBoundOffsetData : IComponentData
    {
        public float3 offset; //center offset from entity position
    }

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EntityBoundCenterDataComponent))]
    [RequireComponent(typeof(EntityBoundExtendDataComponent))]
    [RequireComponent(typeof(EntityBoundMinMaxDataComponent))]
    public class EntityBoundOffsetDataComponent : ComponentDataWrapper<EntityBoundOffsetData>
    {

    }
}
