using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surge.Core.Exceptions
{
    public class NucleusCatchupFailedException : SurgeException
    {
        public NucleusCatchupFailedException(string message, Exception innerException)
            : base(ExceptionCategory.Nickel, message, innerException)
        {
        }
    }
}
