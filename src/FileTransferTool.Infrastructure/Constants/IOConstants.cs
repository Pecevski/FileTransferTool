using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransferTool.Infrastructure.Constants
{
    /// <summary>
    /// Shared I/O tuning constants.
    /// </summary>
    public static class IOConstants
    {
        // 80 KB matches .NET's default internal buffer and is good for large-file streaming.
        public const int FileStreamBufferSize = 81920;
    }
}
