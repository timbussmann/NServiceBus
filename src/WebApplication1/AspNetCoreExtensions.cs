// dependency on IServiceCollection!
// put in dedicated NuGet package
namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Hosting.Helpers;
    using Microsoft.Extensions.DependencyInjection;

    public static class AspNetCoreExtensions
    {
        public static void AddNServiceBus(this IServiceCollection serviceCollection)
        {
            var scannedTypes = GetAllowedTypes();

            RegisterMessageHandlers(serviceCollection, scannedTypes);
        }

        static void RegisterMessageHandlers(IServiceCollection serviceCollection, List<Type> scannedTypes)
        {
            foreach (var t in scannedTypes.Where(IsMessageHandler))
            {
                serviceCollection.AddScoped(t);
            }
        }

        static List<Type> GetAllowedTypes()
        {
            //
            var assemblyScannerSettings = new AssemblyScannerConfiguration();
            //TODO do we need a different scan path for asp.net core?
            var assemblyScanner = new AssemblyScanner(AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory)
            {
                AssembliesToSkip = assemblyScannerSettings.ExcludedAssemblies,
                TypesToSkip = assemblyScannerSettings.ExcludedTypes,
                ScanNestedDirectories = assemblyScannerSettings.ScanAssembliesInNestedDirectories,
                ThrowExceptions = assemblyScannerSettings.ThrowExceptions,
                ScanAppDomainAssemblies = assemblyScannerSettings.ScanAppDomainAssemblies
            };

            return assemblyScanner.GetScannableAssemblies().Types;
        }

        public static bool IsMessageHandler(Type type)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition)
            {
                return false;
            }

            return type.GetInterfaces()
                .Where(@interface => @interface.IsGenericType)
                .Select(@interface => @interface.GetGenericTypeDefinition())
                .Any(genericTypeDef => genericTypeDef == IHandleMessagesType);
        }

        static Type IHandleMessagesType = typeof(IHandleMessages<>);

    }
}