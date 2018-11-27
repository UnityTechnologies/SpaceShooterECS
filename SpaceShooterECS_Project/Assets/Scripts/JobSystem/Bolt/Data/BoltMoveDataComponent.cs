using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct BoltMoveData : IComponentData
    {
        public float speed;
        public float3 forwardDirection;
    }


    public class BoltMoveDataComponent : ComponentDataWrapper<BoltMoveData>
    {

    }
}
