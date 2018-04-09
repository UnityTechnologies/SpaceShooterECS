using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{

    [System.Serializable]
    public struct SpawnerPositionData : IComponentData
    {
        public float3 position;
    }


    public class SpawnerPositionDataComponent : ComponentDataWrapper<SpawnerPositionData>
    {


    }
}

