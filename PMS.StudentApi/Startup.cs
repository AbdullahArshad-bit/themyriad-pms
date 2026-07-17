using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Description;
using PMS.StudentApi.Classes;

namespace PMS.StudentApi
{
    public class Startup
    {
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

        // Swagger Operation Filter - Adds Authorization header parameter for endpoints with ApiAuthorize attribute
        public class AddAuthorizationHeaderOperationFilter : IOperationFilter
        {
            public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
            {
                // Check if the controller/action has ApiAuthorize attribute
                var hasApiAuth = apiDescription.ActionDescriptor.GetCustomAttributes<ApiAuthorize>().Any() ||
                                apiDescription.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<ApiAuthorize>().Any();

                if (hasApiAuth)
                {
                    if (operation.parameters == null)
                        operation.parameters = new List<Parameter>();

                    operation.parameters.Add(new Parameter
                    {
                        name = "Authorization",
                        @in = "header",
                        description = "JWT Token Authentication. Enter the JWT token (Bearer token format: Bearer <token>)",
                        required = true,
                        type = "string"
                    });
                }
            }
        }
    }
}
