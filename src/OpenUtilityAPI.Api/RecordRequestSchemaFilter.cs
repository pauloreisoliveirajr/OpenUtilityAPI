using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OpenUtilityAPI.Api;

/// <summary>Include positional record request metadata in OpenAPI schemas.</summary>
public sealed class RecordRequestSchemaFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema objectSchema || objectSchema.Properties is null ||
            context.Type.Namespace != typeof(RecordRequestSchemaFilter).Namespace ||
            !context.Type.Name.EndsWith("Request", StringComparison.Ordinal))
            return;

        var constructor = context.Type.GetConstructors().Single();
        foreach (var parameter in constructor.GetParameters())
        {
            var property = context.Type.GetProperty(parameter.Name!);
            var name = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!);
            if (property is null || !objectSchema.Properties.ContainsKey(name))
                continue;

            var propertySchema = context.SchemaGenerator.GenerateSchema(
                parameter.ParameterType, context.SchemaRepository, parameterInfo: parameter);
            // Required constructor inputs have examples, while omitted optional inputs use defaults.
            if (!parameter.HasDefaultValue && propertySchema is OpenApiSchema inputSchema)
            {
                inputSchema.Example = inputSchema.Default;
                inputSchema.Default = null;
            }
            if (property.GetCustomAttribute<JsonRequiredAttribute>() is not null ||
                (!parameter.HasDefaultValue && parameter.GetCustomAttribute<RequiredAttribute>() is not null))
            {
                objectSchema.Required ??= new HashSet<string>();
                objectSchema.Required.Add(name);
            }

            objectSchema.Properties[name] = propertySchema;
        }
    }
}
