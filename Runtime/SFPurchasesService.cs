using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFramework.Configs.Runtime;

namespace SFramework.Purchases.Runtime
{
    /// <summary>Sandbox purchase service. Prefer SFMockPurchasesService for new sandbox registrations.</summary>
    public partial class SFPurchasesService : ISFPurchasesService
    {
        private ISFConfigsService _configsService;

        private Dictionary<string, SFPurchaseModel> _models = new(StringComparer.Ordinal);
        public IEnumerable<SFPurchaseModel> Models => _models.Values;

        public UniTask Init(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled(cancellationToken);

            var models = new Dictionary<string, SFPurchaseModel>(StringComparer.Ordinal);
            if (_configsService.TryGetConfigs(out SFPurchasesConfig[] configs))
            {
                if (configs == null) throw new InvalidOperationException("Purchase configurations are missing.");
                foreach (var cfg in configs)
                {
                    if (cfg?.Purchases == null || string.IsNullOrWhiteSpace(cfg.Id))
                        throw new InvalidOperationException("Purchase configurations require an ID and a product list.");

                    foreach (var node in cfg.Purchases)
                    {
                        if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled(cancellationToken);
                        if (node == null || string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.FullId))
                            throw new InvalidOperationException($"Purchase configuration '{cfg.Id}' contains an invalid product ID.");
                        var fullId = node.FullId;
                        if (models.ContainsKey(fullId)) continue;

                        var m = new SFPurchaseModel(node)
                        {
                            LocalizedTitle = node.StoreId,
                            LocalizedPriceString = "99.99",
                            IsoCurrencyCode = "USD",
                            Ready = true
                        };

                        models.Add(fullId, m);
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled(cancellationToken);
            _models = models;
            return UniTask.CompletedTask;
        }

        public void Dispose() => _models = new(StringComparer.Ordinal);

        public UniTask<SFPurchaseResult> PurchaseAsync(string productFullId, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested)
                return UniTask.FromResult(new SFPurchaseResult(SFPurchaseStatus.Canceled, productFullId, null, null, null));
            if (!IsReady(productFullId))
            {
                return UniTask.FromResult(new SFPurchaseResult(SFPurchaseStatus.Failed, productFullId,
                    null, null, null, "Unknown product"));
            }

            return UniTask.FromResult(new SFPurchaseResult(SFPurchaseStatus.Confirmed, productFullId, null, null, null));
        }


        public void RestoreTransactions(Action<bool, string> callback = null)
        {
            callback?.Invoke(true, null);
        }

        public bool TryGetProduct(string productFullId, out SFPurchaseModel model)
        {
            if (!string.IsNullOrWhiteSpace(productFullId)) return _models.TryGetValue(productFullId, out model);
            model = null;
            return false;
        }

        public bool IsReady(string productFullId) => TryGetProduct(productFullId, out _);

        public string GetLocalizedTitle(string productFullId) =>
            TryGetProduct(productFullId, out var m) ? m.LocalizedTitle ?? string.Empty : string.Empty;

        public string GetLocalizedPrice(string productFullId) =>
            TryGetProduct(productFullId, out var m) ? m.LocalizedPriceString ?? string.Empty : string.Empty;

        public string GetIsoCurrency(string productFullId) =>
            TryGetProduct(productFullId, out var m) ? m.IsoCurrencyCode ?? string.Empty : string.Empty;
    }
}
