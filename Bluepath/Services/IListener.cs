namespace Bluepath.Services
{
    public interface IListener
    {
        ServiceUri CallbackUri { get; }

        void Wait();

        void Stop();
    }
}
