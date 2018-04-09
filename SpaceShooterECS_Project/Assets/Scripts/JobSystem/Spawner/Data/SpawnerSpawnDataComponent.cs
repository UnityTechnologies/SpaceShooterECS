using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_SpaceShooterDemo
{

    [System.Serializable]
    public struct SpawnerSpawnData : IComponentData
    {
        public float timeSinceLastSpawn;
        public float spawnDelay;
    }


    public class SpawnerSpawnDataComponent : ComponentDataWrapper<SpawnerSpawnData>
    {


    }
}

