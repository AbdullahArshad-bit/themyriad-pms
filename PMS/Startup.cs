using Microsoft.Owin;
using Owin;
using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Description;
using PMS.Filters;

[assembly: OwinStartupAttribute(typeof(PMS.Startup))]
namespace PMS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        // Swagger Schema Filter - Removes virtual/navigation properties that cause circular references
        public class ApplyCustomSchemaFilters : ISchemaFilter
        {
            public void Apply(Schema schema, SchemaRegistry schemaRegistry, Type type)
            {
                // Remove virtual/navigation properties that cause circular references
                if (schema?.properties == null) return;

                var propsToRemove = schema.properties
                    .Where(p => type.GetProperty(p.Key)?.GetGetMethod()?.IsVirtual == true &&
                               !type.GetProperty(p.Key).GetGetMethod().IsFinal)
                    .Select(p => p.Key)
                    .ToList();

                foreach (var prop in propsToRemove)
                {
                    schema.properties.Remove(prop);
                }
            }
        }

        // Swagger Operation Filter - Adds Authorization header parameter for endpoints with BasicAuthenticationApi attribute
        public class AddAuthorizationHeaderOperationFilter : IOperationFilter
        {
            public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
            {
                var hasBasicAuth = apiDescription.ActionDescriptor.GetCustomAttributes<BasicAuthenticationApiAttribute>().Any() ||
                                  apiDescription.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<BasicAuthenticationApiAttribute>().Any();
                var hasStudentBearer = apiDescription.ActionDescriptor.GetCustomAttributes<StudentBearerAuthorizeAttribute>().Any() ||
                                       apiDescription.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<StudentBearerAuthorizeAttribute>().Any();

                if (hasBasicAuth || hasStudentBearer)
                {
                    if (operation.parameters == null)
                        operation.parameters = new List<Parameter>();

                    operation.parameters.Add(new Parameter
                    {
                        name = "Authorization",
                        @in = "header",
                        description = hasStudentBearer
                            ? "Student token from POST /api/Student/Auth/Login. Paste Data.token only (no quotes), or use Bearer <token>. Alternative header: X-Student-Token."
                            : "Basic Authentication. Enter the base64 encoded token (e.g., dGhlbXlyaWFkOkabcdefghijklmnop) or use format: Basic <base64-token>",
                        required = true,
                        type = "string"
                    });
                }
            }
        }
    }
}
