using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

public class ParticleSystemManager : MonoBehaviour
{
    public ParticleSystemObject particleSystemPrefab;
    public int priorityInitialAmount = 50;
    public int nonPriorityInitialAmount = 200;

    List<ParticleSystemObject> priorityParticleList;
    NativeArray<float> priorityParticleTimeArray;
    NativeQueue<int> priorityParticleAvailableQueue;

    List<ParticleSystemObject> nonPriorityParticleList;
    NativeArray<float> nonPriorityParticleTimeArray;
    NativeQueue<int> nonPriorityParticleAvailableQueue;

    JobHandle nonPriorityUpdateJobHandle;
    NativeQueue<int> nonPriorityDeadParticleQueue;

    JobHandle priorityUpdateJobHandle;
    NativeQueue<int> priorityDeadParticleQueue;


    // Use this for initialization
    void OnEnable ()
    {
        priorityParticleList = new List<ParticleSystemObject>(priorityInitialAmount);
        priorityParticleTimeArray = new NativeArray<float>(priorityInitialAmount, Allocator.Persistent);
        priorityParticleAvailableQueue = new NativeQueue<int>(Allocator.Persistent);

        nonPriorityParticleList = new List<ParticleSystemObject>(nonPriorityInitialAmount);
        nonPriorityParticleTimeArray = new NativeArray<float>(nonPriorityInitialAmount, Allocator.Persistent);
        nonPriorityParticleAvailableQueue = new NativeQueue<int>(Allocator.Persistent);

        GameObject priorityParentObject = new GameObject("PriorityVFX");
        priorityParentObject.transform.parent = transform;

        GameObject nonPriorityParentObject = new GameObject("NonPriorityVFX");
        nonPriorityParentObject.transform.parent = transform;


        for (int i = 0; i < priorityParticleList.Capacity; i++)
        {
            priorityParticleList.Add(Instantiate(particleSystemPrefab, priorityParentObject.transform));
            priorityParticleTimeArray[i] = -1.0f;
            priorityParticleAvailableQueue.Enqueue(i);
        }

        for (int i = 0; i < nonPriorityParticleList.Capacity; i++)
        {
            nonPriorityParticleList.Add(Instantiate(particleSystemPrefab, nonPriorityParentObject.transform));
            nonPriorityParticleTimeArray[i] = -1.0f;
            nonPriorityParticleAvailableQueue.Enqueue(i);
        }

        priorityDeadParticleQueue = new NativeQueue<int>(Allocator.Persistent);
        nonPriorityDeadParticleQueue = new NativeQueue<int>(Allocator.Persistent);
    }

    private void OnDisable()
    {
        nonPriorityUpdateJobHandle.Complete();
        priorityUpdateJobHandle.Complete();


        for (int i = 0; i < priorityParticleList.Count; i++)
        {
            Destroy(priorityParticleList[i].gameObject);
        }
        priorityParticleList.Clear();
        priorityParticleTimeArray.Dispose();
        priorityParticleAvailableQueue.Dispose();

        for (int i = 0; i < nonPriorityParticleList.Count; i++)
        {
            Destroy(nonPriorityParticleList[i].gameObject);
        }
        nonPriorityParticleList.Clear();
        nonPriorityParticleTimeArray.Dispose();
        nonPriorityParticleAvailableQueue.Dispose();

        nonPriorityDeadParticleQueue.Dispose();
        priorityDeadParticleQueue.Dispose();
    }

    public void SpawnParticle(bool priority, Vector3 position, Quaternion rotation)
    {
        List<ParticleSystemObject> tmpParticleListToUse = priority ? priorityParticleList : nonPriorityParticleList;
        NativeArray<float> tmpParticleTimeArray = priority ? priorityParticleTimeArray : nonPriorityParticleTimeArray;
        NativeQueue<int> tmpParticleAvailableQueue = priority ? priorityParticleAvailableQueue : nonPriorityParticleAvailableQueue;

        if(tmpParticleAvailableQueue.Count == 0)
        {
            return;
        }

        int nextAvailableParticle = tmpParticleAvailableQueue.Dequeue();
        if(tmpParticleListToUse[nextAvailableParticle].runningTime > 0.0f)
        {
            tmpParticleListToUse[nextAvailableParticle].StartParticleSystem(position, rotation);
            tmpParticleTimeArray[nextAvailableParticle] = tmpParticleListToUse[nextAvailableParticle].runningTime;
        }
        else
        {
            tmpParticleAvailableQueue.Enqueue(nextAvailableParticle);
        }

    }

    struct UpdateParticleTimeJob : IJobParallelFor
    {
        public float deltaTime;
        public NativeArray<float> particleTimeList;
        public NativeQueue<int>.Concurrent deadParticleQueue;

        public void Execute(int index)
        {
            float particleTime = particleTimeList[index];
            if(particleTime > 0.0f)
            {
                particleTime -= deltaTime;
                particleTimeList[index] = particleTime;
                if(particleTime <= 0.0f)
                {
                    deadParticleQueue.Enqueue(index);
                }
            }
        }
    }


    public void Update()
    {
        UpdateParticleTimeJob nonPriorityUpdateJob = new UpdateParticleTimeJob
        {
            deltaTime = Time.deltaTime,
            particleTimeList = nonPriorityParticleTimeArray,
            deadParticleQueue = nonPriorityDeadParticleQueue.ToConcurrent(),
        };

        nonPriorityUpdateJobHandle = nonPriorityUpdateJob.Schedule(nonPriorityParticleTimeArray.Length, 10);

        UpdateParticleTimeJob priorityUpdateJob = new UpdateParticleTimeJob
        {
            deltaTime = Time.deltaTime,
            particleTimeList = priorityParticleTimeArray,
            deadParticleQueue = priorityDeadParticleQueue.ToConcurrent(),
        };
        priorityUpdateJobHandle = priorityUpdateJob.Schedule(priorityParticleTimeArray.Length, 10);

        //start executing the jobs now
        JobHandle.ScheduleBatchedJobs();


    }

    private void LateUpdate()
    {
        nonPriorityUpdateJobHandle.Complete();

        while (nonPriorityDeadParticleQueue.Count > 0)
        {
            int deadParticleIndex = nonPriorityDeadParticleQueue.Dequeue();

            nonPriorityParticleList[deadParticleIndex].StopParticleSystem();
            nonPriorityParticleTimeArray[deadParticleIndex] = -1.0f;
            nonPriorityParticleAvailableQueue.Enqueue(deadParticleIndex);
        }

        priorityUpdateJobHandle.Complete();

        while (priorityDeadParticleQueue.Count > 0)
        {
            int deadParticleIndex = priorityDeadParticleQueue.Dequeue();

            priorityParticleList[deadParticleIndex].StopParticleSystem();
            nonPriorityParticleTimeArray[deadParticleIndex] = -1.0f;
            priorityParticleAvailableQueue.Enqueue(deadParticleIndex);
        }
    }

}
