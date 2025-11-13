namespace FileTransferTool.Domain.Enums
{
    public enum BlockTransferStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        VerificationFailed,
        Retrying
    }
}
