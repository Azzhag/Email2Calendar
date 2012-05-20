// Copyright 2012 Kindel Systems, LLC.
//   
// This file is part of Email2Calendar
//  
// Email2Calendar is free software: you can redistribute it and/or modify it under the 
// terms of the MIT License (http://www.opensource.org/licenses/mit-license.php)
//  
// Official source repository is at https://github.com/tig/Email2Calendar
//  

using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Email2Calendar {
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : HttpApplication {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new {controller = "Home", action = "Index", id = UrlParameter.Optional} // Parameter defaults
                );

            routes.MapRoute(
                "GetProvider", // Route name
                "{controller}/{action}/{address}", // URL with parameters
                new {controller = "Home", action = "GetProvider", address = UrlParameter.Optional} // Parameter defaults
                );

            routes.MapRoute(
                "AddFeedback", // Route name
                "{controller}/{action}/{address}", // URL with parameters
                new {
                    controller = "Home", action = "GetProvider", address = UrlParameter.Optional,
                    provider = UrlParameter.Optional,
                    realProvider = UrlParameter.Optional
                } // Parameter defaults
                );
        }

        protected void Application_Start() {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }
    }
}