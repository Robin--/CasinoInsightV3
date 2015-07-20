using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surge.Core.Exceptions
{
    public enum ExceptionCategory
    {
        /// <summary>
        /// Any issue with the nuclei in the Cerebral
        /// </summary>
        Nickel
    }

    public class SurgeException : Exception
    {
        public SurgeException(ExceptionCategory category, string message, Exception innerException)
            : base(message, innerException)
        {
            Category = category;
        }

        public ExceptionCategory Category { get; private set; }
    }
}
