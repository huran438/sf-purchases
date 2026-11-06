using System;
using UnityEngine;

namespace SFramework.Purchases.Runtime
{
    internal static class SFAndroidReceiptParser
    {
        [Serializable]
        private class UnityReceipt
        {
            public string Store;
            public string TransactionID;
            public string Payload;
        }

        [Serializable]
        private class GooglePayloadWrapper
        {
            public string json;
            public string signature;
        }

        [Serializable]
        private class GoogleJson
        {
            public string packageName;
            public string applicationPackageName;
            public string productId;
            public string purchaseToken;
            public long purchaseTime;
            public int purchaseState;
        }

        internal static bool TryExtract(string receipt, out string packageName, out string productId,
            out string purchaseToken)
        {
            packageName = productId = purchaseToken = null;
            if (string.IsNullOrEmpty(receipt)) return false;

            try
            {
                var unity = JsonUtility.FromJson<UnityReceipt>(receipt);
                string inner = unity?.Payload ?? receipt;

                GooglePayloadWrapper wrap = null;
                try
                {
                    wrap = JsonUtility.FromJson<GooglePayloadWrapper>(inner);
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }

                var json = wrap?.json ?? inner;

                var g = JsonUtility.FromJson<GoogleJson>(json);
                if (g == null) return false;

                packageName = string.IsNullOrEmpty(g.packageName) ? g.applicationPackageName : g.packageName;
                productId = g.productId;
                purchaseToken = g.purchaseToken;

                return !string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(purchaseToken);
            }
            catch
            {
                return false;
            }
        }
    }
}