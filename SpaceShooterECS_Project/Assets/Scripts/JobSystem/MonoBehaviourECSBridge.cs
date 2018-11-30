using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace ECS_SpaceShooterDemo
{
    public class MonoBehaviourECSBridge : MonoBehaviour
    {
        [Header("Job System")]
        //Used for debugging purpose to find the best jobBatchCount
        public int jobBatchcountMin = 10;
        public int jobBatchCountPerTenThousand = 30;

        [Header("Player")]
        public GameObject playerEntityPrefab;
        public Transform[] playerStartPosition;

        [Header("Gameplay")]
        public int destroyScoreValue = 15;

        [Header("Spawner - Gameplay")]
        public GameObject gameplaySpawnerPrefab;
        public GameObject[] hazards;

        [Header("Spawner - Background")]
        public GameObject backgroundSpawnerPrefab;
        public GameObject[] hazardsBackground;
        public int amountOfBackgroundSpawner = 30;

        [Header("UI")]
        public Text scoreText;
        public Text entitiesCountText;
        public Text backgroundSpawnerCountText;
        public Text jobBatchCountText;
        public Text fpsText;
        public Text restartText;
        public Text gameOverText;

        [Header("VFX")]
        public ParticleSystemManager asteroidExplosion;
        public ParticleSystemManager enemyExplosion;
        public ParticleSystemManager allyExplosion;
        public ParticleSystemManager playerExplosion;

        [Header("Bolts Prefab")]
        public GameObject allyBolt;
        public GameObject enemyBolt;
        public GameObject playerBolt;

        [Header("Game Camera")]
        public Camera gameCamera;

        EntityManager entityManager;

        private List<ScriptBehaviourManager> gameSystemlist = new List<ScriptBehaviourManager>(100);

        List<Entity> gameplayHazardIndexToPrefabs = new List<Entity>(10);
        List<Entity> backgroundHazardIndexToPrefabs = new List<Entity>(10);

        private static MonoBehaviourECSBridge instanceValue = null;
        public static MonoBehaviourECSBridge Instance
        {
            get
            {
                return instanceValue;
            }
            set
            {
                if (instanceValue != null && value != null)
                {
                    Debug.LogError("There should only be one instance of MonoBehaviourECSBridge");
                }
                instanceValue = value;
            }
        }


        void CreateGameSystems()
        {
            IEnumerable<Type> allTypes;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    allTypes = assembly.GetTypes();

                }
                catch (ReflectionTypeLoadException e)
                {
                    allTypes = e.Types.Where(t => t != null);
                    Debug.LogWarning($"MonoBehaviourECSBridge failed loading assembly: {(assembly.IsDynamic ? assembly.ToString() : assembly.Location)}");
                }
                
                

                // Create all ComponentSystem
                var gameControllerComponentSystems = allTypes.Where(t => t.IsSubclassOf(typeof(GameControllerComponentSystem)) && !t.IsAbstract && !t.ContainsGenericParameters);
                foreach (var type in gameControllerComponentSystems)
                {
                    try
                    {
                        gameSystemlist.Add(World.Active.GetOrCreateManager(type));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                var gameControllerJobComponentSystems = allTypes.Where(t => t.IsSubclassOf(typeof(GameControllerJobComponentSystem)) && !t.IsAbstract && !t.ContainsGenericParameters);
                foreach (var type in gameControllerJobComponentSystems)
                {
                    try
                    {
                        gameSystemlist.Add(World.Active.GetOrCreateManager(type));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);
            }
        }

        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            entityManager = World.Active.GetOrCreateManager<EntityManager>();

            CreateGameSystems();

            for (int i = 0; i < hazards.Length; i++)
            {
                Entity newPrefabEntity = entityManager.Instantiate(hazards[i]);
                entityManager.AddComponentData<Prefab>(newPrefabEntity, new Prefab());

                gameplayHazardIndexToPrefabs.Add(newPrefabEntity);
            }

            for (int i = 0; i < hazardsBackground.Length; i++)
            {
                Entity newPrefabEntity = entityManager.Instantiate(hazardsBackground[i]);
                entityManager.AddComponentData<Prefab>(newPrefabEntity, new Prefab());

                backgroundHazardIndexToPrefabs.Add(newPrefabEntity);
            }

        }

        void OnDisable()
        {
            if (World.Active != null)
            {
                for (int i = 0; i < gameSystemlist.Count; i++)
                {
                    World.Active.DestroyManager(gameSystemlist[i]);
                }

                ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);
                gameSystemlist.Clear();
            }

            if(entityManager.IsCreated)
            {
                for(int i = 0; i < gameplayHazardIndexToPrefabs.Count; i++)
                {
                    if(entityManager.Exists(gameplayHazardIndexToPrefabs[i]))
                    {
                        entityManager.DestroyEntity(gameplayHazardIndexToPrefabs[i]);
                    }
                }
                gameplayHazardIndexToPrefabs.Clear();

                for (int i = 0; i < backgroundHazardIndexToPrefabs.Count; i++)
                {
                    if (entityManager.Exists(backgroundHazardIndexToPrefabs[i]))
                    {
                        entityManager.DestroyEntity(backgroundHazardIndexToPrefabs[i]);
                    }
                }
                backgroundHazardIndexToPrefabs.Clear();

            }

        }


        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public int GetJobBatchCount(int workSize)
        {
            return Mathf.Max(jobBatchcountMin, (int)(((float)workSize / 10000.0f) * jobBatchCountPerTenThousand));
        }

        public Entity GetPrefabHazardEntity(int hazardIndex, bool backgroundHazards)
        {
            Entity prefabEntity;
            if (backgroundHazards)
            {
                prefabEntity = backgroundHazardIndexToPrefabs[hazardIndex];
            }
            else
            {
                prefabEntity = gameplayHazardIndexToPrefabs[hazardIndex];
            }

            return prefabEntity;
        }

    }
}
