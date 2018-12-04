using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct PlayerMoveData : IComponentData
    {
        public float3 forwardDirection;
        public float3 rightDirection;

        public float3 minBoundary;
        public float3 maxBoundary;
        public float speed;

    }


    public class PlayerMoveDataComponent : ComponentDataWrapper<PlayerMoveData>
    {


    }
}
