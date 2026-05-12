// Attach to: ExpDrop prefab
namespace ShooterGame.Economy
{
    public class ExpDrop : DropBase
    {
        private int _value;

        public void SetValue(int value) => _value = value;

        protected override void OnCollect() => ExpSystem.Instance?.Add(_value);
    }
}
