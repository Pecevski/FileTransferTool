namespace FileTransferTool.Domain.Enums
{
    /// <summary>
    /// Enum for block transfer operations states.
    /// </summary>
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
