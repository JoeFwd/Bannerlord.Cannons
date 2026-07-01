using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Plays the muzzle-blast particle burst and one of two randomly-selected fire sounds
    /// when the cannon discharges.
    /// </summary>
    public class FireEffectsPlayer : IFireEffectsPlayer
    {
        private int _fireSoundIndex;
        private int _fireSoundIndex2;
        private SoundEvent? _fireSound;
        private string _explosionEffect = string.Empty;
        private Scene? _scene;

        /// <inheritdoc/>
        public void Initialise(string fireSoundId1, string fireSoundId2, string explosionEffect, Scene scene)
        {
            _fireSoundIndex = SoundEvent.GetEventIdFromString(fireSoundId1);
            _fireSoundIndex2 = SoundEvent.GetEventIdFromString(fireSoundId2);
            _explosionEffect = explosionEffect;
            _scene = scene;
        }

        /// <inheritdoc/>
        public void Play(MatrixFrame muzzleFrame, Vec3 position)
        {
            AddParticleToFrame(muzzleFrame, _explosionEffect);

            if (_fireSound == null || !_fireSound.IsValid)
            {
                int fireSoundIndex = ChooseFireSoundIndex();
                if (_scene == null || fireSoundIndex < 0)
                    return;

                _fireSound = SoundEvent.CreateEvent(fireSoundIndex, _scene);
                if (_fireSound != null && _fireSound.IsValid)
                    _fireSound.PlayInPosition(position);
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (_fireSound != null && _fireSound.IsValid)
            {
                _fireSound.Stop();
                _fireSound.Release();
            }

            _fireSound = null;
        }

        private int ChooseFireSoundIndex()
        {
            bool fireSoundIndexIsValid = _fireSoundIndex >= 0;
            bool fireSoundIndex2IsValid = _fireSoundIndex2 >= 0;

            if (fireSoundIndexIsValid && fireSoundIndex2IsValid)
                return MBRandom.RandomFloat > 0.5f ? _fireSoundIndex : _fireSoundIndex2;

            if (fireSoundIndexIsValid)
                return _fireSoundIndex;

            return fireSoundIndex2IsValid ? _fireSoundIndex2 : -1;
        }

        private static void AddParticleToFrame(MatrixFrame frame, string particuleName)
        {
#if IS_MULTIPLAYER_BUILD
            Mission.Current.AddParticleSystemBurstByName(particuleName, frame, true);
#else
            Mission.Current.AddParticleSystemBurstByName(particuleName, frame, false);
#endif
        }
    }
}
