using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

namespace Lse.API.Swagger
{
    // Registers a Swagger document for each discovered API version
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var desc in _provider.ApiVersionDescriptions)
            {
                var info = new OpenApiInfo
                {
                    Title = "LSE Trading API",
                    Version = desc.ApiVersion.ToString(),
                    Description = "LSE Trading API - average price service"
                };

                if (desc.IsDeprecated)
                {
                    info.Description += " This API version has been deprecated.";
                }

                // Add document entry directly to SwaggerGeneratorOptions
                options.SwaggerGeneratorOptions.SwaggerDocs[desc.GroupName] = info;
            }
        }
    }
}
