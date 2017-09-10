using System;
using System.Text;

namespace Cronus.Transport.AzureServiceBus
{
    public static class ExceptionExtensions
    {
        public static string GetFullErrorMessage(this Exception e)
        {
            var res = new StringBuilder();

            while (e != null)
            {
                res.AppendLine(e.Message);
                e = e.InnerException;
            }

            return res.ToString();
        }
    }
}