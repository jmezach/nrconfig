using System;
using System.Linq;
using System.Collections.Generic;
using NRConfig;
using System.Reflection;
using NRConfigManager.Infrastructure.Reflected;

namespace NRConfigManager.Infrastructure.NServiceBus
{
    public class NServiceBusInstrumentationDiscoverer : InstrumentationDiscovererBase
    {
        public override IEnumerable<InstrumentationTarget> GetInstrumentationSet(string assemblyPath, InstrumentAttribute context, Predicate<ITypeDetails> typeFilter)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var types = assembly.GetTypes()
                                .Where(type => type.IsClass && type.GetInterfaces().Any(@interface => @interface.FullName.StartsWith("NServiceBus.IHandleMessages")))
                                .Where(type => !type.BaseType.FullName.StartsWith("NServiceBus.Saga.Saga"));

            var handlers = types.Select(type => new
            {
                HandlerType = type,
                HandleMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(method => method.Name.Equals("Handle"))
            });

            var result = from handler in handlers
                         from handlerMethod in handler.HandleMethods
                         select new InstrumentationTarget(
                             new ReflectedMethodDetails(handlerMethod),
                             handler.HandlerType.Name,
                             "NewRelic.Agent.Core.Tracer.Factories.BackgroundThreadTracerFactory",
                             null,
                             Metric.Scoped
                         );

            return result;
        }

        protected override IEnumerable<ITypeDetails> GetTypes(string assemblyPath)
        {
            // Should not be called
            throw new NotImplementedException();
        }
    }
}
