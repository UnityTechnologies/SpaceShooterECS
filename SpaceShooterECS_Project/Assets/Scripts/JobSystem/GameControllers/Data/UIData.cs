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
    public struct UIData : IComponentData
    {
        public float score;
        public int gameOver;
        public int restart;
    }
}
