using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using IRouteHandler = SmartMap.Web.Routers.IRouteHandler;

namespace SmartMap.Web.Routers
{
    public class CmsTransformer : DynamicRouteValueTransformer
    {
        private readonly IRouteHandler _routeHandler;

        public CmsTransformer(IRouteHandler routeHandler)
        {
            _routeHandler = routeHandler;
        }

        public override async ValueTask<RouteValueDictionary> TransformAsync(
            HttpContext httpContext, 
            RouteValueDictionary values)
        {
            return await _routeHandler.GetRouteValue(values);
        }
    }
}
