﻿using System;
using LightCore.Integration.AspNetCore.Lifecycle;
using LightCore.Lifecycle;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LightCore.Integration.AspNetCore
{
    public static class LightCoreExtension
    {
        public static void Populate(this IContainerBuilder builder, IServiceCollection services, IHttpContextAccessor httpContextAccessor)
        {
            builder.RegisterFactory<IServiceProvider>(container => new LightCoreServiceProvider(container))
                .ControlledBy<SingletonLifecycle>();
            builder.RegisterFactory<IServiceScopeFactory>(container => new LightCoreServiceScopeFactory(container))
                .ControlledBy<SingletonLifecycle>();

            RegisterServices(builder, services, httpContextAccessor);
        }

        private static void RegisterServices(IContainerBuilder builder, IServiceCollection services, IHttpContextAccessor httpContextAccessor)
        {
            foreach (var serviceDescriptor in services)
            {
                switch (serviceDescriptor.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        Register(builder, serviceDescriptor, new SingletonLifecycle());
                        break;
                    case ServiceLifetime.Scoped:
                        Register(builder, serviceDescriptor, new HttpRequestLifecycle(httpContextAccessor));
                        break;
                    case ServiceLifetime.Transient:
                        Register(builder, serviceDescriptor, new TransientLifecycle());
                        break;
                }
            }
        }

        private static void Register(IContainerBuilder builder, ServiceDescriptor serviceDescriptor, ILifecycle lifecycle)
        {
            if (serviceDescriptor.ImplementationType != null)
            {
                builder.Register(serviceDescriptor.ServiceType, serviceDescriptor.ImplementationType)
                    .ControlledBy(lifecycle);
            }
            else if (serviceDescriptor.ImplementationFactory != null)
            {
                builder.RegisterFactory(serviceDescriptor.ServiceType, container =>
                    {
                        var serviceProvider = container.Resolve<IServiceProvider>();
                        return serviceDescriptor.ImplementationFactory(serviceProvider);
                    })
                    .ControlledBy(lifecycle);
            }
            else if (serviceDescriptor.ImplementationInstance != null)
            {
                builder.RegisterInstance(serviceDescriptor.ServiceType, serviceDescriptor.ImplementationInstance)
                    .ControlledBy(lifecycle);
            }
        }
    }
}
