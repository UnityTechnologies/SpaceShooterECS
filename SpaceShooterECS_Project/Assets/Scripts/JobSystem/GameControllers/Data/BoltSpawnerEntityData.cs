using Unity.Burst;
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
        public NativeQueue<Entity>.Concurrent enemyBoltSpawnQueueConcurrent;
        public NativeQueue<Entity>.Concurrent allyBoltSpawnQueueConcurrent;
        public NativeQueue<Entity>.Concurrent playerBoltSpawnQueueConcurrent;
    }
}
