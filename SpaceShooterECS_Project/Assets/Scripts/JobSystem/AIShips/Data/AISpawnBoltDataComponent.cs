using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS_SpaceShooterDemo
{
    [System.Serializable]
    public struct AISpawnBoltData : IComponentData
    {
        public float3 spawnPosition;
        public float3 spawnDirection;
        public float fireRate;
        public float timeSinceFire;
        public float offset;
    }

    [ExecuteInEditMode]
    public class AISpawnBoltDataComponent : ComponentDataWrapper<AISpawnBoltData>
    {

#if UNITY_EDITOR
        void Update()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                AISpawnBoltData spawnData = Value;
                spawnData.spawnPosition = transform.position;
                Value = spawnData;
            }
        }
#endif

        void OnDrawGizmosSelected()
        {
            AISpawnBoltData spawnData = Value;
            Gizmos.color = Color.blue;
            Vector3 position = new Vector3(spawnData.spawnPosition.x,
                                        spawnData.spawnPosition.y,
                                        spawnData.spawnPosition.z);
            position += transform.forward * spawnData.offset;

            Gizmos.DrawSphere(position, 0.1f);
        }


    }
}

