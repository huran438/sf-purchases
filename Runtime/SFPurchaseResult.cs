namespace SFramework.Purchases.Runtime
{
    public readonly struct SFPurchaseResult
    {
        private readonly bool _initialized;
        public readonly SFPurchaseStatus Status;
        public readonly string FullId;
        public readonly string StoreId;
        public readonly string TransactionId;
        public readonly string Receipt;
        public readonly string FailureReason;

        public bool Success => _initialized && Status == SFPurchaseStatus.Confirmed;

        public SFPurchaseResult(SFPurchaseStatus status, string fullId, string storeId,
            string transactionId, string receipt, string failureReason = null)
        {
            _initialized = true;
            Status = status;
            FullId = fullId;
            StoreId = storeId;
            TransactionId = transactionId;
            Receipt = receipt;
            FailureReason = failureReason;
        }
    }
}
