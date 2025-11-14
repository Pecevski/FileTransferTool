namespace FileTransferTool.Domain.Enums
{
    /// <summary>
    /// Enum representing block transfer operation states.
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
