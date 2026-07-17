using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using Swashbuckle.Application;
using Swashbuckle.Swagger;
using WebActivatorEx;
using PMSAPI.Classes;

[assembly: PreApplicationStartMethod(typeof(PMSAPI.App_Start.SwaggerConfig), "Register")]

namespace PMSAPI.App_Start
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "Odyssey Integration API");
                    c.UseFullTypeNameInSchemaIds();
                    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                    c.IgnoreObsoleteActions();
                    c.IgnoreObsoleteProperties();
                    c.SchemaFilter<ApplyCustomSchemaFilters>();
                    c.ApiKey("Authorization")
                        .Description("JWT Bearer token. Format: Bearer {token}")
                        .Name("Authorization")
                        .In("header");
                    c.OperationFilter<AddAuthorizationHeaderOperationFilter>();
                })
                .EnableSwaggerUi(c =>
                {
                    c.DocExpansion(DocExpansion.List);
                    c.DisableValidator();
                    c.EnableApiKeySupport("Authorization", "header");
                });
        }
    }

    public class ApplyCustomSchemaFilters : ISchemaFilter
    {
        public void Apply(Schema schema, SchemaRegistry schemaRegistry, System.Type type)
        {
            if (schema?.properties == null)
            {
                return;
            }

            var propsToRemove = schema.properties
                .Where(p =>
                {
                    var prop = type.GetProperty(p.Key);
                    var getter = prop?.GetGetMethod();
                    return getter != null && getter.IsVirtual && !getter.IsFinal;
                })
                .Select(p => p.Key)
                .ToList();

            foreach (var prop in propsToRemove)
            {
                schema.properties.Remove(prop);
            }
        }
    }

    public class AddAuthorizationHeaderOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var hasApiAuth =
                apiDescription.ActionDescriptor.GetCustomAttributes<ApiAuthorize>().Any() ||
                apiDescription.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<ApiAuthorize>().Any();

            if (!hasApiAuth)
            {
                return;
            }

            if (operation.parameters == null)
            {
                operation.parameters = new System.Collections.Generic.List<Parameter>();
            }

            operation.parameters.Add(new Parameter
            {
                name = "Authorization",
                @in = "header",
                description = "JWT Bearer token. Example: Bearer eyJhbGciOi...",
                required = true,
                type = "string"
            });
        }
    }
}
