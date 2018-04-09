using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

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
                PlayerSpawnBoltData spawnData = Value;
                spawnData.spawnPosition = transform.position;
                Value = spawnData;
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
            position += transform.forward * spawnData.offset;

            Gizmos.DrawSphere(position, 0.1f);
        }


    }
}

