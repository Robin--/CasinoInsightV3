using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;

namespace Surge.Core.IoC
{
    public class IoC
    {
        public static IContainer Container { get; private set; }

        public static void BootStrapContainer(Action<ContainerBuilder> configure)
        {
            var builder = new ContainerBuilder();

            configure.Invoke(builder);

            Container = builder.Build();
        }
    }
}
