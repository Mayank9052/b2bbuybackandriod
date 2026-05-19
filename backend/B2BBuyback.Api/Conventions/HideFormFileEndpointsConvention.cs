using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace B2BBuyback.Api.Conventions
{
    /// <summary>
    /// Convention that automatically hides API endpoints with IFormFile parameters from Swagger.
    /// This prevents "Error reading parameter" exceptions in Swagger generation.
    /// </summary>
    public class HideFormFileEndpointsConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            // Check if any parameter is IFormFile or List<IFormFile>
            var hasFormFile = action.Parameters.Any(p =>
                p.ParameterType == typeof(IFormFile) ||
                p.ParameterType == typeof(List<IFormFile>) ||
                (p.ParameterType.IsGenericType &&
                 p.ParameterType.GetGenericTypeDefinition() == typeof(List<>) &&
                 p.ParameterType.GenericTypeArguments[0] == typeof(IFormFile)));

            if (hasFormFile)
            {
                // Hide from Swagger if it has file upload parameters
                action.ApiExplorer.IsVisible = false;
            }
        }
    }
}
