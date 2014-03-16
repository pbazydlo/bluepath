namespace Bluepath.SampleRunner
{
    using Bluepath.Services;

    internal class BluepathSingleton
    {
        private static readonly object InstanceLock = new object();
        private static BluepathSingleton instance;
        
        private BluepathSingleton()
        {
        }

        public static BluepathSingleton Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    if (instance == null)
                    {
                        instance = new BluepathSingleton();
                    }

                    return instance;
                }
            }
        }

        public IListener Listener { get; private set; }

        public void Initialize(string ip, int? port = null)
        {
            this.Listener = BluepathListener.InitializeDefaultListener(ip, port);
        }
    }
}
