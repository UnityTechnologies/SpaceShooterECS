using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct PlayerInputData : IComponentData
    {
        public float3 inputMovementDirection;
        public int fireButtonPressed;
        public int playerID;
    }


    public class PlayerInputDataComponent : ComponentDataWrapper<PlayerInputData>
    {


    }
}
