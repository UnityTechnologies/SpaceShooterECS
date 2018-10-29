using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct AIMoveData : IComponentData
    {
        public float3 position;
        public float3 forwardDirection;
        public float speed;
    }


    public class AIMoveDataComponent : ComponentDataWrapper<AIMoveData>
    {


    }
}
