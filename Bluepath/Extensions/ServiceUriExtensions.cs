namespace Bluepath.Extensions
{
    public static class ServiceUriExtensions
    {
        public static ServiceReferences.ServiceUri Convert(this Services.ServiceUri uri)
        {
            return new ServiceReferences.ServiceUri()
            {
                Address = uri.Address
            };
        }
    }
}
