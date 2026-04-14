namespace WebReport.Controllers.API.DTOs
{
    /// <summary>
    /// Represents a HATEOAS link for resource navigation
    /// </summary>
    public class LinkDto
    {
        /// <summary>
        /// The URL of the link
        /// </summary>
        /// <example>https://api.example.com/api/users/1</example>
        public string Href { get; set; } = string.Empty;

        /// <summary>
        /// The relationship type of the link
        /// </summary>
        /// <example>self</example>
        public string Rel { get; set; } = string.Empty;

        /// <summary>
        /// The HTTP method for the link
        /// </summary>
        /// <example>GET</example>
        public string Method { get; set; } = string.Empty;

        public LinkDto() { }

        public LinkDto(string href, string rel, string method)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }
    }
}
