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
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="RemoteExecutorServiceResult", Namespace="http://schemas.datacontract.org/2004/07/Bluepath.Services")]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(object[]))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(Bluepath.ServiceReferences.RemoteExecutorServiceResult.State))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(System.Exception))]
    public partial class RemoteExecutorServiceResult : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private System.Nullable<System.TimeSpan> ElapsedTimeField;
        
        private System.Exception ErrorField;
        
        private Bluepath.ServiceReferences.RemoteExecutorServiceResult.State ExecutorStateField;
        
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
        public Bluepath.ServiceReferences.RemoteExecutorServiceResult.State ExecutorState
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
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
        [System.Runtime.Serialization.DataContractAttribute(Name="RemoteExecutorServiceResult.State", Namespace="http://schemas.datacontract.org/2004/07/Bluepath.Services")]
        public enum State : int
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
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(object[]))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Bluepath.ServiceReferences.RemoteExecutorServiceResult))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(Bluepath.ServiceReferences.RemoteExecutorServiceResult.State))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(System.Exception))]
        void Execute(System.Guid eId, object[] parameters);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/Execute", ReplyAction="http://tempuri.org/IRemoteExecutorService/ExecuteResponse")]
        System.Threading.Tasks.Task ExecuteAsync(System.Guid eId, object[] parameters);
        
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
        
        public void Execute(System.Guid eId, object[] parameters)
        {
            base.Channel.Execute(eId, parameters);
        }
        
        public System.Threading.Tasks.Task ExecuteAsync(System.Guid eId, object[] parameters)
        {
            return base.Channel.ExecuteAsync(eId, parameters);
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
