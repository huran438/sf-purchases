using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SFramework.Configs.Runtime;
using SFramework.Core.Runtime;

namespace SFramework.Purchases.Runtime
{
    public sealed class SFPurchasesService : ISFPurchasesService
    {
        [SFInject]
        private readonly ISFConfigsService _configsService;

        private readonly Dictionary<string, SFPurchaseModel> _models = new(StringComparer.Ordinal);
        public IEnumerable<SFPurchaseModel> Models => _models.Values;

        public UniTask Init(CancellationToken cancellationToken)
        {
            _models.Clear();

            if (_configsService.TryGetConfigs(out SFPurchasesConfig[] configs))
            {
                foreach (var cfg in configs)
                {
                    foreach (var node in cfg.Purchases)
                    {
                        var fullId = node.FullId;
                        if (_models.ContainsKey(fullId)) continue;

                        var m = new SFPurchaseModel(node)
                        {
                            LocalizedTitle = node.StoreId,
                            LocalizedPriceString = "99.99",
                            IsoCurrencyCode = "USD",
                            Ready = true
                        };

                        _models.Add(fullId, m);
                    }
                }
            }

            return UniTask.CompletedTask;
        }

        public void Dispose() => _models.Clear();

        public UniTask<SFPurchaseResult> PurchaseAsync(string productFullId, CancellationToken ct = default)
        {
            // Always succeed immediately
            if (!_models.ContainsKey(productFullId))
            {
                return UniTask.FromResult(new SFPurchaseResult(SFPurchaseStatus.Failed, productFullId,
                    "Unknown product", null, null, "Unknown product"));
            }

            return new UniTask<SFPurchaseResult>(new SFPurchaseResult(SFPurchaseStatus.Confirmed, productFullId, null,
                null, null, null));
        }


        public void RestoreTransactions(Action<bool, string> callback = null)
        {
            callback?.Invoke(true, null);
        }

        public bool TryGetProduct(string productFullId, out SFPurchaseModel model) =>
            _models.TryGetValue(productFullId, out model);

        public bool IsReady(string productFullId) => _models.ContainsKey(productFullId);

        public string GetLocalizedTitle(string productFullId) =>
            _models.TryGetValue(productFullId, out var m) ? m.LocalizedTitle ?? string.Empty : string.Empty;

        public string GetLocalizedPrice(string productFullId) =>
            _models.TryGetValue(productFullId, out var m) ? m.LocalizedPriceString ?? string.Empty : string.Empty;

        public string GetIsoCurrency(string productFullId) =>
            _models.TryGetValue(productFullId, out var m) ? m.IsoCurrencyCode ?? string.Empty : string.Empty;
    }
}