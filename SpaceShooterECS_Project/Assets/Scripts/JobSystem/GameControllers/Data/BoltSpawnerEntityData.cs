using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace ECS_SpaceShooterDemo
{
    public struct BoltSpawnerEntityData : IComponentData
    {
        public NativeQueue<Entity>.Concurrent aiBoltSpawnQueueConcurrent;
        public NativeQueue<Entity>.Concurrent playerBoltSpawnQueueConcurrent;
    }

    struct BoltSpawnerEntityDataGroup
    {
        public ComponentDataArray<BoltSpawnerEntityData> boltSpawnerEntityData;
        public int Length;
    }

}
