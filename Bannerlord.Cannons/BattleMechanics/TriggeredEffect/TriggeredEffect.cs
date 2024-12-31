using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using System;
using TOR_Core.BattleMechanics.TriggeredEffect.Scripts;
using System.Collections.Generic;
using Bannerlord.Cannons.Logging;
using TOR_Core.AbilitySystem;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using Timer = System.Timers.Timer;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class TriggeredEffect(TriggeredEffectTemplate template, bool isTemplateMutated = false) : IDisposable
    {
        private const float DefaultStatusEffectDuration = 5;

        private static readonly ILogger Logger = new ConsoleLoggerFactory().CreateLogger<TriggeredEffect>();
        
        private TriggeredEffectTemplate _template = template;
        private int _soundIndex;
        private SoundEvent _sound;
        private Timer _timer;
        private readonly object _sync = new();
        private readonly bool _isTemplateMutated = isTemplateMutated;

        public float EffectRadius => _template.Radius;
        public string SummonedTroopId => _template.TroopIdToSummon;
        public void Trigger(Vec3 position, Vec3 normal, Agent triggererAgent, AbilityTemplate originAbilityTemplate = null, MBList<Agent> targets = null)
        {
            if (_template == null || !triggererAgent.IsActive()) return;
            _timer = new Timer(2000)
            {
                AutoReset = false,
                Enabled = false
            };
            _timer.Elapsed += (s, e) =>
            {
                lock (_sync)
                {
                    Dispose();
                }
            };
            if (_template.SoundEffectLength > 0)
            {
                _timer.Interval = _template.SoundEffectLength * 1000;
            }
            _timer.Start();

            float radius = _template.Radius;

            //Determine targets
            if (targets == null && triggererAgent != null)
            {
                targets = [];
                if(_template.TargetType == TargetType.Self)
                {
                    targets.Add(triggererAgent);
                }
                else if (_template.TargetType == TargetType.Enemy)
                {
                    targets = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, radius, triggererAgent.Team, targets);
                }
                else if (_template.TargetType == TargetType.Friendly)
                {
                    targets = Mission.Current.GetNearbyAllyAgents(position.AsVec2, radius, triggererAgent.Team, targets);
                }
                else if (_template.TargetType == TargetType.All)
                {
                    targets = Mission.Current.GetNearbyAgents(position.AsVec2, radius, targets);
                }
            }
            //Cause Damage
            // TODO: clean completely damage as cannon script damage value is 0
            // if (_template.DamageAmount > 0)
            // {
            //     TORMissionHelper.DamageAgents(targets, (int)(_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, _template, _template.DamageType, _template.HasShockWave, position, originAbilityTemplate);
            // }
            // else if (_template.DamageAmount < 0)
            // {
            //     TORMissionHelper.HealAgents(targets, (int)(-_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(-_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, originAbilityTemplate);
            // }
            if (_template.DoNotAlignParticleEffectPrefabOnImpact)
            {
                var groundPos = new Vec3(position.x, position.y, position.z - 5f);
                using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
                {
                    Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(position, groundPos, out float distance, 0.01f, BodyFlags.CommonCollisionExcludeFlagsForAgent);
                    if (distance >= 0.0000001f)
                    {
                        position = new Vec3(position.x, position.y, position.z - distance);
                    }
                }
                normal = Vec3.Forward;
            }

            SpawnVisuals(position, normal);
            PlaySound(position);
            TriggerScript(position, triggererAgent, targets, DefaultStatusEffectDuration);
        }

        private void SpawnVisuals(Vec3 position, Vec3 normal)
        {
            //play visuals
            if (_template!=null&&_template.BurstParticleEffectPrefab != "none")
            {
                var effect = GameEntity.CreateEmpty(Mission.Current.Scene);
                MatrixFrame frame = MatrixFrame.Identity;
                ParticleSystem.CreateParticleSystemAttachedToEntity(_template.BurstParticleEffectPrefab, effect, ref frame);
                var globalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in normal), position);
                effect.SetGlobalFrame(globalFrame);
                effect.FadeOut(_template.SoundEffectLength, true);
            }
        }

        private void PlaySound(Vec3 position)
        {
            //play sound
            if (_template!=null&&_template.SoundEffectId != "none")
            {
                _soundIndex = SoundEvent.GetEventIdFromString(_template.SoundEffectId);
                _sound = SoundEvent.CreateEvent(_soundIndex, Mission.Current.Scene);
                _sound?.PlayInPosition(position);
            }
        }

        private void TriggerScript(Vec3 position, Agent triggerer, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if (_template!=null&&_template.ScriptNameToTrigger != "none")
            {
                try
                {
                    var obj = Activator.CreateInstance(Type.GetType(_template.ScriptNameToTrigger));
                    if(obj is PrefabSpawnerScript)
                    {
                        var script = obj as PrefabSpawnerScript;
                        script.OnInit(_template.SpawnPrefabName);
                    }
                    if (obj is ITriggeredScript)
                    {
                        var script = obj as ITriggeredScript;
                        script.OnTrigger(position, triggerer, triggeredAgents, duration);
                    }
                }
                catch (Exception)
                {
                    Logger.Error("Tried to spawn TriggeredScript: " + _template.ScriptNameToTrigger + ", but failed.");
                }
            }
        }

        public void Dispose()
        {
            CleanUp();
        }

        private void CleanUp()
        {
            _sound?.Release();
            _sound = null;
            _soundIndex = -1;
            _template = null;
            _timer.Stop();
        }
    }
}
