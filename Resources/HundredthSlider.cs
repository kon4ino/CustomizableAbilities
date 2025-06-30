using Satchel.BetterMenus;

namespace CustomizableNailDamage
{
    public class HundredthSlider : CustomSlider
    {
        public HundredthSlider(
            string name,
            System.Action<float> storeValue,
            System.Func<float> loadValue,
            float minValue,
            float maxValue,
            bool wholeNumbers = false,
            string Id = "__UseName"
        ) : base(name, storeValue, loadValue, minValue, maxValue, wholeNumbers, Id)
        {
            if (!Name.EndsWith("\n"))
            {
                Name += "\n";
            }
        }

        protected override void UpdateValueLabel()
        {
            valueLabel.text = $"{value:0.00}";
        }
    }
}
