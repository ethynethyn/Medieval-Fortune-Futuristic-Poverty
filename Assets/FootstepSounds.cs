using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class FootstepSounds : MonoBehaviour
{
    [System.Serializable]
    public class SurfaceAudio
    {
        public string surfaceTag;
        public AudioClip walkClip;
        public AudioClip runClip;
        public AudioClip landClip;
    }

    [Header("Player and Movement")]
    public Transform playerCapsule;
    public List<SurfaceAudio> surfaces = new List<SurfaceAudio>();
    public AudioMixerGroup footstepMixerGroup;

    private AudioSource audioSource;
    private Collider playerCollider;

    private bool wasGroundedLastFrame = true;
    private bool playingLandingSound = false;
    private float landingSoundDuration = 0.1f;

    private enum FootstepState { Idle, Walking, Running, Landing }
    private FootstepState currentState = FootstepState.Idle;

    private float footstepStartTime = -1f;
    private float minPlayDuration = 0.2f;

    private SurfaceAudio currentSurface;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;

        if (footstepMixerGroup != null)
            audioSource.outputAudioMixerGroup = footstepMixerGroup;

        playerCollider = GetComponent<Collider>();
    }

    void Update()
    {
        bool isGrounded = IsGrounded();

        if (!wasGroundedLastFrame && isGrounded)
            PlayLandingSound();

        wasGroundedLastFrame = isGrounded;

        if (playingLandingSound)
            return;

        bool isMovingKeyPressed = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                                  Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!isGrounded || !isMovingKeyPressed)
        {
            if (audioSource.isPlaying && Time.time - footstepStartTime >= minPlayDuration)
            {
                audioSource.Stop();
                currentState = FootstepState.Idle;
                footstepStartTime = -1f;
            }
            return;
        }

        string groundTag = DetectGroundTag();
        currentSurface = surfaces.Find(s => s.surfaceTag == groundTag)
                        ?? surfaces.Find(s => s.surfaceTag == "Default");

        if (currentSurface == null)
            return;

        AudioClip clipToPlay = isRunning ? currentSurface.runClip : currentSurface.walkClip;
        FootstepState newState = isRunning ? FootstepState.Running : FootstepState.Walking;

        if (clipToPlay == null)
            return;

        //  STOP previous clip if switching between walk and run
        if (currentState != newState || audioSource.clip != clipToPlay)
        {
            if (audioSource.isPlaying && Time.time - footstepStartTime >= minPlayDuration)
            {
                audioSource.Stop();
            }

            audioSource.clip = clipToPlay;

            if (newState == FootstepState.Running)
            {
                audioSource.loop = true;
                audioSource.pitch = 1f;
            }
            else if (newState == FootstepState.Walking)
            {
                audioSource.loop = false; // walk simulates loop manually
                audioSource.pitch = Random.Range(0.95f, 1.05f);
            }

            audioSource.Play();
            footstepStartTime = Time.time;
            currentState = newState;
        }

        // WALK: Repeat the clip with new pitch every time it ends
        if (currentState == FootstepState.Walking && !audioSource.isPlaying &&
            Time.time - footstepStartTime >= minPlayDuration)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.Play();
            footstepStartTime = Time.time;
        }
    }

    void PlayLandingSound()
    {
        string groundTag = DetectGroundTag();
        SurfaceAudio surface = surfaces.Find(s => s.surfaceTag == groundTag)
                             ?? surfaces.Find(s => s.surfaceTag == "Default");

        if (surface == null || surface.landClip == null)
            return;

        playingLandingSound = true;
        audioSource.Stop();
        audioSource.clip = surface.landClip;
        audioSource.loop = false;
        audioSource.pitch = 1f;
        audioSource.Play();

        Invoke(nameof(ResetAfterLandingSound), landingSoundDuration);
        currentState = FootstepState.Landing;
        footstepStartTime = -1f;
    }

    void ResetAfterLandingSound()
    {
        playingLandingSound = false;
    }

    bool IsGrounded()
    {
        float checkDistance = 0.1f;
        Vector3 origin = playerCollider.bounds.center;
        float rayLength = playerCollider.bounds.extents.y + checkDistance;
        return Physics.Raycast(origin, Vector3.down, rayLength);
    }

    string DetectGroundTag()
    {
        RaycastHit hit;
        Vector3 origin = playerCapsule.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out hit, playerCollider.bounds.extents.y + 0.2f))
            return hit.collider.tag;

        return "Default";
    }
}
