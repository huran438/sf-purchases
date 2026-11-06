using System;
using SFramework.Configs.Runtime;

namespace SFramework.Purchases.Runtime
{
    [Serializable]
    public class SFPurchaseNode : SFConfigNode
    {
        public string StoreId;
        public SFProductType ProductType = SFProductType.Consumable;
        public string GooglePlayId;
        public string AppleAppStoreId;
        public string HuaweiAppGalleryId;
        public string YandexStoreId;
        public string XsollaSku;
        public override ISFConfigNode[] Children => null;
    }
}