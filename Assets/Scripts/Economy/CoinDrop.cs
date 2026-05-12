// Attach to: CoinDrop prefab
namespace ShooterGame.Economy
{
    public class CoinDrop : DropBase
    {
        private int _value;

        public void SetValue(int value) => _value = value;

        protected override void OnCollect() => CoinSystem.Instance?.Add(_value);
    }
}
