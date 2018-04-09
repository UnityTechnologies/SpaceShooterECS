using System;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Rendering;

namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct BoltTypeData : ISharedComponentData
    {
        public enum BoltType
        {
            AIBolt = 0,
            PlayerBolt,

            BoltTypeCount,
        }
        public BoltType boltType;
    }

    public class BoltTypeDataComponent : SharedComponentDataWrapper<BoltTypeData>
    {


    }
}
