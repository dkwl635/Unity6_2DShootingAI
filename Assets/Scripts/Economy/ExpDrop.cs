// Attach to: PowerDrop prefab
using ShooterGame.Core;
using ShooterGame.Effects;

namespace ShooterGame.Economy
{
    public class PowerDrop : DropBase
    {
        private int _value;

        public void SetValue(int value) => _value = value;

        protected override void OnCollect() => PowerSystem.Instance?.Add(_value);
        protected override EffectType PickupEffect => EffectType.PowerPickup;
        protected override SfxType   PickupSfx    => SfxType.PowerPickup;
    }
}
