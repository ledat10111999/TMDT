using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebSiteBanHang
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            // admin
            routes.MapRoute(
              name: "admin",
              url: "admin",
              defaults: new { controller = "Quyen", action = "Index", id = UrlParameter.Optional }
          );



            // Cấu hình đường dẫn thân thiện cho XemChiTiet
            routes.MapRoute(
                name: "XemChiTiet",
                url: "{tensp}-{id}",
                defaults: new { controller = "SanPham", action = "XemChiTiet", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
