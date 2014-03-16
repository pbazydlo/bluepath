namespace Bluepath.Security
{
    using System.Security.Principal;

    public class UserAccountControlHelper
    {
        public static bool IsUserAdministrator
        {
            get
            {
                var identity = WindowsIdentity.GetCurrent();

                if (identity == null)
                {
                    return false;
                }

                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
