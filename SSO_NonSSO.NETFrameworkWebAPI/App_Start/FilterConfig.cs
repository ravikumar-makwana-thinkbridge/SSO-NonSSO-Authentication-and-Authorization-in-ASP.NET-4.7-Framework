using System.Web;
using System.Web.Mvc;

namespace SSO_NonSSO.NETFrameworkWebAPI
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
