using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.TriggeredEffect;

namespace TOR_Core.AbilitySystem.Scripts
{
    public abstract class AbilityScript : ScriptComponentBehavior
    {
        private Ability _ability;
        private int _soundIndex = -1;
        private SoundEvent _sound;
        private Agent _casterAgent;
        private float _abilityLife = -1;
        private float _timeSinceLastTick = 0;
        private bool _hasCollided;
        private bool _hasTickedOnce;
        private bool _hasTriggered;
        private float _minArmingTimeForCollision = 0.1f;
        private bool _canCollide;
        private bool _soundStarted;
        private MBList<Agent> _targetAgents;
        private float _additionalDuration = 0;
        private Vec3 _previousFrameOrigin = Vec3.Zero;

        public Agent CasterAgent => _casterAgent;
        public MBReadOnlyList<Agent> ExplicitTargetAgents
        {
            get
            {
                if (_targetAgents == null) return [];
                else return new MBReadOnlyList<Agent>(_targetAgents);
            }
        }
        public Ability Ability => _ability;
        public bool IsFading { get; private set; }

        public void SetCasterAgent(Agent agent) => _casterAgent = agent;
        
        public void SetExplicitTargetAgents(MBList<Agent> agents) => _targetAgents = agents;

        protected override bool MovesEntity() => true;

        protected virtual bool ShouldMove()
        {
            return false;
        }

        protected override void OnInit()
        {
            SetScriptComponentToTick(GetTickRequirement());
        }

        public override TickRequirement GetTickRequirement()
        {
            return TickRequirement.Tick;
        }

        public virtual void Initialize(Ability ability)
        {
            _ability = ability;
            if (_ability.Template.SoundEffectToPlay != "none" && _ability.Template.SoundEffectToPlay != null)
            {
                _soundIndex = SoundEvent.GetEventIdFromString(_ability.Template.SoundEffectToPlay);
                _sound = SoundEvent.CreateEvent(_soundIndex, Scene);
            }
        }

        protected virtual void OnBeforeTick(float dt) { } 
        protected virtual void OnAfterTick(float dt) { }

        protected sealed override void OnTick(float dt)
        {
            if (Mission.Current.CurrentState != Mission.State.Continuing || 
                Mission.Current.MissionEnded || 
                Mission.Current.IsMissionEnding ||
                Mission.Current.MissionIsEnding)
            {
                return;
            }

            if (_ability == null) return;

            OnBeforeTick(dt);

            _timeSinceLastTick += dt;
            UpdateLifeTime(dt);

            if (IsFading) return;

            var frame = GameEntity.GetGlobalFrame();
            UpdateSound(frame.origin);

            if (_ability.Template.TriggerType == TriggerType.TickOnce && _abilityLife > _ability.Template.TickInterval && !_hasTriggered)
            {
                var position = frame.origin;
                var normal = frame.origin.NormalizedCopy();

                TriggerEffects(position, normal);
                _hasTriggered = true;
            }
            _hasTickedOnce = true;

            if (ShouldMove())
            {
                UpdatePosition(frame, dt);
            }

            OnAfterTick(dt);
        }

        private void UpdatePosition(MatrixFrame frame, float dt)
        {
            _previousFrameOrigin = frame.origin;
            var newframe = GetNextGlobalFrame(frame, dt);
            GameEntity.SetGlobalFrameMT(newframe);
            using(new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
            {
                GameEntity.GetBodyShape()?.ManualInvalidate();
            }
        }

        protected virtual MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
        {
            return oldFrame.Advance(_ability.Template.BaseMovementSpeed * dt);
        }

        private void UpdateLifeTime(float dt)
        {
            if (_abilityLife < 0) _abilityLife = 0;
            else _abilityLife += dt;
            if (_abilityLife > _minArmingTimeForCollision)
            {
                _canCollide = true;
            }
        }

        private void UpdateSound(Vec3 position)
        {
            if(_sound != null)
            {
                _sound.SetPosition(position);
                if (IsSoundPlaying()) return;
                else
                {
                    if (!_soundStarted)
                    {
                        _sound.Play();
                        _soundStarted = true;
                    }
                    else if (_ability.Template.ShouldSoundLoopOverDuration)
                    {
                        _sound.Play();
                    }
                    else
                    {
                        _sound.Release();
                        _sound = null;
                    }
                }
            }
        }

        private bool IsSoundPlaying()
        {
            return _sound != null && _sound.IsValid && _sound.IsPlaying();
        }

        protected virtual bool CollidedWithAgent()
        {
            if(!_canCollide) return false;
            var collisionRadius = _ability.Template.Radius + 1;
            MBList<Agent> agents = [];
            agents = Mission.Current.GetNearbyAgents(GameEntity.GetGlobalFrame().origin.AsVec2, collisionRadius, agents);
            return agents.Any(agent => agent != _casterAgent && Math.Abs(GameEntity.GetGlobalFrame().origin.Z - agent.Position.Z) < collisionRadius);
        }

        protected sealed override void OnPhysicsCollision(ref PhysicsContact contact)
        {
            base.OnPhysicsCollision(ref contact);
        }

        private void TriggerEffects(Vec3 position, Vec3 normal)
        {
            var effects = GetEffectsToTrigger();
            foreach(var effect in effects)
            {
                if (effect != null)
                {
                    if (_ability.Template.AbilityTargetType == AbilityTargetType.Self)
                    {
                        effect.Trigger(position, normal, _casterAgent, _ability.Template, [_casterAgent]);
                    }
                    else if(_targetAgents != null && _targetAgents.Count() > 0)
                    {
                        effect.Trigger(position, normal, _casterAgent, _ability.Template, _targetAgents.ToMBList());
                    }
                    else effect.Trigger(position, normal, _casterAgent, _ability.Template);
                }
            }
        }

        protected virtual List<TriggeredEffect> GetEffectsToTrigger()
        {
            List<TriggeredEffect> effects = [];
            if (_ability == null) return effects; 
            foreach(var effect in _ability.Template.AssociatedTriggeredEffectTemplates)
            {
                effects.Add(new TriggeredEffect(effect));
            }
            return effects;
        }

        protected sealed override void OnRemoved(int removeReason)
        {
            OnBeforeRemoved(removeReason);
            _sound?.Release();
            _sound = null;
            _ability = null;
            _casterAgent = null;
        }

        protected virtual void OnBeforeRemoved(int removeReason) { }
    }
}
