namespace SFramework.Purchases.Runtime
{
    public readonly struct SFPurchaseResult
    {
        public readonly SFPurchaseStatus Status;
        public readonly string FullId;
        public readonly string StoreId;
        public readonly string TransactionId;
        public readonly string Receipt;
        public readonly string FailureReason;

        public bool Success => Status == SFPurchaseStatus.Confirmed;

        public SFPurchaseResult(SFPurchaseStatus status, string fullId, string storeId,
            string transactionId, string receipt, string failureReason = null)
        {
            Status = status;
            FullId = fullId;
            StoreId = storeId;
            TransactionId = transactionId;
            Receipt = receipt;
            FailureReason = failureReason;
        }
    }
}