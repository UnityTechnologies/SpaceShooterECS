using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct EntityInstanceRenderData : IComponentData
    {
        public float3 position;
        public float3 forward;
        public float3 up;
    }

    public class EntityInstanceRenderDataComponent : ComponentDataWrapper<EntityInstanceRenderData>
    {

    }
}
