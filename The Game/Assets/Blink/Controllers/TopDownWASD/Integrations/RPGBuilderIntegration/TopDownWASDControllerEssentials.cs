using System;
using System.Collections;
using BLINK.Controller;
using BLINK.RPGBuilder.Characters;
using BLINK.RPGBuilder.Combat;
using BLINK.RPGBuilder.LogicMono;
using BLINK.RPGBuilder.Managers;
using UnityEngine;

namespace BLINK.RPGBuilder.Controller
{
    public class TopDownWASDControllerEssentials : RPGBCharacterControllerEssentials
    {
        public TopDownWASDController controller;

        private static readonly int moveSpeedModifier = Animator.StringToHash("MoveSpeedModifier");
        
        /*
        -- EVENT FUNCTIONS --
        */
        public override void MovementSpeedChange(float newSpeed)
        {
            controller.SetSpeed(newSpeed);
        }

        /*
        -- INIT --
        */
        public override void Awake()
        {
            anim = GetComponent<Animator>();
            controller = GetComponent<TopDownWASDController>();
            charController = GetComponent<CharacterController>();
        }

        public override IEnumerator InitControllers()
        {
            yield return new WaitForFixedUpdate();
            controllerIsReady = true;
        }

        /*
        -- DEATH --
        */
        public override void InitDeath()
        {
            anim.Rebind();
            anim.SetBool("Dead", true);
            controller.ResetMovement();
        }

        public override void CancelDeath()
        {
            controller.ResetMovement();
            anim.Rebind();
            anim.SetBool("Dead", false);
        }


        /*
        -- GROUND LEAP FUNCTIONS --
        Ground leaps are mobility abilities. Configurable inside the editor under Combat > Abilities > Ability Type=Ground Leap
        They allow to quickly dash or leap to a certain ground location.
        */
        public override void InitGroundLeap()
        {
            isLeaping = true;
            controller.ResetMovement();
            lastPosition = transform.position;
        }

        public override void EndGroundLeap()
        {
            isLeaping = false;
        }

        /*
        -- FLYING FUNCTIONS --
        */

        public override void InitFlying()
        {
        }

        public override void EndFlying()
        {
        }

        /*
        -- STAND TIME FUNCTIONS --
        Stand time is an optional mechanic for abilities. It allows to root the caster for a certain duration after using the ability.
        */
        public override void InitStandTime(float max)
        {
            standTimeActive = true;
            controller.ResetMovement();
            currentStandTimeDur = 0;
            maxStandTimeDur = max;
        }

        protected override void HandleStandTime()
        {
            currentStandTimeDur += Time.deltaTime;
            if (currentStandTimeDur >= maxStandTimeDur) ResetStandTime();
        }

        protected override void ResetStandTime()
        {
            standTimeActive = false;
            currentStandTimeDur = 0;
            maxStandTimeDur = 0;
        }

        /* KNOCKBACK FUNCTIONS
         */
        public bool knockbackActive;
        private Vector3 knockBackTarget;
        private float cachedKnockbackDistance;

        public override void InitKnockback(float knockbackDistance, Transform attacker)
        {
            knockbackDistance *= 5;
            cachedKnockbackDistance = knockbackDistance;
            knockBackTarget = (transform.position - attacker.position).normalized * knockbackDistance;
            knockbackActive = true;
        }

        protected override void HandleKnockback()
        {
            if (knockBackTarget.magnitude > (cachedKnockbackDistance * 0.15f))
            {
                charController.Move(knockBackTarget * Time.deltaTime);
                knockBackTarget = Vector3.Lerp(knockBackTarget, Vector3.zero, 5 * Time.deltaTime);
            }
            else
            {
                ResetKnockback();
            }
        }

        protected override void ResetKnockback()
        {
            knockbackActive = false;
            cachedKnockbackDistance = 0;
            knockBackTarget = Vector3.zero;
        }

        /* MOTION FUNCTIONS
         */
        private float curMotionSpeed;
        public override void InitMotion(float motionDistance, Vector3 motionDirection, float motionSpeed, bool immune)
        {
            if (GameState.playerEntity.IsShapeshifted()) return;
            if (knockbackActive) return;
            cachedMotionSpeed = motionSpeed;
            curMotionSpeed = cachedMotionSpeed;
            cachedPositionBeforeMotion = transform.position;
            cachedMotionDistance = motionDistance;
            motionTarget = transform.TransformDirection(motionDirection) * motionDistance;
            GameState.playerEntity.SetMotionImmune(immune);
            motionActive = true;
        }
        protected override void HandleMotion()
        {
            float distance = Vector3.Distance(cachedPositionBeforeMotion, transform.position);
            if (distance < cachedMotionDistance)
            {
                lastPosition = transform.position;
                charController.Move(motionTarget * (Time.deltaTime * curMotionSpeed));
                
                if (IsInMotionWithoutProgress(0.05f))
                {
                    ResetMotion();
                    return;
                }
                
                if (!(distance < cachedMotionDistance * 0.75f)) return;
                curMotionSpeed = Mathf.Lerp(curMotionSpeed, 0, Time.deltaTime * 5f);
                if(curMotionSpeed < (cachedMotionSpeed * 0.2f))
                {
                    curMotionSpeed = cachedMotionSpeed * 0.2f;
                }
            }
            else
            {
                ResetMotion();
            }
        }

        public override bool IsInMotionWithoutProgress(float treshold)
        {
            float speed = (transform.position - lastPosition).magnitude;
            return speed > -treshold && speed < treshold;
        }

        protected override void ResetMotion()
        {
            motionActive = false;
            GameState.playerEntity.SetMotionImmune(false);
            cachedMotionDistance = 0;
            motionTarget = Vector3.zero;
        }

        /*
        -- CAST SLOWED FUNCTIONS --
        Cast slow is an optional mechanic for abilities. It allows the player to be temporarily slowed while
        casting an ability. I personally use it to increase the risk of certain ability use, to increase the chance of being hit
        by enemies attacks while casting it. Of course this is targetting abilities that can be casted while moving.
        */
        public override void InitCastMoveSlow(float speedPercent, float castSlowDuration, float castSlowRate)
        {
            curSpeedPercentage = 1;
            speedPercentageTarget = speedPercent;
            currentCastSlowDur = 0;
            maxCastSlowDur = castSlowDuration;
            speedCastSlowRate = castSlowRate;
            isCastingSlowed = true;
        }

        protected override void HandleCastSlowed()
        {
            curSpeedPercentage -= speedCastSlowRate;
            if (curSpeedPercentage < speedPercentageTarget) curSpeedPercentage = speedPercentageTarget;

            currentCastSlowDur += Time.deltaTime;

            MovementSpeedChange(GetMoveSpeed());

            if (currentCastSlowDur >= maxCastSlowDur) ResetCastSlow();
        }

        public float GetMoveSpeed()
        {
            float newMoveSpeed = RPGBuilderUtilities.getCurrentMoveSpeed(GameState.playerEntity);
            newMoveSpeed *= curSpeedPercentage;
            return (float) Math.Round(newMoveSpeed, 2);
        }

        protected override void ResetCastSlow()
        {
            isCastingSlowed = false;
            curSpeedPercentage = 1;
            speedPercentageTarget = 1;
            currentCastSlowDur = 0;
            maxCastSlowDur = 0;
            controller._anim.SetFloat(moveSpeedModifier, curSpeedPercentage);

            MovementSpeedChange(RPGBuilderUtilities.getCurrentMoveSpeed(GameState.playerEntity));
        }

        /*
        -- LOGIC UPDATES --
        */
        public override void FixedUpdate()
        {
            if (GameState.playerEntity == null) return;
            if (GameState.playerEntity.IsDead()) return;

            HandleCombatStates();

            if (knockbackActive)
                HandleKnockback();

            if (motionActive)
                HandleMotion();

            if (isTeleporting)
                HandleTeleporting();

            if (controller.isSprinting)
                HandleSprint();


            if (isResetingSprintCamFOV)
                HandleSprintCamFOVReset();

        }

        private void HandleSprintCamFOVReset()
        {
            controller.playerCamera.fieldOfView = Mathf.Lerp(controller.playerCamera.fieldOfView,
                controller.normalCameraFOV, Time.deltaTime * controller.cameraFOVLerpSpeed);

            if (Mathf.Abs(controller.playerCamera.fieldOfView - controller.normalCameraFOV) < 0.25f)
            {
                controller.playerCamera.fieldOfView = controller.normalCameraFOV;
                isResetingSprintCamFOV = false;
            }
        }
        
        protected override void HandleTeleporting()
        {
            transform.position = teleportTargetPos;
            isTeleporting = false;
        }

        protected override void HandleCombatStates()
        {
            if (isCastingSlowed) HandleCastSlowed();
            if (standTimeActive) HandleStandTime();
        }

        /*
        -- TELEPORT FUNCTIONS --
        Easy way to instantly teleport the player to a certain location.
        Called by DevUIManager and CombatManager
        */
        public override void TeleportToTarget(Vector3 pos) // Teleport to the Vector3 Coordinates
        {
            isTeleporting = true;
            teleportTargetPos = pos;
        }

        public override void TeleportToTarget(CombatEntity target) // Teleport to the CombatNode Coordinates
        {
            isTeleporting = true;
            teleportTargetPos = target.transform.position;
        }

        /*
        -- CHECKING CONDITIONAL FUNCTIONS --
        */
        public override bool HasMovementRestrictions()
        {
            if (CombatManager.Instance == null || GameState.playerEntity == null) return true;
            return GameState.playerEntity.IsDead() ||
                   !canMove ||
                   isTeleporting ||
                   standTimeActive ||
                   knockbackActive ||
                   motionActive ||
                   isLeaping ||
                   GameState.playerEntity.IsStunned() ||
                   GameState.playerEntity.IsSleeping();
        }

        public override bool HasRotationRestrictions()
        {
            if (CombatManager.Instance == null || GameState.playerEntity == null) return true;
            return GameState.playerEntity.IsDead() ||
                   isLeaping ||
                   knockbackActive ||
                   motionActive ||
                   GameState.playerEntity.IsStunned() ||
                   GameState.playerEntity.IsSleeping();
        }

        /*
        -- UI --
        */
        public override void GameUIPanelAction(bool opened)
        {
        }

        /*
         *
         * MOVEMENT
         */

        public override void StartSprint()
        {
            controller.isSprinting = true;
            controller.SetSpeed(GetMoveSpeed() * controller.sprintSpeedModifier);
        }

        public override void EndSprint()
        {
            controller.isSprinting = false;
            isResetingSprintCamFOV = true;
            controller.SetSpeed(GetMoveSpeed());
        }


        public override void HandleSprint()
        {
            if (controller.normalCameraFOV != controller.sprintingCameraFOV)
            {
                controller.playerCamera.fieldOfView = Mathf.Lerp(controller.playerCamera.fieldOfView,
                    controller.sprintingCameraFOV, Time.deltaTime * controller.cameraFOVLerpSpeed);
            }


            if (GameDatabase.Instance.GetSprintDrainStat() == null) return;

            if (!(Time.time >= nextSprintStatDrain)) return;
            nextSprintStatDrain = Time.time + GameDatabase.Instance.GetCharacterSettings().SprintStatDrainInterval;
            CombatUtilities.UpdateCurrentStatValue(GameState.playerEntity, GameDatabase.Instance.GetSprintDrainStat().ID, GameDatabase.Instance.GetCharacterSettings().SprintStatDrainAmount);
        }

        public override bool isSprinting()
        {
            return controller.isSprinting;
        }

        /*
        -- CONDITIONS --
        */
        public override bool ShouldCancelCasting()
        {
            return !IsGrounded() || IsMoving();
        }

        public override bool IsGrounded()
        {
            return true;
        }

        public override bool IsMoving()
        {
            return charController.velocity != Vector3.zero;
        }

        public override bool IsThirdPersonShooter()
        {
            return false;
        }

        public override RPGBuilderGeneralSettings.ControllerTypes GETControllerType()
        {
            return RPGBuilderGeneralSettings.ControllerTypes.TopDownWASD;
        }

        public override void MainMenuInit()
        {
            Destroy(GetComponent<TopDownWASDController>());
            Destroy(GetComponent<TopDownWASDControllerEssentials>());
        }
        
        public override void AbilityInitActions(RPGAbility.RPGAbilityRankData rankREF)
        {
            switch (rankREF.activationType)
            {
                case RPGAbility.AbilityActivationType.Casted when rankREF.faceCursorWhileCasting:
                    break;
                case RPGAbility.AbilityActivationType.Casted when rankREF.faceCursorWhenOnCastStart:
                    break;
            }
        }
        public override void AbilityEndCastActions(RPGAbility.RPGAbilityRankData rankREF)
        {
            switch (rankREF.activationType)
            {
                case RPGAbility.AbilityActivationType.Casted when rankREF.faceCursorWhileCasting:
                    break;
                case RPGAbility.AbilityActivationType.Casted when rankREF.faceCursorWhenOnCastEnd:
                    break;
            }
        }
    }
}
