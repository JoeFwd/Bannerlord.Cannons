using System;
using TaleWorlds.MountAndBlade;
using System.Timers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using Timer = System.Timers.Timer;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.Extensions;
using TOR_Core.BattleMechanics.AI.CastingAI.Components;
using TaleWorlds.Localization;

namespace TOR_Core.AbilitySystem
{
    public abstract class Ability : IDisposable
    {
        private int _coolDownLeft = 0;
        private Timer _timer = null;
        private float _cooldown_end_time;

        protected bool _isLocked = false;
        public bool IsCasting { get; private set; }
        public string StringID { get; }
        public AbilityTemplate Template { get; protected set; }
        public AbilityScript AbilityScript { get; protected set; }
        public AbilityCrosshair Crosshair { get; private set; }
        public bool IsActivationPending { get; private set; }
        public bool IsActive => IsCasting || IsActivationPending || (AbilityScript != null && !AbilityScript.IsFading);
        public AbilityEffectType AbilityEffectType => Template.AbilityEffectType;
        public bool IsOnCooldown() => _timer.Enabled;
        public int GetCoolDownLeft() => _coolDownLeft;
        public bool IsSingleTarget => Template.AbilityTargetType == AbilityTargetType.SingleAlly || Template.AbilityTargetType == AbilityTargetType.SingleEnemy;
        public bool RequiresTargeting => Template.AbilityTargetType != AbilityTargetType.Self;

        public delegate void OnCastCompleteHandler(Ability ability);

        public event OnCastCompleteHandler OnCastComplete;

        public delegate void OnCastStartHandler(Ability ability);

        public event OnCastStartHandler OnCastStart;

        public Ability(AbilityTemplate template)
        {
            StringID = template.StringID;
            Template = template;
            _timer = new Timer(1000);
            _timer.Elapsed += TimerElapsed;
            _timer.Enabled = false;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Mission.Current == null)
            {
                FinalizeTimer();
                return;
            }

            _coolDownLeft = (int)(_cooldown_end_time - Mission.Current.CurrentTime);
            if (_coolDownLeft <= 0)
            {
                FinalizeTimer();
            }
        }

        private void FinalizeTimer()
        {
            _coolDownLeft = 0;
            _timer.Stop();
        }

        public virtual bool IsDisabled(Agent casterAgent, out TextObject disabledReason)
        {
            disabledReason = new TextObject("{=!}Enabled");
            if (IsOnCooldown())
            {
                disabledReason = new TextObject("{=!}On cooldown");
                return true;
            }
            if (_isLocked)
            {
                disabledReason = new TextObject("{=!}Mission is over");
                return true;
            }
            if (IsCasting)
            {
                disabledReason = new TextObject("{=!}Casting");
                return true;
            }

            return false;
        }

        public bool TryCast(Agent casterAgent, out TextObject failureReason)
        {
            if (CanCast(casterAgent, out failureReason))
            {
                DoCast(casterAgent);
                failureReason = null;
                return true;
            }
            return false;
        }

        public virtual bool CanCast(Agent casterAgent, out TextObject failureReason)
        {
            if(IsDisabled(casterAgent, out failureReason))
            {
                return false;
            }
            if(casterAgent.IsPlayerControlled && !IsRightAngleToCast())
            {
                failureReason = new TextObject("Can only cast in a frontal cone");
                return false;
            }
            if(!casterAgent.IsActive() || casterAgent.Health <= 0 || (casterAgent.IsAIControlled && casterAgent.GetMorale() <= 1) || !casterAgent.IsAbilityUser())
            {
                failureReason = new TextObject("Caster is dead or routed");
                return false;
            }
            failureReason = null;
            return true;
        }

        protected virtual void DoCast(Agent casterAgent)
        {
            OnCastStart?.Invoke(this);
            SetAnimationAction(casterAgent);
            if (Template.CastType == CastType.Instant)
            {
                ActivateAbility(casterAgent);
            }
        }

        public void SetCoolDown(int cooldownTime)
        {
            _coolDownLeft = cooldownTime;
            _cooldown_end_time = Mission.Current.CurrentTime + _coolDownLeft + 0.8f; //Adjustment was needed for natural tick on UI
            _timer.Start();
        }

        public virtual void DeactivateAbility()
        {
            _isLocked = true;
        }
        public virtual void ActivateAbility(Agent casterAgent)
        {
            IsActivationPending = false;
            IsCasting = false;
            bool prayerCoolSeperated = false;
            ExplainedNumber cooldown = new(Template.CoolDown);

            SetCoolDown((int)cooldown.ResultNumber);

            var frame = GetSpawnFrame(casterAgent);

            if (Template.DoNotAlignParticleEffectPrefab)
            {
                frame = new MatrixFrame(Mat3.CreateMat3WithForward(Vec3.Forward), frame.origin);
            }

            GameEntity parentEntity = GameEntity.CreateEmpty(Mission.Current.Scene, false);
            parentEntity.SetGlobalFrameMT(frame);

            AddLight(ref parentEntity);

            AddBehaviour(ref parentEntity, casterAgent);
            OnCastComplete?.Invoke(this);
        }

        protected MatrixFrame GetSpawnFrame(Agent casterAgent)
        {
            if (casterAgent.IsPlayerControlled)
            {
                var comp = casterAgent.GetComponent<AbilityComponent>();
                if (comp != null)
                {
                    return comp.LastCastWasQuickCast ? CalculateQuickCastMatrixFrame(casterAgent) : CalculatePlayerCastMatrixFrame(casterAgent);
                }
                else throw new NullReferenceException("casterAgent's abilitycomponent is null");
            }
            else if (casterAgent.IsAIControlled) return CalculateAICastMatrixFrame(casterAgent);
            else throw new ArgumentException("casterAgent's controller is none");
        }

        private MatrixFrame CalculateQuickCastMatrixFrame(Agent casterAgent)
        {
            var frame = casterAgent.LookFrame;
            switch (AbilityEffectType)
            {
                case AbilityEffectType.ArtilleryPlacement:
                    frame.origin =
                        Mission.Current.GetRandomPositionAroundPoint(Agent.Main.GetWorldPosition().GetGroundVec3MT(), 3, 6, false);
                    break;
            }

            return frame;
        }

        private MatrixFrame CalculatePlayerCastMatrixFrame(Agent casterAgent)
        {
            var frame = casterAgent.LookFrame;
            switch (Template.AbilityEffectType)
            {
                case AbilityEffectType.ArtilleryPlacement:
                    {
                        frame = new MatrixFrame(Mat3.Identity, Crosshair.Position);
                        break;
                    }
            }

            return frame;
        }

        private MatrixFrame CalculateAICastMatrixFrame(Agent casterAgent)
        {
            var frame = casterAgent.LookFrame;
            var wizardAIComponent = casterAgent.GetComponent<WizardAIComponent>();
            var target = wizardAIComponent.CurrentCastingBehavior.CurrentTarget;

            switch (Template.AbilityEffectType)
            {
                case AbilityEffectType.ArtilleryPlacement:
                    {
                        frame = new MatrixFrame(Mat3.Identity, target.GetPositionPrioritizeCalculated());
                        target.SelectedWorldPosition = Vec3.Zero;
                        break;
                    }
            }

            return frame;
        }

        protected GameEntity SpawnEntity()
        {
            GameEntity entity = null;
            if (Template.ParticleEffectPrefab != "none")
            {
                entity = GameEntity.Instantiate(Mission.Current.Scene, Template.ParticleEffectPrefab, true);
            }

            if (entity == null)
            {
                entity = GameEntity.CreateEmpty(Mission.Current.Scene);
            }

            return entity;
        }

        private void AddLight(ref GameEntity entity)
        {
            if (Template.HasLight)
            {
                var light = Light.CreatePointLight(Template.LightRadius);
                light.Intensity = Template.LightIntensity;
                light.LightColor = Template.LightColorRGB;
                light.SetShadowType(Light.ShadowType.DynamicShadow);
                light.ShadowEnabled = Template.ShadowCastEnabled;
                light.SetLightFlicker(Template.LightFlickeringMagnitude, Template.LightFlickeringInterval);
                light.Frame = MatrixFrame.Identity;
                light.SetVisibility(true);
                entity.AddLight(light);
            }
        }

        protected void AddBehaviour(ref GameEntity entity, Agent casterAgent)
        {
            switch (Template.AbilityEffectType)
            {
                case AbilityEffectType.ArtilleryPlacement:
                    AddExactBehaviour<ArtilleryPlacementScript>(entity, casterAgent);
                    break;
            }

            if (IsSingleTarget)
            {
                if (casterAgent.IsAIControlled)
                {
                    var wizardAiComponent = casterAgent.GetComponent<WizardAIComponent>();
                    if (wizardAiComponent.CurrentCastingBehavior?.CurrentTarget is not null)
                    {
                        AbilityScript.SetExplicitTargetAgents([wizardAiComponent.CurrentCastingBehavior?.CurrentTarget.Agent]);    
                    }
                    
                    
                }

            }
        }

        protected virtual void AddExactBehaviour<TAbilityScript>(GameEntity parentEntity, Agent casterAgent)
            where TAbilityScript : AbilityScript
        {
            parentEntity.CreateAndAddScriptComponent(typeof(TAbilityScript).Name);
            AbilityScript = parentEntity.GetFirstScriptOfType<TAbilityScript>();
            var prefabEntity = SpawnEntity();
            parentEntity.AddChild(prefabEntity);
            AbilityScript?.Initialize(this);
            AbilityScript?.SetCasterAgent(casterAgent);
            parentEntity.CallScriptCallbacks();
        }

        private void SetAnimationAction(Agent casterAgent)
        {
            if (Template.AnimationActionName != "none")
            {
                casterAgent.SetActionChannel(1, ActionIndexCache.Create(Template.AnimationActionName));
            }
        }

        public void SetCrosshair(AbilityCrosshair crosshair)
        {
            Crosshair = crosshair;
        }

        public void Dispose()
        {
            _timer.Dispose();
            _timer = null;
            Template = null;
            OnCastComplete = null;
            OnCastStart = null;
        }

        private bool IsRightAngleToCast()
        {
            if (Agent.Main.HasMount)
            {
                double xa = Agent.Main.LookDirection.X;
                double ya = Agent.Main.LookDirection.Y;
                double xb = Agent.Main.GetMovementDirection().X;
                double yb = Agent.Main.GetMovementDirection().Y;

                double angle = Math.Acos((xa * xb + ya * yb) / (Math.Sqrt(Math.Pow(xa, 2) + Math.Pow(ya, 2)) * Math.Sqrt(Math.Pow(xb, 2) + Math.Pow(yb, 2))));

                return true ? angle < 1.4 : angle >= 1.4;
            }
            else
            {
                return true;
            }
        }
    }
}