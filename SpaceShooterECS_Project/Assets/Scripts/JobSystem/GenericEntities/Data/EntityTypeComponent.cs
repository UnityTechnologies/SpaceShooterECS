using System;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Rendering;

namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct EntityTypeData : ISharedComponentData
    {
        public enum EntityType
        {
            Asteroid = 0,
            Bolt,
            EnemyShip,
            AllyShip,
            PlayerShip,

            GameplaySpawner,
            BackgroundSpawner,

            EntityTypeCount,
        }
        public EntityType entityType;
    }

    public class EntityTypeComponent : SharedComponentDataWrapper<EntityTypeData>
    {


    }
}

