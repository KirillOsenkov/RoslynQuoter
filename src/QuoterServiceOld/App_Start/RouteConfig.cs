using System.Web.Mvc;
using System.Web.Routing;

namespace QuoterServiceOld
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute(""); // this is needed for the default document index.html to work
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Quoter", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
