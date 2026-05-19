using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace B2BBuyback.Api.Filters
{
    /// <summary>
    /// Operation filter to handle file upload endpoints with [FromForm] and IFormFile parameters.
    /// Converts parameter documentation to proper multipart/form-data schema.
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var formFileParams = context.ApiDescription.ActionDescriptor.Parameters
                .Where(p => p.ParameterType == typeof(IFormFile) ||
                           (p.ParameterType.IsGenericType &&
                            p.ParameterType.GetGenericTypeDefinition() == typeof(List<>) &&
                            p.ParameterType.GenericTypeArguments[0] == typeof(IFormFile)))
                .ToList();

            if (!formFileParams.Any())
                return;

            // Build schema for the request body
            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>(),
                Required = new HashSet<string>()
            };

            // Add file parameter(s)
            foreach (var param in formFileParams)
            {
                var paramName = param.Name;
                schema.Properties[paramName] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = $"File to upload ({paramName})"
                };
                
                schema.Required.Add(paramName);
            }

            // Add other non-file form parameters
            var otherFormParams = context.ApiDescription.ActionDescriptor.Parameters
                .Where(p => p.ParameterType != typeof(IFormFile) &&
                           (p.ParameterType.IsGenericType == false ||
                            p.ParameterType.GetGenericTypeDefinition() != typeof(List<>) ||
                            p.ParameterType.GenericTypeArguments[0] != typeof(IFormFile)))
                .ToList();

            foreach (var param in otherFormParams)
            {
                var paramName = param.Name;
                var schema_param = new OpenApiSchema
                {
                    Type = GetOpenApiType(param.ParameterType),
                    Format = GetOpenApiFormat(param.ParameterType)
                };

                schema.Properties[paramName] = schema_param;
            }

            // Set request body
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            };

            // Remove file parameters from operation parameters
            operation.Parameters = operation.Parameters
                .Where(p => !formFileParams.Any(fp => fp.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private string GetOpenApiType(Type type)
        {
            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                return "integer";
            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return "string";
            
            return "string";
        }

        private string GetOpenApiFormat(Type type)
        {
            if (type == typeof(int) || type == typeof(short))
                return "int32";
            if (type == typeof(long))
                return "int64";
            if (type == typeof(float))
                return "float";
            if (type == typeof(double))
                return "double";
            if (type == typeof(DateTime))
                return "date-time";
            
            return null;
        }
    }
}
