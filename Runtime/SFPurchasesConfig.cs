using System;
using SFramework.Configs.Runtime;

namespace SFramework.Purchases.Runtime
{
    public class SFPurchasesConfig : SFNodesConfig
    {
        public SFPurchaseNode[] Purchases = Array.Empty<SFPurchaseNode>();
        public override ISFConfigNode[] Children => Purchases;
    }
}