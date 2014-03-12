namespace Bluepath.Services
{
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    [DataContract]
    public class ServiceUri
    {
        public static Binding ServiceBinding;

        [DataMember]
        public string Address { get; set; }

        public static ServiceUri FromEndpointAddress(EndpointAddress endpointAddress)
        {
            return new ServiceUri() { Address = endpointAddress.ToString() };
        }

        public EndpointAddress ToEndpointAddress()
        {
            return new EndpointAddress(this.Address);
        }
    }
}
