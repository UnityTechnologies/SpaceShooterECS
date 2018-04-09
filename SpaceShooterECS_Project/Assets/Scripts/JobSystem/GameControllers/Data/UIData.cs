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
        public bool1 gameOver;
        public bool1 restart;
    }

    struct UIEntityDataGroup
    {
        public ComponentDataArray<UIData> uiEntityData;
        public int Length;
    }

}
