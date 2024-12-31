using System;
using System.Xml.Serialization;
using TOR_Core.AbilitySystem;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    [Serializable]
    public class TriggeredEffectTemplate : ITemplate
    {
        [XmlAttribute]
        public string StringID { get; set; } = "";
        [XmlAttribute]
        public string BurstParticleEffectPrefab { get; set; } = "none";
        [XmlAttribute]
        public bool DoNotAlignParticleEffectPrefabOnImpact { get; set; } = false;
        [XmlAttribute]
        public string SoundEffectId { get; set; } = "none";
        [XmlAttribute]
        public float SoundEffectLength { get; set; } = 2.5f;
        [XmlAttribute]
        public int DamageAmount { get; set; } = 50;
        [XmlAttribute]
        public float Radius { get; set; } = 5;
        [XmlAttribute]
        public bool HasShockWave { get; set; } = false;
        [XmlAttribute]
        public TargetType TargetType { get; set; } = TargetType.Enemy;
        [XmlAttribute]
        public float DamageVariance { get; set; } = 0.2f;
        [XmlAttribute]
        public string ScriptNameToTrigger { get; set; } = "none";
        [XmlAttribute]
        public string SpawnPrefabName { get; set; } = "none";
        [XmlAttribute]
        public string TroopIdToSummon { get; set; } = "none";
        [XmlAttribute]
        public int NumberToSummon { get; set; } = 0;

        public ITemplate Clone(string newId)
        {
            return new TriggeredEffectTemplate()
            {
                StringID = newId,
                BurstParticleEffectPrefab = BurstParticleEffectPrefab,
                SoundEffectId = SoundEffectId,
                SoundEffectLength = SoundEffectLength,
                DamageAmount = DamageAmount,
                Radius = Radius,
                HasShockWave = HasShockWave,
                TargetType = TargetType,
                DamageVariance = DamageVariance,
                ScriptNameToTrigger = ScriptNameToTrigger,
                SpawnPrefabName = SpawnPrefabName,
                TroopIdToSummon = TroopIdToSummon,
                NumberToSummon = NumberToSummon
            };
        }
    }
}
