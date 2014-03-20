namespace Bluepath.Services
{
    public interface IListener
    {
        ServiceUri CallbackUri { get; }

        void Stop();
    }
}
