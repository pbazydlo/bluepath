using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Services
{
    [DataContract]
    public class ServiceUri
    {
        public static Binding ServiceBinding;

        [DataMember]
        public EndpointAddressAugust2004 Address { get; set; }

        public void SetAddress(EndpointAddress endpointAddress)
        {
            this.Address = EndpointAddressAugust2004.FromEndpointAddress(endpointAddress);
        }
    }
}
