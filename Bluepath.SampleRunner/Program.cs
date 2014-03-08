namespace Bluepath.SampleRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            BluepathSingleton.Instance.Initialize("127.0.0.1");
        }
    }
}
