using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{

    [System.Serializable]
    public struct SpawnerHazardData : IComponentData
    {
        public int hazardIndexArrayLength;
        public bool1 isBackgroundSpawner;
    }


    public class SpawnerHazardDataComponent : ComponentDataWrapper<SpawnerHazardData>
    {


    }
}
