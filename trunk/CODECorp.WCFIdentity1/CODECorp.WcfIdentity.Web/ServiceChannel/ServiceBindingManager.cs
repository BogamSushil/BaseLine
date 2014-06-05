using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;
using System.Web.Services.Description;
using CODECorp.WcfIdentity.ServiceContract;

namespace CODECorp.WcfIdentity.Web.ServiceChannel
{
    public class ServiceBindingManager
    {
        private readonly Uri _BaseAddress;
        private readonly BindingTypeEnum _BindingType;

        public enum BindingTypeEnum
        {
            BasicHttp = 0,
            NetTcp
        }

        public ServiceBindingManager() { }
        public ServiceBindingManager(Uri BaseAddress, BindingTypeEnum BindingType)
        {
            this._BaseAddress = BaseAddress;
            this._BindingType = BindingType;
        }

        public T Create<T>(string ServicePath) where T : IService
        {
            if (typeof(T).IsInterface)
            {
                System.ServiceModel.Channels.Binding binding = BuildBinding(this._BindingType);
                ChannelFactory<T> channelFactory = new ChannelFactory<T>(binding, new EndpointAddress(BuildEndpointAddress(this._BaseAddress, ServicePath)));
                T instance = channelFactory.CreateChannel();
                return instance;
            }
            else
            {
                throw new ChannelFactoryException("Type provided must be an interface");
            }
        }

        private static System.ServiceModel.Channels.Binding BuildBinding(BindingTypeEnum BindingType)
        {
            switch (BindingType)
            {
                case BindingTypeEnum.BasicHttp:
                    return new BasicHttpBinding();
                case BindingTypeEnum.NetTcp:
                    NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
                    binding.MaxReceivedMessageSize = int.MaxValue;
                    return binding;
                default:
                    throw new NotImplementedException();
            }
        }

        private static string BuildEndpointAddress(Uri BaseAddress, string ServicePath)
        {
            string uri = new Uri(BaseAddress, ServicePath).ToString();
            return uri;
        }
    }

    public class ChannelFactoryException : Exception
    {
        public ChannelFactoryException() { }
        public ChannelFactoryException(string Message) : base(Message) { }
        public ChannelFactoryException(string Message, Exception InnerException) : base(Message, InnerException) { }
    }
}