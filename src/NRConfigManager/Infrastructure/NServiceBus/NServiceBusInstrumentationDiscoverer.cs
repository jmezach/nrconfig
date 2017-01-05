using Microsoft.Cci;
using NRConfig;
using NRConfigManager.Infrastructure.Cci;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NRConfigManager.Infrastructure.NServiceBus
{
    public class NServiceBusInstrumentationDiscoverer : InstrumentationDiscovererBase
    {
        public override IEnumerable<InstrumentationTarget> GetInstrumentationSet(string assemblyPath, InstrumentAttribute context, Predicate<ITypeDetails> typeFilter)
        {
            var host = new PeReader.DefaultHost();
            var assembly = host.LoadUnitFrom(assemblyPath) as IAssembly;

            if (assembly == null || assembly == Dummy.Assembly)
            {
                throw new InvalidOperationException(string.Format("Failed to load assembly from '{0}'", assemblyPath));
            }

            var types = assembly.GetAllTypes()
                                .Where(type => type.IsClass)
                                .Where(type => type.Interfaces.Any(@interface => TypeHelper.GetTypeName(@interface).StartsWith("NServiceBus.IHandleMessages")) ||
                                               (type.BaseClasses.FirstOrDefault()?.ResolvedType.Interfaces.Any(@interface => TypeHelper.GetTypeName(@interface).StartsWith("NServiceBus.IHandleMessages"))).GetValueOrDefault())
                                .Where(type => !TypeHelper.GetTypeName(type.BaseClasses.FirstOrDefault()).StartsWith("NServiceBus.Saga.Saga"));

            var handlers = types.Select(type => new
            {
                HandlerType = type,
                HandleMethods = type.Methods.Where(method => method.Name.Value.Equals("Handle"))
            });

            var result = from handler in handlers
                         from handlerMethod in handler.HandleMethods
                         select new InstrumentationTarget(
                             new CciMethodDetails(handlerMethod),
                             handler.HandlerType.Name.Value,
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
