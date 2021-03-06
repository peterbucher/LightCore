﻿using System;
using System.Reflection;

using LightCore.Activation.Components;
using LightCore.ExtensionMethods.System;
using LightCore.Properties;

namespace LightCore.Activation.Activators
{
    /// <summary>
    /// Represents an reflection instance activator.
    /// </summary>
    internal class ReflectionActivator : IActivator
    {
        /// <summary>
        /// The container.
        /// </summary>
        private IContainer _container;

        /// <summary>
        /// The constructor selector.
        /// </summary>
        private readonly IConstructorSelector _constructorSelector;

        /// <summary>
        /// The argument collector.
        /// </summary>
        private readonly IArgumentCollector _argumentCollector;

        /// <summary>
        /// The implementation type.
        /// </summary>
        private readonly Type _implementationType;

        ///<summary>
        /// Creates a new instance of <see cref="ReflectionActivator" />.
        ///</summary>
        ///<param name="implementationType">The implementation type.</param>
        ///<param name="constructorSelector">The constructor selector.</param>
        ///<param name="argumentCollector">The argument collector.</param>
        internal ReflectionActivator(Type implementationType, IConstructorSelector constructorSelector, IArgumentCollector argumentCollector)
        {
            _implementationType = implementationType;
            _constructorSelector = constructorSelector;
            _argumentCollector = argumentCollector;
        }

        /// <summary>
        /// Activates an instance with given arguments.
        /// </summary>
        /// <param name="resolutionContext">The container.</param>
        /// <returns>The activated instance.</returns>
        public object ActivateInstance(ResolutionContext resolutionContext)
        {
            if (_container == null)
            {
                _container = resolutionContext.Container;
            }

            var constructors = _implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var finalConstructor = _constructorSelector.SelectConstructor(constructors, resolutionContext);

            var finalArguments = _argumentCollector.CollectArguments(
                _container.Resolve,
                finalConstructor.GetParameters(),
                resolutionContext);

            if (finalArguments != null && finalArguments.Length != finalConstructor.GetParameters().Length)
            {
                throw new ResolutionFailedException
                    (Resources.NoSuitableConstructorFoundFormat.FormatWith(_implementationType),
                     _implementationType);
            }

            return finalConstructor.Invoke(finalArguments);
        }
    }
}