using System.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.ECS.Rendering;

namespace ECS_SpaceShooterDemo
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(EntityManagementGroup))]
    [UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.PreUpdate))]
    public class DestroyEntitySystem : GameControllerComponentSystem
    {
        [Inject]
        UIEntityDataGroup uiEntityDataGroup;

        //queues that will be filled by other systems to tell this system what entities to delete
        public NativeQueue<Entity> entityOutOfBoundQueue;
        public NativeQueue<Entity> entityCollisionQueue;

        //entity used by other systems to find the previous queues
        private Entity dataEntity;

        //struct containing information needed to run some logic after an entity is destroyed
        struct InfoForLogicAfterDestroy
        {
            public EntityTypeData entityTypeData;
            public EntityInstanceRenderData renderData;
        }

        //Function do some logic after entities have been destroyed (in this case spawn particles)
        void DestroyLogic(NativeList<InfoForLogicAfterDestroy> infoLogicTmpDataArray)
        {
            for(int i = 0; i < infoLogicTmpDataArray.Length; i++)
            {
                InfoForLogicAfterDestroy infoLogic = infoLogicTmpDataArray[i];
                switch(infoLogic.entityTypeData.entityType)
                {
                    case EntityTypeData.EntityType.Asteroid:
                        {
                            if (MonoBehaviourECSBridge.Instance.asteroidExplosion != null)
                            {
                                //Fow now only spawn particles close to the player position, this is a normal game object spawn and is slow
                                if(infoLogic.renderData.position.y > -23)
                                {
                                    GameObject.Instantiate(MonoBehaviourECSBridge.Instance.asteroidExplosion, infoLogic.renderData.position, Quaternion.LookRotation(infoLogic.renderData.forward, infoLogic.renderData.up));
                                }
                            }
                        }
                        break;
                    case EntityTypeData.EntityType.Bolt:
                        {

                        }
                        break;
                    case EntityTypeData.EntityType.EnemyShip:
                        {
                            if (MonoBehaviourECSBridge.Instance.enemyExplosion != null)
                            {
                                //Fow now only spawn particles close to the player position, this is a normal game object spawn and is slow
                                if (infoLogic.renderData.position.y > -23)
                                {
                                    GameObject.Instantiate(MonoBehaviourECSBridge.Instance.enemyExplosion, infoLogic.renderData.position, Quaternion.LookRotation(infoLogic.renderData.forward, infoLogic.renderData.up));
                                }
                            }
                        }
                        break;
                    case EntityTypeData.EntityType.AllyShip:
                        {
                            if (MonoBehaviourECSBridge.Instance.allyExplosion != null)
                            {
                                //Fow now only spawn particles close to the player position, this is a normal game object spawn and is slow
                                if (infoLogic.renderData.position.y > -23)
                                {
                                    GameObject.Instantiate(MonoBehaviourECSBridge.Instance.allyExplosion, infoLogic.renderData.position, Quaternion.LookRotation(infoLogic.renderData.forward, infoLogic.renderData.up));
                                }
                            }
                        }
                        break;
                    case EntityTypeData.EntityType.PlayerShip:
                        {
                            if(MonoBehaviourECSBridge.Instance.playerExplosion != null)
                            {
                                GameObject.Instantiate(MonoBehaviourECSBridge.Instance.playerExplosion, infoLogic.renderData.position, Quaternion.LookRotation(infoLogic.renderData.forward, infoLogic.renderData.up));
                                // Large shake to indicate player has died
                                CameraController.Instance.OverrideWithShake(CameraController.SHAKE_SIZE.Large);
                            }
                        }
                        break;
                }
            }
        }

        //We need to make sure when deleting our queues that we delete the entities they contain
        void DisposeOfEntityQueue(NativeQueue<Entity> queueToDispose)
        {
            if (queueToDispose.IsCreated)
            {
                while (queueToDispose.Count > 0)
                {
                    Entity entityToDestroy = queueToDispose.Dequeue();
                    if (EntityManager.IsCreated && EntityManager.Exists(entityToDestroy))
                    {
                        EntityManager.DestroyEntity(entityToDestroy);
                    }
                }

                queueToDispose.Dispose();
            }
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            //Allocate our queues
            entityOutOfBoundQueue = new NativeQueue<Entity>(Allocator.Persistent);
            entityCollisionQueue = new NativeQueue<Entity>(Allocator.Persistent);

            //Create the entity that will contain our queues
            dataEntity = EntityManager.CreateEntity();

            //Create the compoenent data used to store our queues, other systems will look for that component data type
            DestroyEntityData data = new DestroyEntityData();
            data.entityOutOfBoundQueueConcurrent = entityOutOfBoundQueue.ToConcurrent();
            data.entityCollisionQueueConcurrent = entityCollisionQueue.ToConcurrent();

            //Add that struct to the entity
            EntityManager.AddComponentData(dataEntity, data);
        }

        protected override void OnDestroyManager()
        {
            EntityManager.DestroyEntity(dataEntity);
            DisposeOfEntityQueue(entityOutOfBoundQueue);
            DisposeOfEntityQueue(entityCollisionQueue);

            base.OnDestroyManager();
        }

        //currently ExclusiveEntityTransaction is not supported by Burst
        //[BurstCompileAttribute(Accuracy.Med, Support.Relaxed)]
        struct DestroyEntityJob : IJob
        {
            //We use an ExclusiveEntityTransaction to have access to the entity manager
            //Only one thread at a time can be running while using an ExclusiveEntityTransaction
            public ExclusiveEntityTransaction entityTransaction;

            //Queues with all the entities we need to destroy
            public NativeQueue<Entity> entityQueue;

            public void Execute()
            {
                while (entityQueue.Count > 0)
                {
                    Entity entityToDestroy = entityQueue.Dequeue();
                    if (entityTransaction.Exists(entityToDestroy))
                    {
                        entityTransaction.DestroyEntity(entityToDestroy);
                    }
                }
            }
        }

        //currently ExclusiveEntityTransaction is not supported by Burst
        //[BurstCompileAttribute(Accuracy.Med, Support.Relaxed)]
        struct DestroyEntityWithLogicJob : IJob
        {
            //We use an ExclusiveEntityTransaction to have access to the entity manager
            //Only one thread at a time can be running while using an ExclusiveEntityTransaction
            public ExclusiveEntityTransaction entityTransaction;

            //We use the first element of this array to get the current score and increment it
            public NativeArray<UIData> uiDataArray;

            //Queues with all the entities we need to destroy
            public NativeQueue<Entity> entityQueue;

            //List to store the InfoForLogicAfterDestroy structs needed to run some logic later
            //We add to this list if needed when destroying an entity
            public NativeList<InfoForLogicAfterDestroy> infoForLogic;

            //score to add when destroying an entity
            public float scoreValue;

            public void Execute()
            {
                while (entityQueue.Count > 0)
                {
                    Entity entityToDestroy = entityQueue.Dequeue();

                    if (entityTransaction.Exists(entityToDestroy))
                    {
                        //Get the EntityTypeData component to figure out what type of entity we are deleting
                        EntityTypeData entityToDestroyTypeData = entityTransaction.GetSharedComponentData<EntityTypeData>(entityToDestroy);
                        switch (entityToDestroyTypeData.entityType)
                        {
                            case EntityTypeData.EntityType.Asteroid:
                            case EntityTypeData.EntityType.EnemyShip:
                            case EntityTypeData.EntityType.AllyShip:
                            case EntityTypeData.EntityType.PlayerShip:
                                {
                                    //Those type of entity will require some additional logic after destruction,
                                    //create the info needed and add it to the list
                                    EntityInstanceRenderData entityToDestroyRenderData = entityTransaction.GetComponentData<EntityInstanceRenderData>(entityToDestroy);
                                    InfoForLogicAfterDestroy newInfo = new InfoForLogicAfterDestroy
                                    {
                                        entityTypeData = entityToDestroyTypeData,
                                        renderData = entityToDestroyRenderData,
                                    };
                                    infoForLogic.Add(newInfo);
                                }
                                break;
                            case EntityTypeData.EntityType.Bolt:
                                {
                                    //Player bolts are only destroyed when they collided with enemies or obstacle,
                                    // add to the score in that case 
                                    BoltTypeData boltTypeData = entityTransaction.GetSharedComponentData<BoltTypeData>(entityToDestroy);
                                    if (boltTypeData.boltType == BoltTypeData.BoltType.PlayerBolt)
                                    {
                                        UIData uiData = uiDataArray[0];
                                        uiData.score += scoreValue;
                                        uiDataArray[0] = uiData;
                                    }
                                }
                                break;
                        }
                        //This will remove the entity from the entity manager
                        entityTransaction.DestroyEntity(entityToDestroy);
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            EntityManager.CompleteAllJobs();

            //We need to call this after EntityManager.CompleteAllJobs so that our uiEntityDataGroup is updated
            UpdateInjectedComponentGroups();

            //Copy our current UI data in a tmp array
            UIData testData = uiEntityDataGroup.uiEntityData[0];
            NativeArray<UIData> uiTmpDataArray = new NativeArray<UIData>(1, Allocator.Temp);
            uiTmpDataArray[0] = testData;

            //Create a tmp list to contain the data needed to do some logic after entities destruction
            NativeList<InfoForLogicAfterDestroy> infoLogicTmpDataList = new NativeList<InfoForLogicAfterDestroy>(entityCollisionQueue.Count, Allocator.Temp);

            //Tell the EntityManager that we will start doing entity work only via an ExclusiveEntityTransaction (that can be passed to a job)
            ExclusiveEntityTransaction exclusiveEntityTransaction = EntityManager.BeginExclusiveEntityTransaction();

            //Set up our job to destroy our entities and fill the infoLogicTmpDataList with the data we need to do some logic after the destruction
            DestroyEntityWithLogicJob destroyEntityWithLogicJob = new DestroyEntityWithLogicJob
            {
                entityTransaction = exclusiveEntityTransaction,
                uiDataArray = uiTmpDataArray,
                entityQueue = entityCollisionQueue,
                scoreValue = MonoBehaviourECSBridge.Instance.destroyScoreValue,
                infoForLogic = infoLogicTmpDataList,
            };

            JobHandle destroyHandle = destroyEntityWithLogicJob.Schedule(EntityManager.ExclusiveEntityTransactionDependency);
            EntityManager.ExclusiveEntityTransactionDependency = JobHandle.CombineDependencies(destroyHandle, EntityManager.ExclusiveEntityTransactionDependency);

            //Send the job to the worker thread queue, we need to do this because we need the job to run now
            JobHandle.ScheduleBatchedJobs();

            //Wait for it to be completed
            destroyHandle.Complete();

            //Start a new job to destroy out of bound entities 
            DestroyEntityJob destroyEntityJob = new DestroyEntityJob
            {
                entityTransaction = exclusiveEntityTransaction,
                entityQueue = entityOutOfBoundQueue,
            };

            //Make sure we depend on the previous job (only one job at a time can use the ExclusiveEntityTransaction)
            destroyHandle = destroyEntityJob.Schedule(destroyHandle);
            EntityManager.ExclusiveEntityTransactionDependency = JobHandle.CombineDependencies(destroyHandle, EntityManager.ExclusiveEntityTransactionDependency);

            //Send the job to the worker thread queue, we need to do this because we need the job to run now
            JobHandle.ScheduleBatchedJobs();

            //While the job for the entity out of bound is running, do our logic for the entity destruction on the main thread
            //The list was generated from the first job
            DestroyLogic(infoLogicTmpDataList);

            //wait for the entity out of bound destroy job to finish
            destroyHandle.Complete();

            //Tell the entity manager that we are done with the ExclusiveEntityTransaction
            EntityManager.EndExclusiveEntityTransaction();

            //We need to call this after EndExclusiveEntityTransaction so that our uiEntityDataGroup is updated
            UpdateInjectedComponentGroups();

            //Copy back the UI data with the update score 
            testData = uiTmpDataArray[0];
            uiEntityDataGroup.uiEntityData[0] = testData;

            //dispose of our tmp array/list
            uiTmpDataArray.Dispose();
            infoLogicTmpDataList.Dispose();
        }
    }

}
