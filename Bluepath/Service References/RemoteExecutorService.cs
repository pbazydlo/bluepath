﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34011
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Bluepath.ServiceReferences
{
    using System.Runtime.Serialization;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ExecutorState", Namespace="http://schemas.datacontract.org/2004/07/Bluepath.Executor")]
    public enum ExecutorState : int
    {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        NotStarted = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Running = 1,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Finished = 2,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Faulted = 3,
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ServiceUri", Namespace="http://schemas.datacontract.org/2004/07/Bluepath.Services")]
    public partial class ServiceUri : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private string AddressField;
        
        private Bluepath.ServiceReferences.BindingType BindingTypeField;
        
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
        public Bluepath.ServiceReferences.BindingType BindingType
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
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="RemoteExecutorServiceResult", Namespace="http://schemas.datacontract.org/2004/07/Bluepath.Services")]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(System.SystemException))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(System.ArgumentException))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(Bluepath.ServiceReferences.ExecutorState))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(object[]))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(System.Exception))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(Bluepath.ServiceReferences.ServiceUri))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(Bluepath.ServiceReferences.BindingType))]
    public partial class RemoteExecutorServiceResult : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private System.Nullable<System.TimeSpan> ElapsedTimeField;
        
        private System.Exception ErrorField;
        
        private Bluepath.ServiceReferences.ExecutorState ExecutorStateField;
        
        private object ResultField;
        
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
        public System.Nullable<System.TimeSpan> ElapsedTime
        {
            get
            {
                return this.ElapsedTimeField;
            }
            set
            {
                this.ElapsedTimeField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Exception Error
        {
            get
            {
                return this.ErrorField;
            }
            set
            {
                this.ErrorField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public Bluepath.ServiceReferences.ExecutorState ExecutorState
        {
            get
            {
                return this.ExecutorStateField;
            }
            set
            {
                this.ExecutorStateField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public object Result
        {
            get
            {
                return this.ResultField;
            }
            set
            {
                this.ResultField = value;
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="Bluepath.ServiceReferences.IRemoteExecutorService")]
    public interface IRemoteExecutorService
    {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/Initialize", ReplyAction="http://tempuri.org/IRemoteExecutorService/InitializeResponse")]
        System.Guid Initialize(byte[] methodHandle);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/Initialize", ReplyAction="http://tempuri.org/IRemoteExecutorService/InitializeResponse")]
        System.Threading.Tasks.Task<System.Guid> InitializeAsync(byte[] methodHandle);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/Execute", ReplyAction="http://tempuri.org/IRemoteExecutorService/ExecuteResponse")]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Bluepath.ServiceReferences.ExecutorState))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(object[]))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(System.Exception))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(System.ArgumentException))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(System.SystemException))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Bluepath.ServiceReferences.ServiceUri))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Bluepath.ServiceReferences.BindingType))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Bluepath.ServiceReferences.RemoteExecutorServiceResult))]
        void Execute(System.Guid eId, object[] parameters, Bluepath.ServiceReferences.ServiceUri callbackUri);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/Execute", ReplyAction="http://tempuri.org/IRemoteExecutorService/ExecuteResponse")]
        System.Threading.Tasks.Task ExecuteAsync(System.Guid eId, object[] parameters, Bluepath.ServiceReferences.ServiceUri callbackUri);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/ExecuteCallback", ReplyAction="http://tempuri.org/IRemoteExecutorService/ExecuteCallbackResponse")]
        void ExecuteCallback(System.Guid eId, Bluepath.ServiceReferences.RemoteExecutorServiceResult executeResult);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/ExecuteCallback", ReplyAction="http://tempuri.org/IRemoteExecutorService/ExecuteCallbackResponse")]
        System.Threading.Tasks.Task ExecuteCallbackAsync(System.Guid eId, Bluepath.ServiceReferences.RemoteExecutorServiceResult executeResult);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/TryJoin", ReplyAction="http://tempuri.org/IRemoteExecutorService/TryJoinResponse")]
        Bluepath.ServiceReferences.RemoteExecutorServiceResult TryJoin(System.Guid eId);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/TryJoin", ReplyAction="http://tempuri.org/IRemoteExecutorService/TryJoinResponse")]
        System.Threading.Tasks.Task<Bluepath.ServiceReferences.RemoteExecutorServiceResult> TryJoinAsync(System.Guid eId);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IRemoteExecutorServiceChannel : Bluepath.ServiceReferences.IRemoteExecutorService, System.ServiceModel.IClientChannel
    {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class RemoteExecutorServiceClient : System.ServiceModel.ClientBase<Bluepath.ServiceReferences.IRemoteExecutorService>, Bluepath.ServiceReferences.IRemoteExecutorService
    {
        
        public RemoteExecutorServiceClient()
        {
        }
        
        public RemoteExecutorServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName)
        {
        }
        
        public RemoteExecutorServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress)
        {
        }
        
        public RemoteExecutorServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress)
        {
        }
        
        public RemoteExecutorServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }
        
        public System.Guid Initialize(byte[] methodHandle)
        {
            return base.Channel.Initialize(methodHandle);
        }
        
        public System.Threading.Tasks.Task<System.Guid> InitializeAsync(byte[] methodHandle)
        {
            return base.Channel.InitializeAsync(methodHandle);
        }
        
        public void Execute(System.Guid eId, object[] parameters, Bluepath.ServiceReferences.ServiceUri callbackUri)
        {
            base.Channel.Execute(eId, parameters, callbackUri);
        }
        
        public System.Threading.Tasks.Task ExecuteAsync(System.Guid eId, object[] parameters, Bluepath.ServiceReferences.ServiceUri callbackUri)
        {
            return base.Channel.ExecuteAsync(eId, parameters, callbackUri);
        }
        
        public void ExecuteCallback(System.Guid eId, Bluepath.ServiceReferences.RemoteExecutorServiceResult executeResult)
        {
            base.Channel.ExecuteCallback(eId, executeResult);
        }
        
        public System.Threading.Tasks.Task ExecuteCallbackAsync(System.Guid eId, Bluepath.ServiceReferences.RemoteExecutorServiceResult executeResult)
        {
            return base.Channel.ExecuteCallbackAsync(eId, executeResult);
        }
        
        public Bluepath.ServiceReferences.RemoteExecutorServiceResult TryJoin(System.Guid eId)
        {
            return base.Channel.TryJoin(eId);
        }
        
        public System.Threading.Tasks.Task<Bluepath.ServiceReferences.RemoteExecutorServiceResult> TryJoinAsync(System.Guid eId)
        {
            return base.Channel.TryJoinAsync(eId);
        }
    }
}
