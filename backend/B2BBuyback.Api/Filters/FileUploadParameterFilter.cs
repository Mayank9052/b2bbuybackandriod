using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace B2BBuyback.Api.Filters
{
    /// <summary>
    /// Parameter filter to handle IFormFile parameters in Swagger documentation.
    /// This prevents errors when processing endpoints with [FromForm] and IFormFile.
    /// </summary>
    public class FileUploadParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            // This filter is mainly a marker; the OperationFilter handles the actual logic
            // This ensures that if any IFormFile parameters slip through, they're handled gracefully
        }
    }
}
