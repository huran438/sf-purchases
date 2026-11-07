using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SFramework.Configs.Runtime;
using SFramework.Core.Runtime;
using SFramework.Purchases.Runtime;
using UnityEngine;

namespace SFramework.Purchases.Editor.Tests
{
    public sealed class SFPurchasesServiceTests
    {
        private readonly List<SFServiceContainer> _containers = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var container in _containers) container.Dispose();
            _containers.Clear();
        }

        [Test]
        public void DefaultResultDoesNotConfirmAPurchase()
        {
            Assert.That(default(SFPurchaseResult).Success, Is.False);
            Assert.That(new SFPurchaseResult(SFPurchaseStatus.Confirmed, null, null, null, null).Success, Is.True);
            Assert.That((int)SFPurchaseStatus.Confirmed, Is.Zero);
        }

        [Test]
        public void LegacyServiceCanStillBeRegistered()
        {
            var builder = new SFContainerBuilder();
            builder.RegisterInstance<ISFConfigsService>(new MemoryConfigsService(Config("shop", new SFPurchaseNode { Id = "item" })));
            builder.Register<ISFPurchasesService, SFPurchasesService>();
            using var container = builder.Build();
            var service = container.Resolve<ISFPurchasesService>();
            service.Init(CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(service.IsReady("shop/item"), Is.True);
        }

        [Test]
        public void Init_IndexesProductsAcrossConfigurationsUsingQualifiedIds()
        {
            var first = Config("coins", new SFPurchaseNode { Id = "small", StoreId = "store.coins.small" });
            var second = Config("gems", new SFPurchaseNode { Id = "small", StoreId = "store.gems.small" });
            var service = CreateService(first, second);

            service.Init(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(service.Models.Count(), Is.EqualTo(2));
            Assert.That(service.TryGetProduct("coins/small", out var coins), Is.True);
            Assert.That(service.TryGetProduct("gems/small", out var gems), Is.True);
            Assert.That(coins.Node, Is.SameAs(first.Purchases[0]));
            Assert.That(gems.Node, Is.SameAs(second.Purchases[0]));
            Assert.That(service.GetLocalizedTitle("coins/small"), Is.EqualTo("store.coins.small"));
            Assert.That(service.GetLocalizedTitle("gems/small"), Is.EqualTo("store.gems.small"));
            Assert.That(service.IsReady("coins/small"), Is.True);
            Assert.That(service.IsReady("small"), Is.False);
        }

        [Test]
        public void Init_DuplicateQualifiedProductId_KeepsFirstConfiguredProduct()
        {
            var first = Config("products", new SFPurchaseNode { Id = "item", StoreId = "first" });
            var second = Config("products", new SFPurchaseNode { Id = "item", StoreId = "second" });
            var service = CreateService(first, second);

            service.Init(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(service.Models.Count(), Is.EqualTo(1));
            Assert.That(service.TryGetProduct("products/item", out var model), Is.True);
            Assert.That(model.Node, Is.SameAs(first.Purchases[0]));
            Assert.That(service.GetLocalizedTitle("products/item"), Is.EqualTo("first"));
        }

        [Test]
        public void PurchaseAsync_ConfiguredProduct_ReturnsConfirmedResultForRequestedId()
        {
            var service = CreateService(Config("products", new SFPurchaseNode { Id = "item", StoreId = "store.item" }));
            service.Init(CancellationToken.None).GetAwaiter().GetResult();

            var result = service.PurchaseAsync("products/item").GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(SFPurchaseStatus.Confirmed));
            Assert.That(result.Success, Is.True);
            Assert.That(result.FullId, Is.EqualTo("products/item"));
            Assert.That(result.FailureReason, Is.Null);
        }

        [Test]
        public void PurchaseAsync_UnknownProduct_ReturnsFailureAndPreservesRequestedId()
        {
            var service = CreateService();
            service.Init(CancellationToken.None).GetAwaiter().GetResult();

            var result = service.PurchaseAsync("products/missing").GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(SFPurchaseStatus.Failed));
            Assert.That(result.Success, Is.False);
            Assert.That(result.FullId, Is.EqualTo("products/missing"));
            Assert.That(result.FailureReason, Is.Not.Null.And.Not.Empty);
            Assert.That(result.TransactionId, Is.Null);
            Assert.That(result.Receipt, Is.Null);
        }

        [Test]
        public void InitAgain_RebuildsModelsFromCurrentConfiguration()
        {
            var config = Config("products", new SFPurchaseNode { Id = "old", StoreId = "store.old" });
            var service = CreateService(config);
            service.Init(CancellationToken.None).GetAwaiter().GetResult();
            config.Purchases = new[] { new SFPurchaseNode { Id = "new", StoreId = "store.new" } };
            config.BuildTree();

            service.Init(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(service.IsReady("products/old"), Is.False);
            Assert.That(service.TryGetProduct("products/old", out var old), Is.False);
            Assert.That(old, Is.Null);
            Assert.That(service.IsReady("products/new"), Is.True);
            Assert.That(service.Models.Count(), Is.EqualTo(1));
        }

        [Test]
        public void FailedOrCanceledInitKeepsPreviouslyPublishedModels()
        {
            var config = Config("products", new SFPurchaseNode { Id = "old" });
            var service = CreateService(config);
            service.Init(CancellationToken.None).GetAwaiter().GetResult();
            config.Purchases = new[] { new SFPurchaseNode { Id = "new", FullId = "products/new" }, null };

            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            Assert.Throws<OperationCanceledException>(() => service.Init(canceled.Token).GetAwaiter().GetResult());
            Assert.Throws<InvalidOperationException>(() => service.Init(CancellationToken.None).GetAwaiter().GetResult());
            Assert.That(service.IsReady("products/old"), Is.True);
            Assert.That(service.Models.Count(), Is.EqualTo(1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void BlankProductIdIsHandledLikeAnUnknownProduct(string id)
        {
            var service = CreateService();
            Assert.That(service.PurchaseAsync(id).GetAwaiter().GetResult().Status, Is.EqualTo(SFPurchaseStatus.Failed));
            Assert.That(service.TryGetProduct(id, out var product), Is.False);
            Assert.That(product, Is.Null);
            Assert.That(service.GetLocalizedTitle(id), Is.Empty);
        }

        [Test]
        public void CanceledPurchaseNeverConfirms()
        {
            var service = CreateService(Config("products", new SFPurchaseNode { Id = "item" }));
            service.Init(CancellationToken.None).GetAwaiter().GetResult();
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();

            var result = service.PurchaseAsync("products/item", canceled.Token).GetAwaiter().GetResult();
            Assert.That(result.Status, Is.EqualTo(SFPurchaseStatus.Canceled));
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Dispose_RemovesProductsAndSubsequentPurchasesFail()
        {
            var service = CreateService(Config("products", new SFPurchaseNode { Id = "item" }));
            service.Init(CancellationToken.None).GetAwaiter().GetResult();

            service.Dispose();

            Assert.That(service.Models, Is.Empty);
            Assert.That(service.TryGetProduct("products/item", out var model), Is.False);
            Assert.That(model, Is.Null);
            Assert.That(service.IsReady("products/item"), Is.False);
            Assert.That(service.PurchaseAsync("products/item").GetAwaiter().GetResult().Success, Is.False);
        }

        [Test]
        public void UnknownProductMetadata_ReturnsEmptyStrings()
        {
            var service = CreateService();
            service.Init(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(service.GetLocalizedTitle("missing"), Is.Empty);
            Assert.That(service.GetLocalizedPrice("missing"), Is.Empty);
            Assert.That(service.GetIsoCurrency("missing"), Is.Empty);
        }

        [Test]
        public void RestoreTransactions_InvokesOptionalCallbackOnceWithSuccess()
        {
            var service = CreateService();
            var calls = 0;

            service.RestoreTransactions((success, message) =>
            {
                calls++;
                Assert.That(success, Is.True);
                Assert.That(message, Is.Null);
            });
            Assert.DoesNotThrow(() => service.RestoreTransactions());

            Assert.That(calls, Is.EqualTo(1));
        }

        private ISFPurchasesService CreateService(params ISFConfig[] configs)
        {
            var builder = new SFContainerBuilder();
            builder.RegisterInstance<ISFConfigsService>(new MemoryConfigsService(configs));
            builder.Register<ISFPurchasesService, SFMockPurchasesService>();
            var container = builder.Build();
            _containers.Add(container);
            return container.Resolve<ISFPurchasesService>();
        }

        private static SFPurchasesConfig Config(string id, params SFPurchaseNode[] nodes)
        {
            var config = new SFPurchasesConfig { Id = id, Purchases = nodes };
            config.BuildTree();
            return config;
        }

        private sealed class MemoryConfigsService : ISFConfigsService
        {
            private readonly ISFConfig[] _configs;
            public MemoryConfigsService(params ISFConfig[] configs) => _configs = configs;
            public IEnumerable<ISFConfig> Configs => _configs;
            public bool TryGetConfigs<T>(out T[] configs) where T : ISFConfig
            {
                configs = _configs.OfType<T>().ToArray();
                return configs.Length != 0;
            }
            public IEnumerable<T> GetConfigs<T>() where T : ISFConfig => _configs.OfType<T>();
            public bool TryGetGlobalConfig<T>(out T config) where T : ISFGlobalConfig
            {
                config = default;
                return false;
            }
            public UniTask Init(CancellationToken cancellationToken) => UniTask.CompletedTask;
            public void Dispose() { }
        }
    }

    public sealed class SFAndroidReceiptParserTests
    {
        private const string GoogleJson = "{\"packageName\":\"com.example.game\",\"productId\":\"coins.small\",\"purchaseToken\":\"purchase-token\"}";
        private static readonly MethodInfo ExtractMethod = typeof(SFPurchaseResult).Assembly
            .GetType("SFramework.Purchases.Runtime.SFAndroidReceiptParser")
            .GetMethod("TryExtract", BindingFlags.Static | BindingFlags.NonPublic);

        [Test]
        public void TryExtract_RawGoogleJson_ReturnsProductTokenAndPackage()
        {
            AssertReceipt(GoogleJson);
        }

        [Test]
        public void TryExtract_GooglePayloadWrapper_ReturnsProductTokenAndPackage()
        {
            AssertReceipt(JsonUtility.ToJson(new PayloadWrapper { json = GoogleJson, signature = "test-signature" }));
        }

        [Test]
        public void TryExtract_UnityReceiptWithWrappedPayload_UnwrapsBothLayers()
        {
            var payload = JsonUtility.ToJson(new PayloadWrapper { json = GoogleJson, signature = "test-signature" });
            var receipt = JsonUtility.ToJson(new UnityReceipt { Store = "GooglePlay", Payload = payload });

            AssertReceipt(receipt);
        }

        [Test]
        public void TryExtract_UnityReceiptWithDirectPayload_UnwrapsGoogleJson()
        {
            AssertReceipt(JsonUtility.ToJson(new UnityReceipt { Store = "GooglePlay", Payload = GoogleJson }));
        }

        [Test]
        public void TryExtract_ApplicationPackageNameAlias_IsAccepted()
        {
            AssertReceipt("{\"applicationPackageName\":\"com.example.game\",\"productId\":\"coins.small\",\"purchaseToken\":\"purchase-token\"}");
        }

        [Test]
        public void TryExtract_BothPackageNames_PrefersPackageName()
        {
            AssertReceipt("{\"packageName\":\"com.example.game\",\"applicationPackageName\":\"other.package\",\"productId\":\"coins.small\",\"purchaseToken\":\"purchase-token\"}");
        }

        [Test]
        public void TryExtract_MissingPackageName_StillAcceptsProductAndToken()
        {
            var success = Extract("{\"productId\":\"coins.small\",\"purchaseToken\":\"purchase-token\"}", out var package, out var product, out var token);

            Assert.That(success, Is.True);
            Assert.That(package, Is.Null.Or.Empty);
            Assert.That(product, Is.EqualTo("coins.small"));
            Assert.That(token, Is.EqualTo("purchase-token"));
        }

        [TestCase("{}")]
        [TestCase("{\"purchaseToken\":\"token\"}")]
        [TestCase("{\"productId\":\"product\"}")]
        [TestCase("{\"productId\":\"\",\"purchaseToken\":\"token\"}")]
        [TestCase("{\"productId\":\"product\",\"purchaseToken\":\"\"}")]
        public void TryExtract_MissingProductOrToken_ReturnsFalse(string receipt)
        {
            Assert.That(Extract(receipt, out _, out _, out _), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-json")]
        public void TryExtract_EmptyOrMalformedInput_ReturnsFalseAndClearsOutputs(string receipt)
        {
            Assert.That(Extract(receipt, out var package, out var product, out var token), Is.False);
            Assert.That(package, Is.Null);
            Assert.That(product, Is.Null);
            Assert.That(token, Is.Null);
        }

        private static void AssertReceipt(string receipt)
        {
            Assert.That(Extract(receipt, out var package, out var product, out var token), Is.True);
            Assert.That(package, Is.EqualTo("com.example.game"));
            Assert.That(product, Is.EqualTo("coins.small"));
            Assert.That(token, Is.EqualTo("purchase-token"));
        }

        private static bool Extract(string receipt, out string package, out string product, out string token)
        {
            // The parser is intentionally internal; exercise its actual implementation without
            // changing the runtime assembly's visibility just for tests.
            var arguments = new object[] { receipt, "stale-package", "stale-product", "stale-token" };
            var success = (bool)ExtractMethod.Invoke(null, arguments);
            package = (string)arguments[1];
            product = (string)arguments[2];
            token = (string)arguments[3];
            return success;
        }

        [Serializable]
        private sealed class PayloadWrapper
        {
            public string json;
            public string signature;
        }

        [Serializable]
        private sealed class UnityReceipt
        {
            public string Store;
            public string Payload;
        }
    }

    public sealed class SFCurrencyUtilsTests
    {
        private CultureInfo _originalCulture;

        [SetUp]
        public void SetUp()
        {
            _originalCulture = CultureInfo.CurrentCulture;
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = _originalCulture;
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void GetCurrencySymbol_BlankCode_ReturnsEmptyString(string code)
        {
            Assert.That(SFCurrencyUtils.GetCurrencySymbol(code), Is.Empty);
        }

        [Test]
        public void GetCurrencySymbol_UnknownCode_ReturnsInvariantUppercase()
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            Assert.That(SFCurrencyUtils.GetCurrencySymbol("x-test-i"), Is.EqualTo("X-TEST-I"));
        }

        [Test]
        public void GetCurrencySymbol_PreferLocalized_UsesMatchingCurrentCultureSymbol()
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            Assert.That(SFCurrencyUtils.GetCurrencySymbol("usd", true), Is.EqualTo("$"));
        }

        [Test]
        public void GetCurrencySymbol_LocalizedLookupIsIndependentOfFallbackCache()
        {
            var fallback = SFCurrencyUtils.GetCurrencySymbol("USD");
            var first = new CultureInfo("en-US");
            first.NumberFormat.CurrencySymbol = "USD first";
            CultureInfo.CurrentCulture = first;
            Assert.That(SFCurrencyUtils.GetCurrencySymbol("USD", true), Is.EqualTo("USD first"));

            var second = new CultureInfo("en-US");
            second.NumberFormat.CurrencySymbol = "USD second";
            CultureInfo.CurrentCulture = second;
            Assert.That(SFCurrencyUtils.GetCurrencySymbol("USD", true), Is.EqualTo("USD second"));
            Assert.That(SFCurrencyUtils.GetCurrencySymbol("USD"), Is.EqualTo(fallback));
        }

        [Test]
        public void GetCurrencySymbol_CurrencyCodeLookupIsCaseInsensitiveAcrossRepeatedCalls()
        {
            var symbol = SFCurrencyUtils.GetCurrencySymbol("EUR");

            Assert.That(symbol, Is.EqualTo("€"));
            Assert.That(SFCurrencyUtils.GetCurrencySymbol("eur"), Is.EqualTo(symbol));
            Assert.That(SFCurrencyUtils.GetCurrencySymbol("EuR"), Is.EqualTo(symbol));
        }

        [Test]
        public void GetCurrencySymbol_NeutralCulture_DoesNotFailLocalizedLookup()
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en");

            Assert.That(SFCurrencyUtils.GetCurrencySymbol("EUR", true), Is.EqualTo("€"));
        }
    }
}
