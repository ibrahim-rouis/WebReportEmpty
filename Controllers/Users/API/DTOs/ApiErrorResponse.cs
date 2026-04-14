namespace WebReport.Controllers.API.DTOs
{
    /// <summary>
    /// Standard error response for API errors
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// Error type identifier
        /// </summary>
        /// <example>NotFound</example>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable error title
        /// </summary>
        /// <example>Resource Not Found</example>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// HTTP status code
        /// </summary>
        /// <example>404</example>
        public int Status { get; set; }

        /// <summary>
        /// Detailed error message
        /// </summary>
        /// <example>The user with ID 123 was not found.</example>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Request trace identifier for debugging
        /// </summary>
        /// <example>00-abc123-def456-00</example>
        public string? TraceId { get; set; }

        /// <summary>
        /// Validation errors (for 400 Bad Request responses)
        /// </summary>
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
