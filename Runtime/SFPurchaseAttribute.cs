using SFramework.Configs.Runtime;

namespace SFramework.Purchases.Runtime
{
    public class SFPurchaseAttribute : SFIdAttribute
    {
        public SFPurchaseAttribute() : base(typeof(SFPurchasesConfig))
        {
        }
    }
}
