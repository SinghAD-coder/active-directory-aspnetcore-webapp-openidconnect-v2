using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
//using Swashbuckle.Swagger;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
//using System.Web.Http.Description;

namespace TodoListService
{
    /// <summary>
    /// BasePath Document Filter sets BasePath property of Swagger and removes it from the individual URL paths
    /// </summary>
    public class BasePathFilter : Swashbuckle.AspNetCore.SwaggerGen.IDocumentFilter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="basePath">BasePath to remove from Operations</param>
        public BasePathFilter(string basePath)
        {
            BasePath = basePath;
        }

        /// <summary>
        /// Gets the BasePath of the Swagger Doc
        /// </summary>
        /// <returns>The BasePath of the Swagger Doc</returns>
        public string BasePath { get; }
        
        //public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        //{
        //    swaggerDoc.host = "api.mavimcloud.com";
        //    swaggerDoc.basePath = "v2";
        //    swaggerDoc.schemes = new string[] { "http", "https" };
        //}

        ///// <summary>
        ///// Apply the filter
        ///// </summary>
        ///// <param name="swaggerDoc">OpenApiDocument</param>
        ///// <param name="context">FilterContext</param>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            //swaggerDoc.SecurityRequirements = new List<OpenApiSecurityRequirement> { new OpenApiSecurityRequirement {  } };
            //swaggerDoc.Servers.Add(new OpenApiServer() { Url = this.BasePath });
            swaggerDoc.Servers.Add(new OpenApiServer() { Url = "https://api.mavimcloud.clom" + this.BasePath });

            var pathsToModify = swaggerDoc.Paths.Where(p => p.Key.StartsWith(this.BasePath)).ToList();

            foreach (var path in pathsToModify)
            {
                if (path.Key.StartsWith(this.BasePath))
                {
                    string newKey = Regex.Replace(path.Key, $"^{this.BasePath}", string.Empty);
                    swaggerDoc.Paths.Remove(path.Key);
                    swaggerDoc.Paths.Add(newKey, path.Value);
                }
            }
        }

    }
}
