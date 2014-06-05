using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Web.Configuration;
using CODECorp.WcfIdentity.ServiceContract;

namespace CODECorp.WcfIdentity.Web.ServiceChannel
{
    public static class ServiceFactory
    {
        private static readonly string BaseServiceAddress;
        private static readonly Int32 BindingType;

        private const string ServicePathString = "{0}.svc";

        static ServiceFactory()
        {
            ServiceFactory.BaseServiceAddress = WebConfigurationManager.AppSettings["BaseServiceAddress"];
            ServiceFactory.BindingType = Convert.ToInt32(WebConfigurationManager.AppSettings["BindingType"]);
        }

        /// <summary>
        /// Does a really straightforward 1 to 1 mapping of IServiceInterface to ServiceInterface, basically assumes that your service and interface names are identical
        /// </summary>
        /// <param name="Type">Accepts any type</param>
        /// <returns>Type Name minus first character</returns>
        private static string GetInterfaceName(Type Type)
        {
            return Type.Name.Substring(1, Type.Name.Length - 1);
        }

        /// <summary>
        /// Create an instance of IService binding
        /// </summary>
        /// <typeparam name="T">Any IService</typeparam>
        /// <returns>Active binding instance</returns>
        public static T Create<T>() where T : IService
        {
            ServiceBindingManager manager = new ServiceBindingManager(new Uri(ServiceFactory.BaseServiceAddress), (ServiceBindingManager.BindingTypeEnum)ServiceFactory.BindingType);
            string servicePath = string.Format(ServicePathString, ServiceFactory.GetInterfaceName(typeof(T)));
            return manager.Create<T>(servicePath);
        }


    }


}