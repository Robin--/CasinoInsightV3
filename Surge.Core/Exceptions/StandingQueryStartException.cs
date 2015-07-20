using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surge.Core.Exceptions
{
    public class StandingQueryStartException : SurgeException
    {
        public StandingQueryStartException(string message, Exception innerException)
            : base(ExceptionCategory.Nickel, message, innerException)
        {
        }
    }
}
