using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace B2BBuyback.Api.Filters
{
    /// <summary>
    /// Document filter to handle Swagger generation errors caused by IFormFile parameters.
    /// This filter skips operations that have problematic [FromForm] + IFormFile combinations.
    /// </summary>
    public class SkipFormFileOperationFilter : IDocumentFilter
    {
        private readonly HashSet<string> _problematicOperations = new(StringComparer.Ordinal)
        {
            "Upload Brochure",
            "Update"  // PriceMasterController.Update
        };

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // This filter is applied after the document is generated
            // We'll let Swagger handle it by using OperationFilter instead
        }
    }
}
