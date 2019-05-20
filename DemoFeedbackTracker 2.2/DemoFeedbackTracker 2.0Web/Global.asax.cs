using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace DemoFeedbackTracker_2._0Web
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
          
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            switch (Request.Url.Scheme)
            {
                case "https":
                    Response.AddHeader("Strict-Transport-Security", "max-age=300");
                    break;
                case "http":
                    var path = "https://" + Request.Url.Host + Request.Url.PathAndQuery;
                    Response.Status = "301 Moved Permanently";
                    Response.AddHeader("Location", path);
                    break;
            }
        }
    }

    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}