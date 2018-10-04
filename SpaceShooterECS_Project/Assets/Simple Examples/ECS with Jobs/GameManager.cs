using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Shooter.ECSwithJobs
{
    public class GameManager : MonoBehaviour
    {
        #region GAME_MANAGER_STUFF

        //Boilerplat game manager stuff that is the same in each example
        public static GameManager GM;

        [Header("Simulation Settings")]
        public float topBound = 16.5f;
        public float bottomBound = -13.5f;
        public float leftBound = -23.5f;
        public float rightBound = 23.5f;

        [Header("Enemy Settings")]
        public GameObject enemyShipPrefab;
        public float enemySpeed = 1f;

        [Header("Spawn Settings")]
        public int enemyShipCount = 1;
        public int enemyShipIncremement = 1;

        FPS fps;
        int count;


        void Awake()
        {
            if (GM == null)
                GM = this;
            else if (GM != this)
                Destroy(gameObject);
        }
        #endregion

        EntityManager manager;
        

        void Start()
        {
            fps = GetComponent<FPS>();

            manager = World.Active.GetOrCreateManager<EntityManager>();
            AddShips(enemyShipCount);
        }

        void Update()
        {
            if (Input.GetKeyDown("space"))
                AddShips(enemyShipIncremement);
        }

        void AddShips(int amount)
        {
            NativeArray<Entity> entities = new NativeArray<Entity>(amount, Allocator.Temp);
            manager.Instantiate(enemyShipPrefab, entities);

            for (int i = 0; i < amount; i++)
            {
                float xVal = UnityEngine.Random.Range(leftBound, rightBound);
                float zVal = UnityEngine.Random.Range(0f, 10f);
                manager.SetComponentData(entities[i], new Position { Value = new float3(xVal, 0f, topBound + zVal) });
                manager.SetComponentData(entities[i], new Rotation { Value = new quaternion(0, 1, 0, 0) });
                manager.SetComponentData(entities[i], new MoveSpeed { Value = enemySpeed });
            }
            entities.Dispose();

            count += amount;
            fps.SetElementCount(count);
        }
    }
}
