using System;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace UnityEngine.ECS.Rendering
{
    [System.Serializable]
    public struct EntityInstanceRendererTransform : IComponentData
    {
        public float4x4 matrix;
    }

    public class EntityRendererTransformComponent : ComponentDataWrapper<EntityInstanceRendererTransform> { }
}
