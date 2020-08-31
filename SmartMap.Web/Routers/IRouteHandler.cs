using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace SmartMap.Web.Routers
{
    public interface IRouteHandler
    {
        Task<RouteValueDictionary> GetRouteValue(RouteValueDictionary values);
    }
}
