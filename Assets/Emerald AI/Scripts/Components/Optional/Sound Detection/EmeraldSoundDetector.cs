using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace EmeraldAI.SoundDetection
{
    /// <summary>
    /// Gives AI that ability to hear noises and detect unseen player targets.
    /// </summary>
    [HelpURL("https://black-horizon-studios.gitbook.io/emerald-ai-wiki/emerald-components-optional/sound-detector-component")]
    public class EmeraldSoundDetector : MonoBehaviour
    {      
        #region Sound Detector Variables
        public GameObject DetectedAttractModifier;
        public ThreatLevels CurrentThreatLevel = ThreatLevels.Unaware;
        public float CurrentThreatAmount;
        public float AttentionRate = 0.1f;
        public float MinVelocityThreshold = 0.5f;
        public float TargetVelocityFactor = 0.3f;
        public float DistanceFactor = 0.1f;
        public float AttentionFalloff = 0.05f;
        public float FalloffDealy = 3f;       
        public float DelayUnawareSeconds = 5f;
        public float AttractModifierCooldown = 5;
        public bool MovingTargetDetected;

        //Unaware
        public float UnawareThreatLevel = 0.05f;
        bool UnawareTriggered;
        [SerializeField]
        public ReactionObject UnawareReaction;
        public UnityEvent UnawareEvent;

        //Suspicious
        public float SuspiciousThreatLevel = 0.5f;
        bool SuspiciousTriggered;
        [SerializeField]
        public ReactionObject SuspiciousReaction;
        public UnityEvent SuspiciousEvent;

        //Aware
        public float AwareThreatLevel = 1f;
        bool AwareTriggered;
        [SerializeField]
        public ReactionObject AwareReaction;
        public UnityEvent AwareEvent;

        //Private variables
        float DelayUnawareTimer = 0;
        EmeraldSystem EmeraldComponent;
        EmeraldDetection EmeraldDetection;
        EmeraldMovement EmeraldMovement;
        bool ArrivedAtDestination;
        Coroutine CurrentReactionCoroutine;
        Coroutine CalculateMovementCoroutine;
        Coroutine RotateTowardsCoroutine;
        float TimeSinceLastAttractModifier;
        bool SoundDetectorEnabled = true;
        float FalloffDelayTimer;

        [SerializeField]
        public List<TargetDataClass> CurrentTargetData = new List<TargetDataClass>();
        [System.Serializable]
        public class TargetDataClass
        {
            public Transform Target;
            public Vector3 LastPosition;
            public float Velocty;
            public float Distance;
            public float NoiseLevel;

            public TargetDataClass (Transform m_Target, Vector3 m_LastPosition, float m_Velocty, float m_Distance, float m_NoiseLevel)
            {
                Target = m_Target;
                LastPosition = m_LastPosition;
                Velocty = m_Velocty;
                Distance = m_Distance;
                NoiseLevel = m_NoiseLevel;
            }
        }

        #endregion

        #region Editor Variables
        public bool HideSettingsFoldout;
        public bool SoundDetectorFoldout;
        public bool UnawareFoldout;
        public bool SuspiciousFoldout;
        public bool AwareFoldout;
        #endregion

        void Start()
        {
            InitializeSoundDetector();
        }

        /// <summary>
        /// Initializes the Sound Detector script.
        /// </summary>
        void InitializeSoundDetector ()
        {
            CurrentThreatAmount = 0;
            TimeSinceLastAttractModifier = AttractModifierCooldown;
            EmeraldComponent = GetComponent<EmeraldSystem>();
            EmeraldMovement = GetComponent<EmeraldMovement>();
            EmeraldDetection = GetComponent<EmeraldDetection>();
        }

        /// <summary>
        /// Allows the Sound Detector to run after already having called DisableSoundDetector as all Sound Detectors are enabled by default.
        /// </summary>
        public void EnableSoundDetector ()
        {
            SoundDetectorEnabled = true;
        }

        /// <summary>
        /// Stops the Sound Detector from running.
        /// </summary>
        public void DisableSoundDetector()
        {
            SoundDetectorEnabled = false;
            CancelAll();
        }

        /// <summary>
        /// Checks for sounds levels for each LineOfSightTargets (targets detected within the AI's detection radius, but have not been seen).
        /// </summary>
        void CheckForSounds ()
        {
            //If the AI enters combat or there are no EmeraldDetection.LineOfSightTargets, return as nothing further needs to be done.
            if (EmeraldComponent.CombatComponent.CombatState || EmeraldDetection.LineOfSightTargets.Count == 0)
            {
                MovingTargetDetected = false;
                return;
            }

            //Add each target from LineOfSightTargets to CurrentTargetData, given that it hasn't already been added and it has the EmeraldComponent.PlayerTag.
            for (int i = 0; i < EmeraldDetection.LineOfSightTargets.Count; i++)
            {
                if (!CurrentTargetData.Exists(x => x.Target == EmeraldDetection.LineOfSightTargets[i].transform))
                {
                    if (!EmeraldDetection.LineOfSightTargets[i].gameObject.CompareTag(EmeraldDetection.PlayerTag)) continue; //Skip non-player targets
                    float DistanceFromTarget = Vector3.Distance(transform.position, EmeraldDetection.LineOfSightTargets[i].transform.position);
                    CurrentTargetData.Add(new TargetDataClass(EmeraldDetection.LineOfSightTargets[i].transform, EmeraldDetection.LineOfSightTargets[i].transform.position, MinVelocityThreshold, DistanceFromTarget, 0));
                }
            }

            UpdateTargetData();
        }

        private void Update()
        {
            if (EmeraldComponent.TargetToFollow) return;

            if (!EmeraldComponent.AnimationComponent.IsDead && SoundDetectorEnabled)
            {
                TimeSinceLastAttractModifier += Time.deltaTime;

                if (EmeraldDetection.LineOfSightTargets.Count > 0)
                {
                    CheckForSounds();
                    CheckEvents();
                }

                if (CurrentThreatAmount > 0 && EmeraldDetection.LineOfSightTargets.Count == 0)
                {
                    if (CurrentTargetData.Count > 0)
                    {
                        SuspiciousTriggered = false;
                        AwareTriggered = false;
                        CurrentTargetData.Clear();
                    }

                    FalloffDelayTimer += Time.deltaTime; //Allow a brief resting period between when a moving target is detected and when it's not.
                    if (FalloffDelayTimer > FalloffDealy) CurrentThreatAmount = Mathf.MoveTowards(CurrentThreatAmount, 0, AttentionFalloff * Time.deltaTime);
                    CurrentThreatAmount = Mathf.Clamp(CurrentThreatAmount, 0f, 1f);
                    if (CurrentThreatAmount < 0.001f)
                    {
                        InvokeReactionList(UnawareReaction);
                        UnawareEvent.Invoke();
                        DelayUnawareTimer = 0;
                        ClearThreats();
                    }
                }
            }
        }

        /// <summary>
        /// Updates all info for each target and stores it in CurrentTargetData.
        /// </summary>
        void UpdateTargetData ()
        {
            for (int i = 0; i < CurrentTargetData.Count; i++)
            {
                if (CurrentTargetData[i].Target != null)
                {
                    //Calculate the velocity of each target by storing its previous distance and comparing it to its current distance (with each CheckIncrement).
                    float DistanceFromTarget = Vector3.Distance(transform.position, CurrentTargetData[i].Target.position);
                    float TargetVelocity = (CurrentTargetData[i].Target.position - CurrentTargetData[i].LastPosition).magnitude / Time.deltaTime;
                    float MitigationValue = (1f - Mathf.Clamp01(DistanceFromTarget / EmeraldComponent.DetectionComponent.DetectionRadius));

                    CurrentTargetData[i].LastPosition = CurrentTargetData[i].Target.position;
                    CurrentTargetData[i].NoiseLevel = TargetVelocity;
                    CalculateThreatLevel(TargetVelocity, MitigationValue);

                    if (TargetVelocity > MinVelocityThreshold)
                    {
                        MovingTargetDetected = true;
                    }
                    else if (CurrentThreatAmount > 0 && TargetVelocity <= MinVelocityThreshold)
                    {
                        MovingTargetDetected = false;
                    }
                }
            }
        }

        /// <summary>
        /// Simply increases or descreases the CurrentThreatLevel depending on whether or not detected targets are moving. 
        /// </summary>
        void CalculateThreatLevel (float CurrentTargetVelocity, float MitigationValue)
        {
            if (MovingTargetDetected)
            {
                FalloffDelayTimer = 0;
                float attentionEffect = AttentionRate * Time.deltaTime;
                attentionEffect += (TargetVelocityFactor * CurrentTargetVelocity * 0.01f) * Time.deltaTime;
                attentionEffect += (DistanceFactor * MitigationValue) * Time.deltaTime;
                CurrentThreatAmount = Mathf.MoveTowards(CurrentThreatAmount, 1, attentionEffect);
            }
            else
            {
                FalloffDelayTimer += Time.deltaTime; //Allow a brief resting period between when a moving target is detected and when it's not.
                if (FalloffDelayTimer > FalloffDealy) CurrentThreatAmount = Mathf.MoveTowards(CurrentThreatAmount, 0, AttentionFalloff * Time.deltaTime);
            }

            CurrentThreatAmount = Mathf.Clamp(CurrentThreatAmount, 0f, 1f);
        }

        /// <summary>
        /// Cancels all sound detection fuctionality and reactions.
        /// </summary>
        void CancelAll ()
        {
            if (CurrentReactionCoroutine != null) StopCoroutine(CurrentReactionCoroutine);
            if (CalculateMovementCoroutine != null) StopCoroutine(CalculateMovementCoroutine);
            if (RotateTowardsCoroutine != null) StopCoroutine(RotateTowardsCoroutine);
            EmeraldComponent.MovementComponent.RotateTowardsTarget = false;
            EmeraldComponent.m_NavMeshAgent.isStopped = false;
            CurrentTargetData.Clear();
            EmeraldComponent.MovementComponent.ChangeWanderType((EmeraldMovement.WanderTypes)EmeraldMovement.StartingWanderingType);
        }

        void CheckEvents()
        {
            //Cancels all sound detection fuctionality and reactions if the AI goes into its combat state.
            if (EmeraldComponent.CombatComponent.CombatState && CurrentTargetData.Count > 0)
            {
                CancelAll();
            }
            //Return if the AI is incombat and the CurrentTargetData has already been cleared.
            else if (EmeraldComponent.CombatComponent.CombatState && CurrentTargetData.Count == 0)
            {
                return;
            }

            //If any threat has been triggered, and the CurrentThreatLevel reaches UnawareLevel for the GiveUpSeconds cooldown amount, there's no detectable threats.
            if (SuspiciousTriggered || AwareTriggered)
            {
                if (CurrentThreatAmount <= UnawareThreatLevel)
                {
                    if (!EmeraldComponent.CombatComponent.CombatState || EmeraldComponent.CombatComponent.CombatState && EmeraldDetection.TargetObstructed)
                    {
                        DelayUnawareTimer += Time.deltaTime;

                        if (DelayUnawareTimer >= DelayUnawareSeconds)
                        {
                            InvokeReactionList(UnawareReaction);
                            UnawareEvent.Invoke();
                            ClearThreats();
                            DelayUnawareTimer = 0;
                        }
                    }
                }
            }

            if (CurrentThreatAmount > UnawareThreatLevel)
            {
                DelayUnawareTimer = 0; //Reset the DelayUnawareTimer if a threat is detected
            }

            if (CurrentThreatAmount >= SuspiciousThreatLevel && CurrentThreatAmount < AwareThreatLevel && !SuspiciousTriggered)
            {
                //Only invoke reactions and events when not in combat mode
                if (!EmeraldComponent.CombatComponent.CombatState)
                {
                    InvokeReactionList(SuspiciousReaction);
                    SuspiciousEvent.Invoke();
                }

                CurrentThreatLevel = ThreatLevels.Suspicious;
                SuspiciousTriggered = true;
            }
            else if (CurrentThreatAmount >= AwareThreatLevel && !AwareTriggered)
            {
                //Only invoke reactions and events when not in combat mode
                if (!EmeraldComponent.CombatComponent.CombatState)
                {
                    InvokeReactionList(AwareReaction);
                    AwareEvent.Invoke();
                }

                CurrentThreatLevel = ThreatLevels.Aware;
                AwareTriggered = true;
            }
        }

        void ClearThreats ()
        {
            CurrentThreatLevel = ThreatLevels.Unaware;
            CurrentThreatAmount = 0;
            SuspiciousTriggered = false;
            AwareTriggered = false;

            //Only remove AI that don't exist within the current LineOfSightTargets after the AI has reached its Unaware ThreatLevel. 
            //This allows an AI to finish out their current reactions that may rely on recent target data. 
            for (int i = 0; i < CurrentTargetData.Count; i++)
            {
                if (!EmeraldDetection.LineOfSightTargets.Exists(x => x.transform == CurrentTargetData[i].Target))
                {
                    CurrentTargetData.RemoveAt(i);
                }
            }

            if (EmeraldDetection.LineOfSightTargets.Count == 0) CurrentTargetData.Clear();
        }

        public void InvokeReactionList (ReactionObject SentReactionObject, bool SentByAttractModifier = false)
        {
            //Only allow reactions to be invoked if the AI is not in combat as combat logic is handled separately.
            if (EmeraldComponent.CombatComponent.CombatState || TimeSinceLastAttractModifier < AttractModifierCooldown)
                return;

            if (SentReactionObject == null)
            {
                if (SentByAttractModifier)
                    Debug.Log("A sent Reaction Object to the AI " + gameObject.name + " by the " + DetectedAttractModifier.name + " Attract Modifier was null. Please ensure the Reaction Object slot on this Attract Modifier object is not null.");
                return;
            }

            //Ensure the AI is using its Starting WanderType.
            EmeraldComponent.MovementComponent.ChangeWanderType((EmeraldMovement.WanderTypes)EmeraldMovement.StartingWanderingType);

            if (CurrentReactionCoroutine != null) { StopCoroutine(CurrentReactionCoroutine); }
            CurrentReactionCoroutine = StartCoroutine(InvokeReactionListInternal(SentReactionObject, SentByAttractModifier));
        }

        IEnumerator InvokeReactionListInternal (ReactionObject SentReactionObject, bool SentByAttractModifier)
        {
            //Add a slight random delay before initializing the reactions list so no two AI reactions play exactly at the same time.
            float RandomDelay = Random.Range(0f, 0.15f);
            yield return new WaitForSeconds(RandomDelay);

            for (int i = 0; i < SentReactionObject.ReactionList.Count; i++)
            {
                //Update the target's sound detection data before checking each reaction, in case something has changed.
                yield return new WaitForSeconds(0.001f);
                EmeraldComponent.DetectionComponent.UpdateAIDetection();
                yield return new WaitForSeconds(0.001f);
                CheckForSounds();
                yield return new WaitForSeconds(0.001f);

                //Go through the list, in order, and play each reaction according to its enum Reaction Type (not the most elegant)
                if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.Delay)
                {
                    yield return new WaitForSeconds(SentReactionObject.ReactionList[i].FloatValue);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.DebugLogMessage)
                {
                    DebugLogMessage(SentReactionObject.ReactionList[i].StringValue);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.PlaySound)
                {
                    EmeraldComponent.SoundComponent.m_AudioSource.volume = SentReactionObject.ReactionList[i].FloatValue;
                    EmeraldComponent.SoundComponent.m_AudioSource.PlayOneShot(SentReactionObject.ReactionList[i].SoundRef);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.PlayEmoteAnimation)
                {
                    EmeraldComponent.AnimationComponent.PlayEmoteAnimation(SentReactionObject.ReactionList[i].IntValue1);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.LookAtLoudestTarget)
                {
                    LookAtLoudestTarget();
                    yield return new WaitForSeconds(SentReactionObject.ReactionList[i].IntValue1);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.SetLoudestTargetAsCombatTarget)
                {
                    SetLoudestTargetAsCombatTarget();
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.ReturnToStartingPosition)
                {
                    ReturnToDefaultPosition();
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.ExpandDetectionDistance)
                {
                    ExpandDetectionDistance(SentReactionObject.ReactionList[i].IntValue1);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.SetMovementState)
                {
                    SetMovementState(SentReactionObject.ReactionList[i].MovementState);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.ResetDetectionDistance)
                {
                    ResetDetectionDistance();
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.ResetLookAtPosition)
                {
                    ResetLookAtPosition();
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.ResetAllToDefault)
                {
                    ResetAllToDefault();
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.ReturnToStartingPosition)
                {
                    ReturnToDefaultPosition();
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.EnterCombatState)
                {
                    SetCombatState(true);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.ExitCombatState)
                {
                    SetCombatState(false);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.FleeFromLoudestTarget)
                {
                    FleeFromLoudestTarget();
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.AttractModifier)
                {
                    TimeSinceLastAttractModifier = 0;
                    AttractModifierInternal(SentReactionObject.ReactionList[i].IntValue2, SentReactionObject.ReactionList[i].IntValue1, SentReactionObject.ReactionList[i].FloatValue, SentReactionObject.ReactionList[i].ReactionType, SentReactionObject.ReactionList[i].AttractModifierReaction);

                    if (SentReactionObject.ReactionList[i].AttractModifierReaction != AttractModifierReactionTypes.LookAtAttractSource)
                    {
                        yield return new WaitForSeconds(0.1f);
                        if (SentReactionObject.ReactionList[i].BoolValue == true) yield return new WaitUntil(() => ArrivedAtDestination);
                    }
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.MoveToLoudestTarget)
                {
                    CalculateMovement(1, 0, SentReactionObject.ReactionList[i].FloatValue, SentReactionObject.ReactionList[i].ReactionType, SentReactionObject.ReactionList[i].AttractModifierReaction);
                    yield return new WaitForSeconds(0.1f);
                    if (SentReactionObject.ReactionList[i].BoolValue == true) yield return new WaitUntil(() => ArrivedAtDestination);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.MoveAroundCurrentPosition)
                {
                    CalculateMovement(SentReactionObject.ReactionList[i].IntValue2, SentReactionObject.ReactionList[i].IntValue1, SentReactionObject.ReactionList[i].FloatValue, SentReactionObject.ReactionList[i].ReactionType, SentReactionObject.ReactionList[i].AttractModifierReaction);
                    yield return new WaitForSeconds(0.1f);
                    if (SentReactionObject.ReactionList[i].BoolValue == true) yield return new WaitUntil(() => ArrivedAtDestination);
                }
                else if (SentReactionObject.ReactionList[i].ReactionType == ReactionTypes.MoveAroundLoudestTarget)
                {
                    CalculateMovement(SentReactionObject.ReactionList[i].IntValue2, SentReactionObject.ReactionList[i].IntValue1, SentReactionObject.ReactionList[i].FloatValue, SentReactionObject.ReactionList[i].ReactionType, SentReactionObject.ReactionList[i].AttractModifierReaction);
                    yield return new WaitForSeconds(0.1f);
                    if (SentReactionObject.ReactionList[i].BoolValue == true) yield return new WaitUntil(() => ArrivedAtDestination);
                }
            }
        }

        /// <summary>
        /// Debug Logs a message to the Unity Console (useful for testing mechanics and values).
        /// </summary>
        public void DebugLogMessage(string DebugMessage)
        {
            Debug.Log(DebugMessage);
        }

        /// <summary>
        /// Generates a new position to move to within the specified radius based on the passed transform.
        /// </summary>
        public void GenerateWaypoint(int Radius, Transform DestinationTransform)
        {
            if (DestinationTransform == null)
            {
                Debug.Log("Destination Transform is null. This reaction has been canceled.");
                return;
            }

            //Destination within radius
            if (Radius > 0)
            {
                Vector3 NewDestination = DestinationTransform.transform.position + new Vector3(Random.Range(-1, 2), 0, Random.Range(-1, 2)) * Radius;
                RaycastHit HitDown;
                if (Physics.Raycast(new Vector3(NewDestination.x, NewDestination.y + 5, NewDestination.z), -transform.up, out HitDown, 10, EmeraldMovement.DynamicWanderLayerMask, QueryTriggerInteraction.Ignore))
                {
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(NewDestination, out hit, 5f, EmeraldComponent.m_NavMeshAgent.areaMask))
                    {
                        EmeraldComponent.m_NavMeshAgent.SetDestination(NewDestination);
                    }
                }
            }
            //Exact destination
            else
            {
                EmeraldComponent.m_NavMeshAgent.SetDestination(DestinationTransform.transform.position);
            }
        }

        /// <summary>
        /// Clears the AI's current Look At Target.
        /// </summary>
        public void ClearLookAtTarget ()
        {
            if (CurrentTargetData.Exists(x => x.Target == EmeraldComponent.LookAtTarget))
            {
                EmeraldComponent.LookAtTarget = null;
            }
        }

        /// <summary>
        /// Sets the AI's Look At Target to the loudest detected target.
        /// </summary>
        public void LookAtLoudestTarget ()
        {
            if (CurrentTargetData.Count == 0)
                return;

            if (RotateTowardsCoroutine != null) StopCoroutine(RotateTowardsCoroutine);
            RotateTowardsCoroutine = StartCoroutine(RotateTowardsPosition(GetLoudestTarget().position)); //Have the AI rotate towards the loudest position
        }

        /// <summary>
        /// Sets the loudest target as the AI's current target.
        /// </summary>
        public void SetLoudestTargetAsCombatTarget()
        {
            EmeraldAPI.Combat.SetCombatTarget(EmeraldComponent, GetLoudestTarget());
        }

        /// <summary>
        /// Returns the AI to its starting position (which is the value set within the Emerald AI Editor).
        /// </summary>
        public void ReturnToDefaultPosition()
        {
            EmeraldComponent.m_NavMeshAgent.destination = EmeraldMovement.StartingDestination;
        }

        /// <summary>
        /// Expand the AI's Detection Distance, in addition to its current detection distance (useful for detecting a target that may have recently attacked a nearby target).
        /// </summary>
        public void ExpandDetectionDistance(int Distance)
        {
            if ((EmeraldDetection.StartingDetectionRadius + Distance) != EmeraldDetection.DetectionRadius)
                EmeraldDetection.DetectionRadius = EmeraldDetection.DetectionRadius + Distance;
        }

        /// <summary>
        /// Changes the AI's movement type.
        /// </summary>
        public void SetMovementState (EmeraldMovement.MovementStates MovementState)
        {
            EmeraldComponent.MovementComponent.CurrentMovementState = MovementState;
        }

        /// <summary>
        /// Resets the AI's Detection Distance to its default/starting value.
        /// </summary>
        void ResetDetectionDistance ()
        {
            EmeraldDetection.DetectionRadius = EmeraldDetection.StartingDetectionRadius;
        }

        /// <summary>
        /// Resets the AI's Look at Position to its default/starting value after the passed amount of seconds has passed.
        /// </summary>
        void ResetLookAtPosition()
        {
            //If the AI dies before the needed amount of seconds has passed.
            if (EmeraldComponent.AnimationComponent.IsDead)
                return;

            EmeraldComponent.LookAtTarget = null;
        }

        /// <summary>
        /// Resets all modified values back to their default values (Look At Position, Detection Distance, Movement State, and Combat State).
        /// </summary>
        void ResetAllToDefault ()
        {
            EmeraldDetection.DetectionRadius = EmeraldDetection.StartingDetectionRadius;
            EmeraldComponent.LookAtTarget = null;
            EmeraldMovement.CurrentMovementState = EmeraldMovement.StartingMovementState;
            SetCombatState(false);
        }

        /// <summary>
        /// Allows external mechanics from an AttractModifier (collisions, triggers, OnStart, and custom calls) to invoke a Reaction Object.
        /// </summary>
        public void AttractModifierInternal(int TotalWaypoints, int Radius, float WaitTime, ReactionTypes ReactionType, AttractModifierReactionTypes AttractModifierReaction)
        {
            if (DetectedAttractModifier == null)
                return;

            if (AttractModifierReaction == AttractModifierReactionTypes.MoveToAttractSource)
            {
                CalculateMovement(1, 0, WaitTime, ReactionType, AttractModifierReaction);
            }
            else if (AttractModifierReaction == AttractModifierReactionTypes.MoveAroundAttractSource)
            {
                CalculateMovement(TotalWaypoints, Radius, WaitTime, ReactionType, AttractModifierReaction);
            }
            else if (AttractModifierReaction == AttractModifierReactionTypes.LookAtAttractSource)
            {
                EmeraldComponent.GetComponent<EmeraldSystem>().LookAtTarget = DetectedAttractModifier.transform;
            }
        }

        /// <summary>
        /// Sets the combat state. If true, this allows the AI to use its combat state animations. If false, returns the AI to its default state using non-combat animations.
        /// </summary>
        public void SetCombatState (bool State)
        {
            EmeraldComponent.m_NavMeshAgent.ResetPath();
            EmeraldComponent.AIAnimator.SetBool("Idle Active", false);
            EmeraldComponent.AIAnimator.SetBool("Combat State Active", State);
        }

        /// <summary>
        /// Generates the proper waypoint based on the reaction passed.
        /// </summary>
        void GenerateWaypointInternal (int Radius, ReactionTypes ReactionType, AttractModifierReactionTypes AttractModifierReaction)
        {
            if (CurrentTargetData.Count > 0 && ReactionType == ReactionTypes.MoveAroundLoudestTarget || CurrentTargetData.Count > 0 && ReactionType == ReactionTypes.MoveToLoudestTarget)
            {
                GenerateWaypoint(Radius, GetLoudestTarget());
            }
            else if (ReactionType == ReactionTypes.AttractModifier)
            {
                GenerateWaypoint(Radius, DetectedAttractModifier.transform);
            }
            else
            {
                GenerateWaypoint(Radius, transform);
            }
        }

        /// <summary>
        /// Calulates the AI's next series of moves triggered by a reaction.
        /// </summary>
        void CalculateMovement (int TotalWaypoints, int Radius, float WaitTime, ReactionTypes ReactionType, AttractModifierReactionTypes AttractModifierReaction)
        {
            if (CalculateMovementCoroutine != null) StopCoroutine(CalculateMovementCoroutine);
            CalculateMovementCoroutine = StartCoroutine(CalculateMovementInternal(TotalWaypoints, Radius, WaitTime, ReactionType, AttractModifierReaction));
        }

        IEnumerator CalculateMovementInternal(int TotalWaypoints, int Radius, float WaitTime, ReactionTypes ReactionType, AttractModifierReactionTypes AttractModifierReaction)
        {
            EmeraldComponent.MovementComponent.ChangeWanderType(EmeraldMovement.WanderTypes.Stationary); //Changes the AI's Wander Type to Stationary so that its default wandering type doesn't interfere with this waypoint generation process.
            ArrivedAtDestination = false; //Used for confirming when an AI arrives at its destination elsewhere. A variable and a delay is needed to avoid this giving a false positive.
            EmeraldComponent.m_NavMeshAgent.ResetPath(); //Reset the AI's current path/destination
            int CurrentWaypoints = 1; //Count the current of generated waypoints.
            float WaitTimer = 0; //Used to allow the AI to stay at each waypoint according to the user set WaitTime.

            GenerateWaypointInternal(Radius, ReactionType, AttractModifierReaction);
            yield return new WaitForSeconds(0.1f);
            ClearTurningValues();

            while (CurrentWaypoints <= TotalWaypoints)
            {
                //If the AI goes into Combat Mode, exit generating waypoints and set the Wander Type back to its default.
                if (EmeraldComponent.CombatComponent.CombatState)
                {
                    EmeraldComponent.MovementComponent.ChangeWanderType((EmeraldMovement.WanderTypes)EmeraldMovement.StartingWanderingType);
                    yield break;
                }

                //Generate a waypoint for until the TotalWaypoints have been met. When the AI has arrived at each waypoint, wait according to the WaitTime.
                if (EmeraldComponent.m_NavMeshAgent.remainingDistance < EmeraldMovement.StoppingDistance && !EmeraldComponent.m_NavMeshAgent.pathPending)
                {
                    WaitTimer += Time.deltaTime;

                    if (WaitTimer > WaitTime)
                    {
                        GenerateWaypointInternal(Radius, ReactionType, AttractModifierReaction);
                        ClearTurningValues();
                        WaitTimer = 0;

                        if (CurrentWaypoints == TotalWaypoints)
                        {
                            EmeraldComponent.m_NavMeshAgent.ResetPath();
                            break;
                        }
                        else
                        {
                            CurrentWaypoints++;
                        }
                    }
                }

                yield return null;
            }

            yield return new WaitForSeconds(WaitTime);
            ArrivedAtDestination = true;
            EmeraldMovement.WaypointTimer = 0;
            //Change the AI's Wander Type back to its default so it can continue functioning as it was.
            EmeraldComponent.MovementComponent.ChangeWanderType((EmeraldMovement.WanderTypes)EmeraldMovement.StartingWanderingType);
        }

        IEnumerator RotateTowardsPosition(Vector3 TargetPosition)
        {
            if (EmeraldComponent.MovementComponent.RotateTowardsTarget) yield break; //Don't allow the AI to rotate towards the TargetPosition if it's already doing so

            EmeraldComponent.MovementComponent.RotateTowardsTarget = true; //Stops certain functionality while in this state
            EmeraldComponent.MovementComponent.LockTurning = false; //This is used for preventing an AI from getting stuck between two turn animations
            EmeraldComponent.m_NavMeshAgent.isStopped = true; //Stop the AI's NavMesh
            yield return new WaitForSeconds(0.1f); //Add a delay so the changes above aren't missed

            while (!EmeraldComponent.MovementComponent.LockTurning)
            {
                Vector3 Direction = new Vector3(TargetPosition.x, 0, TargetPosition.z) - new Vector3(EmeraldComponent.transform.position.x, 0, EmeraldComponent.transform.position.z);
                EmeraldComponent.MovementComponent.UpdateRotations(Direction);
                EmeraldComponent.AnimationComponent.CalculateTurnAnimations(true);
                yield return null;
            }
            
            EmeraldComponent.MovementComponent.RotateTowardsTarget = false;
            EmeraldComponent.m_NavMeshAgent.isStopped = false;
        }

        /// <summary>
        /// Sets the AI's flee target as the loudest detected target. (Cautious Coward AI Only)
        /// </summary>
        void FleeFromLoudestTarget()
        {
            EmeraldComponent.DetectionComponent.SetDetectedTarget(GetLoudestTarget());
        }

        /// <summary>
        /// Returns the loudest detected target.
        /// </summary>
        Transform GetLoudestTarget ()
        {
            //Return null of there's no current targets.
            if (CurrentTargetData.Count == 0)
                return null;

            float MaxNoiseLevel = CurrentTargetData.Max(x => x.NoiseLevel); //Find the highest level of noise within CurrentTargetData
            Transform LoudestTarget = CurrentTargetData.Find(x => x.NoiseLevel == MaxNoiseLevel).Target; //Using the highest level of noise, find that target and assign its position as the PositionOfInterest
            return LoudestTarget;
        }

        /// <summary>
        /// Clears the AI's internal turning values.
        /// </summary>
        void ClearTurningValues ()
        {
            EmeraldComponent.AnimationComponent.IsTurning = false;
            EmeraldComponent.MovementComponent.LockTurning = false;
        }
    }
}