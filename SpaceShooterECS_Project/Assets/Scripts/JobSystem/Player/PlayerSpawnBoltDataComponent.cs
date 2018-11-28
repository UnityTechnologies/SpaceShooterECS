using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct PlayerSpawnBoltData : IComponentData
    {
        public float3 spawnPosition;
        public float3 spawnDirection;
        public float fireRate;
        public float nextFireTime;
        public float offset;
    }

    [ExecuteInEditMode]
    public class PlayerSpawnBoltDataComponent : ComponentDataWrapper<PlayerSpawnBoltData>
    {

#if UNITY_EDITOR
        void Update()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                PositionComponent positionComponent = GetComponent<PositionComponent>();
                if (positionComponent)
                {
                    PlayerSpawnBoltData spawnData = Value;
                    spawnData.spawnPosition = positionComponent.Value.Value;
                    Value = spawnData;
                }
            }
        }
#endif

        void OnDrawGizmosSelected()
        {          
            PlayerSpawnBoltData spawnData = Value;
            Gizmos.color = Color.blue;
            Vector3 position = new Vector3(spawnData.spawnPosition.x,
                                        spawnData.spawnPosition.y,
                                        spawnData.spawnPosition.z);
            
            RotationComponent rotationComponent = GetComponent<RotationComponent>();
            if (rotationComponent)
            {
                Vector3 forward = math.forward(rotationComponent.Value.Value);
                position += forward * spawnData.offset;
            }
            
            Gizmos.DrawSphere(position, 0.1f);
        }


    }
}

