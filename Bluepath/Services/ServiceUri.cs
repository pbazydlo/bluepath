namespace Bluepath.Services
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    [DataContract]
    public class ServiceUri
    {
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:StaticReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Public property starting with upper-case letter is exposed.")]
        // ReSharper disable once InconsistentNaming
        private static Binding serviceBinding;

        public static Binding ServiceBinding
        {
            get
            {
                return ServiceUri.serviceBinding;
            }
            set
            {
                ServiceUri.serviceBinding = value;
            }
        }

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
