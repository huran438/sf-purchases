using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFramework.Core.Runtime;

namespace SFramework.Purchases.Runtime
{
    public interface ISFPurchasesService : ISFService
    {
        UniTask<SFPurchaseResult> PurchaseAsync(string productFullId, CancellationToken ct = default);
        void RestoreTransactions(Action<bool, string> callback = null);

        bool TryGetProduct(string productFullId, out SFPurchaseModel model);
        IEnumerable<SFPurchaseModel> Models { get; }
        bool IsReady(string productFullId);
        string GetLocalizedTitle(string productFullId);
        string GetLocalizedPrice(string productFullId);
        string GetIsoCurrency(string productFullId);
    }
}