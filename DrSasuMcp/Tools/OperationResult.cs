using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools
{
    /// <summary>
    /// Represents the result of the operation, including success status, error message, number of rows affected, and any returned data.
    /// </summary>
    public class OperationResult(bool success, string? error = null, int? rowsAffected = null, object? data = null)
    {
        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; } = success;

        /// <summary>
        /// Gets the error message if the operation failed; otherwise, null.
        /// </summary>
        public string? Error { get; } = error;

        /// <summary>
        /// Gets the number of rows affected by the operation, if applicable.
        /// </summary>
        public int? RowsAffected { get; } = rowsAffected;

        /// <summary>
        /// Gets any data returned by the operation, such as query results.
        /// </summary>
        public object? Data { get; } = data;
    }
}
