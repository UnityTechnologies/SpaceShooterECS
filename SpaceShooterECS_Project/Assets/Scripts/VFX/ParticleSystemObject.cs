using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemObject : MonoBehaviour
{
    public float runningTime;
    ParticleSystem mainParticleSystem = null;
    Transform cachedTransform;

	// Use this for initialization
	void OnEnable ()
    {
        if(mainParticleSystem == null)
        {
            mainParticleSystem = GetComponent<ParticleSystem>();
        }
        cachedTransform = transform;

        StopParticleSystem();
	}

    public void StopParticleSystem()
    {
        mainParticleSystem.Stop(true);
    }

    public void StartParticleSystem(Vector3 position, Quaternion rotation)
    {
        cachedTransform.SetPositionAndRotation(position, rotation);
        mainParticleSystem.Play(true);
    }
}
