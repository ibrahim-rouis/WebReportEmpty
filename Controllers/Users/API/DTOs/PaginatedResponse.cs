namespace WebReport.Controllers.API.DTOs
{
    /// <summary>
    /// Pagination metadata for paginated responses
    /// </summary>
    public class PaginationMetadata
    {
        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        /// <example>1</example>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        /// <example>5</example>
        public int TotalPages { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        /// <example>10</example>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        /// <example>50</example>
        public int TotalCount { get; set; }

        /// <summary>
        /// Indicates if there is a previous page
        /// </summary>
        /// <example>false</example>
        public bool HasPrevious { get; set; }

        /// <summary>
        /// Indicates if there is a next page
        /// </summary>
        /// <example>true</example>
        public bool HasNext { get; set; }
    }

    /// <summary>
    /// Paginated response wrapper with HATEOAS links
    /// </summary>
    /// <typeparam name="T">The type of items in the response</typeparam>
    public class PaginatedResponse<T>
    {
        /// <summary>
        /// The collection of items for the current page
        /// </summary>
        public IEnumerable<T> Items { get; set; } = [];

        /// <summary>
        /// Pagination metadata
        /// </summary>
        public PaginationMetadata Pagination { get; set; } = new();

        /// <summary>
        /// HATEOAS navigation links
        /// </summary>
        public List<LinkDto> Links { get; set; } = [];
    }
}
