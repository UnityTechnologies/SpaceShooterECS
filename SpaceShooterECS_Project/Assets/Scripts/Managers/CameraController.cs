using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using ECS_SpaceShooterDemo;
using UnityEngine.Playables;
using UnityEngine.Assertions;

namespace ECS_SpaceShooterDemo
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance = null;

        public enum CINEMATIC_STATE
        {
            Intro,
            Gameplay,
            Shaking,
            Outro,
            Done
        }

        public enum SHAKE_SIZE
        {
            Small,
            Medium,
            Large,
            Done
        }

        [Header("Required References")]
        public CinemachineVirtualCamera mainGameplayCam;
        public Transform playerFollowTransform;
        public Transform shakeTargetTransform;

        [Header("Camera Shake Settings")]
        public CinemachineVirtualCamera smallShakeCam;
        public CinemachineVirtualCamera mediumShakeCam;
        public CinemachineVirtualCamera largeShakeCam;
        public PlayableDirector smallShakeSequence;
        public PlayableDirector mediumShakeSequence;
        public PlayableDirector largeShakeSequence;

        [Header("Intro Settings")]
        public PlayableDirector introSequence;
        public CinemachineVirtualCamera introPlayerFocusCam;
        public CinemachineVirtualCamera introEnemyFocusCamOne;
        public CinemachineVirtualCamera introEnemyFocusCamTwo;
        public bool FadeInText = false;

        [Header("Runtime")]
        // Invisible in inspector
        public int NumberOfExplosionsThisFrame = 0;

        public CINEMATIC_STATE CurrentState
        {
            get { return state; }
        }
        private CINEMATIC_STATE state = CINEMATIC_STATE.Intro;

        void Awake()
        {
            if(!Instance)
            {
                Instance = this;
            }
            else
            {
                Assert.IsNull(Instance,
                    "Only one camera controller should exist at a time");
                DestroyImmediate(this);
            }
        }

        public void IncrementExplodeCount(int weightOfExplosion)
        {
            NumberOfExplosionsThisFrame += weightOfExplosion;
        }

        // Use this for initialization
        void Start()
        {
            Assert.IsNotNull(playerFollowTransform,
                "Plug an empty transform from the GO Hierarchy into "
                + gameObject.name + "'s playerFollowTransform field");

            mainGameplayCam.Follow = playerFollowTransform;
            introPlayerFocusCam.Follow = playerFollowTransform;
            smallShakeCam.Follow = shakeTargetTransform;
            mediumShakeCam.Follow = shakeTargetTransform;
            largeShakeCam.Follow = shakeTargetTransform;

            StartCoroutine(Co_IntroSequence());
            //StartCoroutine(Co_Update());
        }

        IEnumerator Co_IntroSequence()
        {
            while (!mainGameplayCam.Follow)
                yield return null;
            introSequence.Play();
            double currWaitTime = 0.0f;
            while(introSequence.duration >= currWaitTime)
            {
                currWaitTime += Time.deltaTime;
                yield return null;
            }

            // reset explosion count from everything that died during intro
            NumberOfExplosionsThisFrame = 0;

            // done with intro sequence
            state++;
        }

        private void Update()
        {
            playerFollowTransform.position = MonoBehaviourECSBridge.Instance.playerPosition;
        }
        // Update is called once per frame
        IEnumerator Co_Update()
        {
            // possibly remove
            while(state != CINEMATIC_STATE.Done && state != CINEMATIC_STATE.Shaking)
            {
                Debug.Log("State = " + state);
                switch (state)
                {
                    case CINEMATIC_STATE.Intro:
                        yield return Co_IntroSequence();
                        break;
                    case CINEMATIC_STATE.Gameplay:
                        yield return Co_ExplodeShake();
                        break;
                    case CINEMATIC_STATE.Outro:
                        break;
                    default:
                        break;
                }
                yield return null;
            }
        }
        IEnumerator Co_ExplodeShake()
        {
            PlayableDirector cliptoPlay = null;

            if (NumberOfExplosionsThisFrame > 5)
            {
                // play big shake
                cliptoPlay = largeShakeSequence;
            }
            if (NumberOfExplosionsThisFrame > 3)
            {
                // med shake
                cliptoPlay = mediumShakeSequence;
            }
            else if(NumberOfExplosionsThisFrame > 1)
            {
                // small shake
                cliptoPlay = smallShakeSequence;
            }
            Debug.Log("Num Explosions this frame: " + NumberOfExplosionsThisFrame);

            yield return Co_Shake(cliptoPlay);
        }

        public void OverrideWithShake(SHAKE_SIZE shakeSize)
        {
            // By default, don't change focus from player ship
            OverrideWithShake(shakeSize, playerFollowTransform.position);
        }

        public void OverrideWithShake(SHAKE_SIZE shakeSize, Vector3 FocusTarget)
        {
            PlayableDirector cliptoPlay = null;
            shakeTargetTransform.position = FocusTarget;
            switch (shakeSize)
            {
                case SHAKE_SIZE.Small:
                    cliptoPlay = smallShakeSequence;
                    break;
                case SHAKE_SIZE.Medium:
                    cliptoPlay = mediumShakeSequence;
                    break;
                case SHAKE_SIZE.Large:
                    cliptoPlay = largeShakeSequence;
                    break;
            }

            if (state != CINEMATIC_STATE.Shaking && state != CINEMATIC_STATE.Intro)
            {
                StartCoroutine(Co_Shake(cliptoPlay));
            }
        }

        IEnumerator Co_Shake(PlayableDirector cliptoPlay)
        {
            if(state != CINEMATIC_STATE.Shaking && state != CINEMATIC_STATE.Intro)
            {
                if (cliptoPlay != null)
                {
                    CINEMATIC_STATE oldState = state;
                    state = CINEMATIC_STATE.Shaking;
                    cliptoPlay.Play();
                    double currWaitTime = 0.0f;
                    while (cliptoPlay.duration >= currWaitTime)
                    {
                        currWaitTime += Time.deltaTime;
                        yield return null;
                    }
                    cliptoPlay.time = 0.0f;
                    NumberOfExplosionsThisFrame = 0;
                    state = oldState;
                }
            }
        }
    }
}
