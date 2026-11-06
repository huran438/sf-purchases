using SFramework.Configs.Runtime;

namespace SFramework.Purchases.Runtime
{
    public class SFPurchaseModel : SFNodeModel<SFPurchaseNode>
    {
        public bool Ready { get; internal set; }
        public string LocalizedTitle { get; internal set; }
        public string LocalizedPriceString { get; internal set; }
        public string IsoCurrencyCode { get; internal set; }

        public SFPurchaseModel(SFPurchaseNode node) : base(node) { }
        public override void Dispose() { }
    }
}