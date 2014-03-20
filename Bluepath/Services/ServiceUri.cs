namespace Bluepath.Services
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    [DataContract]
    public class ServiceUri
    {
        // TODO: Maybe we could serialize just the type of binding and then
        // create new object based on serialized type
        [DataMember]
        public BindingType BindingType { get; set; }

        [IgnoreDataMember]
        public Binding Binding
        {
            get
            {
                switch (this.BindingType)
                {
                    case Services.BindingType.BasicHttpBinding:
                        return new BasicHttpBinding();
                    case Services.BindingType.BasicHttpContextBinding:
                        return new BasicHttpContextBinding();
                    case Services.BindingType.BasicHttpsBinding:
                        return new BasicHttpsBinding();
                }

                return null;
            }

            set
            {
                if (value is BasicHttpBinding)
                {
                    this.BindingType = Services.BindingType.BasicHttpBinding;
                }

                if (value is BasicHttpContextBinding)
                {
                    this.BindingType = Services.BindingType.BasicHttpContextBinding;
                }

                if (value is BasicHttpsBinding)
                {
                    this.BindingType = Services.BindingType.BasicHttpsBinding;
                }
            }
        }

        [DataMember]
        public string Address { get; set; }

        public static ServiceUri FromEndpointAddress(EndpointAddress endpointAddress, Binding binding)
        {
            return new ServiceUri() { Address = endpointAddress.ToString() };
        }

        public EndpointAddress ToEndpointAddress()
        {
            return new EndpointAddress(this.Address);
        }

        public override bool Equals(object obj)
        {
            if(obj is ServiceUri)
            {
                var uri = obj as ServiceUri;
                return this.Address == uri.Address && this.BindingType == uri.BindingType;
            }

            return base.Equals(obj);
        }
    }
}
