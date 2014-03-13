namespace Bluepath.Extensions
{
    public static class ServiceUriExtensions
    {
        public static ServiceReferences.ServiceUri Convert(this Services.ServiceUri uri)
        {
            if(uri==null)
            {
                return null;
            }

            return new ServiceReferences.ServiceUri()
            {
                Address = uri.Address
            };
        }
    }
}
