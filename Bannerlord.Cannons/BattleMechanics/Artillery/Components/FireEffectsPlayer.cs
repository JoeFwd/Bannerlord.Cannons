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
                if (MBRandom.RandomFloat > 0.5f)
                    _fireSound = SoundEvent.CreateEvent(_fireSoundIndex, _scene);
                else
                    _fireSound = SoundEvent.CreateEvent(_fireSoundIndex2, _scene);

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
