﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34011
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Bluepath.CentralizedDiscovery.ServiceReferences
{
    using System.Runtime.Serialization;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ServiceUri", Namespace="http://schemas.datacontract.org/2004/07/Bluepath.Services")]
    public partial class ServiceUri : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private string AddressField;
        
        private Bluepath.CentralizedDiscovery.ServiceReferences.BindingType BindingTypeField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get
            {
                return this.extensionDataField;
            }
            set
            {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Address
        {
            get
            {
                return this.AddressField;
            }
            set
            {
                this.AddressField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public Bluepath.CentralizedDiscovery.ServiceReferences.BindingType BindingType
        {
            get
            {
                return this.BindingTypeField;
            }
            set
            {
                this.BindingTypeField = value;
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="BindingType", Namespace="http://schemas.datacontract.org/2004/07/Bluepath.Services")]
    public enum BindingType : int
    {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        BasicHttpBinding = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        BasicHttpContextBinding = 1,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        BasicHttpsBinding = 2,
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="Bluepath.CentralizedDiscovery.ServiceReferences.ICentralizedDiscoveryService")]
    public interface ICentralizedDiscoveryService
    {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICentralizedDiscoveryService/GetAvailableServices", ReplyAction="http://tempuri.org/ICentralizedDiscoveryService/GetAvailableServicesResponse")]
        Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri[] GetAvailableServices();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICentralizedDiscoveryService/GetAvailableServices", ReplyAction="http://tempuri.org/ICentralizedDiscoveryService/GetAvailableServicesResponse")]
        System.Threading.Tasks.Task<Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri[]> GetAvailableServicesAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICentralizedDiscoveryService/Register", ReplyAction="http://tempuri.org/ICentralizedDiscoveryService/RegisterResponse")]
        void Register(Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri uri);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICentralizedDiscoveryService/Register", ReplyAction="http://tempuri.org/ICentralizedDiscoveryService/RegisterResponse")]
        System.Threading.Tasks.Task RegisterAsync(Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri uri);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICentralizedDiscoveryService/Unregister", ReplyAction="http://tempuri.org/ICentralizedDiscoveryService/UnregisterResponse")]
        void Unregister(Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri uri);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICentralizedDiscoveryService/Unregister", ReplyAction="http://tempuri.org/ICentralizedDiscoveryService/UnregisterResponse")]
        System.Threading.Tasks.Task UnregisterAsync(Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri uri);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ICentralizedDiscoveryServiceChannel : Bluepath.CentralizedDiscovery.ServiceReferences.ICentralizedDiscoveryService, System.ServiceModel.IClientChannel
    {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class CentralizedDiscoveryServiceClient : System.ServiceModel.ClientBase<Bluepath.CentralizedDiscovery.ServiceReferences.ICentralizedDiscoveryService>, Bluepath.CentralizedDiscovery.ServiceReferences.ICentralizedDiscoveryService
    {
        
        public CentralizedDiscoveryServiceClient()
        {
        }
        
        public CentralizedDiscoveryServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName)
        {
        }
        
        public CentralizedDiscoveryServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress)
        {
        }
        
        public CentralizedDiscoveryServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress)
        {
        }
        
        public CentralizedDiscoveryServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }
        
        public Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri[] GetAvailableServices()
        {
            return base.Channel.GetAvailableServices();
        }
        
        public System.Threading.Tasks.Task<Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri[]> GetAvailableServicesAsync()
        {
            return base.Channel.GetAvailableServicesAsync();
        }
        
        public void Register(Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri uri)
        {
            base.Channel.Register(uri);
        }
        
        public System.Threading.Tasks.Task RegisterAsync(Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri uri)
        {
            return base.Channel.RegisterAsync(uri);
        }
        
        public void Unregister(Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri uri)
        {
            base.Channel.Unregister(uri);
        }
        
        public System.Threading.Tasks.Task UnregisterAsync(Bluepath.CentralizedDiscovery.ServiceReferences.ServiceUri uri)
        {
            return base.Channel.UnregisterAsync(uri);
        }
    }
}
