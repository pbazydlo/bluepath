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
	[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
	[System.ServiceModel.ServiceContractAttribute(ConfigurationName="IRemoteExecutorService")]
	public interface IRemoteExecutorService
	{
		
		[System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IExecutor/Execute", ReplyAction="http://tempuri.org/IExecutor/ExecuteResponse")]
		[System.ServiceModel.ServiceKnownTypeAttribute(typeof(object[]))]
		void Execute(object[] parameters);
		
		[System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IExecutor/Execute", ReplyAction="http://tempuri.org/IExecutor/ExecuteResponse")]
		System.Threading.Tasks.Task ExecuteAsync(object[] parameters);
		
		[System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IExecutor/Join", ReplyAction="http://tempuri.org/IExecutor/JoinResponse")]
		void Join();
		
		[System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IExecutor/Join", ReplyAction="http://tempuri.org/IExecutor/JoinResponse")]
		System.Threading.Tasks.Task JoinAsync();
		
		[System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IExecutor/GetResult", ReplyAction="http://tempuri.org/IExecutor/GetResultResponse")]
		[System.ServiceModel.ServiceKnownTypeAttribute(typeof(object[]))]
		object GetResult();
		
		[System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IExecutor/GetResult", ReplyAction="http://tempuri.org/IExecutor/GetResultResponse")]
		System.Threading.Tasks.Task<object> GetResultAsync();
		
		[System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/Initialize", ReplyAction="http://tempuri.org/IRemoteExecutorService/InitializeResponse")]
		void Initialize(byte[] methodHandle);
		
		[System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IRemoteExecutorService/Initialize", ReplyAction="http://tempuri.org/IRemoteExecutorService/InitializeResponse")]
		System.Threading.Tasks.Task InitializeAsync(byte[] methodHandle);
	}

	[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
	public interface IRemoteExecutorServiceChannel : IRemoteExecutorService, System.ServiceModel.IClientChannel
	{
	}

	[System.Diagnostics.DebuggerStepThroughAttribute()]
	[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
	public partial class RemoteExecutorServiceClient : System.ServiceModel.ClientBase<IRemoteExecutorService>, IRemoteExecutorService
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
		
		public void Execute(object[] parameters)
		{
			base.Channel.Execute(parameters);
		}
		
		public System.Threading.Tasks.Task ExecuteAsync(object[] parameters)
		{
			return base.Channel.ExecuteAsync(parameters);
		}
		
		public void Join()
		{
			base.Channel.Join();
		}
		
		public System.Threading.Tasks.Task JoinAsync()
		{
			return base.Channel.JoinAsync();
		}
		
		public object GetResult()
		{
			return base.Channel.GetResult();
		}
		
		public System.Threading.Tasks.Task<object> GetResultAsync()
		{
			return base.Channel.GetResultAsync();
		}
		
		public void Initialize(byte[] methodHandle)
		{
			base.Channel.Initialize(methodHandle);
		}
		
		public System.Threading.Tasks.Task InitializeAsync(byte[] methodHandle)
		{
			return base.Channel.InitializeAsync(methodHandle);
		}
	}
}