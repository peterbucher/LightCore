﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightCore.Activation.Activators;
using LightCore.ExtensionMethods.System;
using LightCore.ExtensionMethods.System.Collections.Generic;
using LightCore.Fluent;
using LightCore.Lifecycle;
using LightCore.Properties;
using LightCore.Registration;
using LightCore.Registration.RegistrationSource;
using LightCore.Activation.Components;

namespace LightCore
{
    /// <summary>
    /// Represents a builder that is reponsible for accepting, validating registrations
    /// and builds the container with that registrations.
    /// </summary>
    public class ContainerBuilder : IContainerBuilder
    {
        /// <summary>
        /// Contains the active registration groups as comma separated string.
        /// </summary>
        private string _activeRegistrationGroups;

        /// <summary>
        /// Contains the active registration groups as array for internal use.
        /// </summary>
        private string[] _activeRegistrationGroupsInternal;

        /// <summary>
        /// Gets or sets the active group configurations.
        /// </summary>
        public string ActiveRegistrationGroups
        {
            get
            {
                return _activeRegistrationGroups;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException("value");
                }

                this._activeRegistrationGroups = value;
                this._activeRegistrationGroupsInternal = value.Split(new[] { ',' });
            }
        }

        /// <summary>
        /// Holds a container for bootstrapping LightCore.
        /// </summary>
        private readonly IContainer _bootStrappingContainer;

        /// <summary>
        /// Holds a container with registrations to register.
        /// </summary>
        private readonly IRegistrationContainer _registrationContainer;

        /// <summary>
        /// Holds a list with registering callbacks.
        /// </summary>
        private readonly IList<Action> _registrationCallbacks;

        /// <summary>
        /// Holds the default lifecycle function.
        /// </summary>
        private Func<ILifecycle> _defaultLifecycleFunction;

        /// <summary>
        /// Initializes a new instance of <see cref="ContainerBuilder" />.
        /// </summary>
        public ContainerBuilder()
        {
            this._registrationContainer = new RegistrationContainer();
            this._registrationCallbacks = new List<Action>();
            this._defaultLifecycleFunction = () => new TransientLifecycle();

            this._bootStrappingContainer = this.Build();
        }

        /// <summary>
        /// Builds the container.
        /// </summary>
        /// <returns>The builded container.</returns>
        public IContainer Build()
        {
            var allRegistrationSources = new List<IRegistrationSource>()
                                             {
                                                 new OpenGenericRegistrationSource(this._registrationContainer),
                                                 new EnumerableRegistrationSource(this._registrationContainer),
                                                 new ArrayRegistrationSource(this._registrationContainer),
                                                 new ConcreteTypeRegistrationSource()
                                             };

            allRegistrationSources.Add(new FactoryRegistrationSource(this._registrationContainer));

            this._registrationContainer.RegistrationSources = allRegistrationSources;

            this.BootStrappLightCore();

            // Invoke the callbacks, they assert if the registration already exists, if not, register the registration.
            this._registrationCallbacks.ForEach(registerCallback => registerCallback());
            this._registrationCallbacks.Clear();

            var container = new Container(this._registrationContainer);

            return container;
        }

        private void BootStrappLightCore()
        {
            var typeOfArgumentCollector = typeof(IArgumentCollector);
            var typeOfConstructorSelector = typeof(IConstructorSelector);

            if (!_registrationContainer.HasRegistration(typeOfArgumentCollector))
            {
                RegisterFactory<IArgumentCollector>(c => new ArgumentCollector()).ControlledBy<SingletonLifecycle>();
            }

            if (!_registrationContainer.HasRegistration(typeOfConstructorSelector))
            {
                RegisterFactory<IConstructorSelector>(c => new ConstructorSelector()).ControlledBy<SingletonLifecycle>();
            }
        }

        /// <summary>
        /// Registers a module with registrations.
        /// </summary>
        /// <param name="registrationModule">The module to register within this container builder.</param>
        public void RegisterModule(RegistrationModule registrationModule)
        {
            registrationModule.Register(this);
        }

        /// <summary>
        /// Sets the default lifecycle for this container. (e.g. SingletonLifecycle, TrainsientLifecycle, ...).
        /// </summary>
        /// <typeparam name="TLifecycle">The default lifecycle.</typeparam>
        public void DefaultControlledBy<TLifecycle>() where TLifecycle : ILifecycle, new()
        {
            _defaultLifecycleFunction = () => new TLifecycle();
        }

        /// <summary>
        /// Sets the default lifecycle function for this container. (e.g. SingletonLifecycle, TrainsientLifecycle, ...).
        /// </summary>
        /// <param name="lifecycleFunction">The creator function for default lifecycle.</param>
        public void DefaultControlledBy(Func<ILifecycle> lifecycleFunction)
        {
            _defaultLifecycleFunction = lifecycleFunction;
        }

        /// <summary>
        /// Registers a type to itself.
        /// </summary>
        /// <typeparam name="TSelf">The type.</typeparam>
        /// <returns>An instance of <see cref="IFluentRegistration"  /> that exposes fluent registration.</returns>
        public IFluentRegistration Register<TSelf>()
        {
            var typeOfSelf = typeof(TSelf);

            if (!typeOfSelf.IsConcreteType())
            {
                throw new InvalidRegistrationException(
                    Resources.InvalidRegistrationFormat.FormatWith(typeOfSelf.ToString()));
            }

            // Return a new instance of <see cref="IFluentRegistration" /> for supporting a fluent interface for registration configuration.
            return Register(typeOfSelf, typeOfSelf);
        }

        /// <summary>
        /// Registers a type an instance.
        /// </summary>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <param name="instance">Instance to return</param>
        /// <returns>An instance of <see cref="IFluentRegistration"  /> that exposes fluent registration.</returns>
        public IFluentRegistration RegisterInstance<TInstance>(TInstance instance)
        {
            // Return a new instance of <see cref="IFluentRegistration" /> for supporting a fluent interface for registration configuration.
            return AddToRegistrationFluent(new RegistrationItem(typeof(TInstance))
            {
                Activator = new InstanceActivator<TInstance>(instance)
            });
        }

        /// <summary>
        /// Registers a type an instance.
        /// </summary>
        /// <param name="contractType">The type of the contract</param>
        /// <param name="instance">Instance to return</param>
        /// <returns>An instance of <see cref="IFluentRegistration"  /> that exposes fluent registration.</returns>
        public IFluentRegistration RegisterInstance(Type contractType, object instance)
        {
            var classType = typeof(InstanceActivator<>);
            var typeParams = new Type[] { contractType };
            var constructedType = classType.MakeGenericType(typeParams);

            var activator = Activator.CreateInstance(constructedType, new object[] { instance });

            return AddToRegistrationFluent(new RegistrationItem(contractType)
            {
                Activator = (IActivator)activator
            });
        }

        /// <summary>
        /// Registers a contract with an activator function.
        /// </summary>
        /// <typeparam name="TContract">The type of the contract.</typeparam>
        /// <param name="activatorFunction">The activator as function..</param>
        /// <returns>An instance of <see cref="IFluentRegistration"  /> that exposes a fluent interface for registration configuration.</returns>
        public IFluentRegistration RegisterFactory<TContract>(Func<IContainer, TContract> activatorFunction)
        {
            // Return a new instance of <see cref="IFluentRegistration" /> for supporting a fluent interface for registration configuration.
            return AddToRegistrationFluent(new RegistrationItem(typeof(TContract))
            {
                Activator = new DelegateActivator(c => activatorFunction(c))
            });
        }

        /// <summary>
        /// Registers a contract with an activator function.
        /// </summary>
        /// <param name="contractType">The type of the contract</param>
        /// <param name="activatorFunction">The activator as function..</param>
        /// <returns>An instance of <see cref="IFluentRegistration"  /> that exposes a fluent interface for registration configuration.</returns>
        public IFluentRegistration RegisterFactory(Type contractType, Func<IContainer, object> activatorFunction)
        {
            // Return a new instance of <see cref="IFluentRegistration" /> for supporting a fluent interface for registration configuration.
            return AddToRegistrationFluent(new RegistrationItem(contractType)
            {
                Activator = new DelegateActivator(c => activatorFunction(c))
            });
        }

        /// <summary>
        /// Add a registrationItem to the registrations.
        /// </summary>
        /// <param name="registrationItem">The registration to add.</param>
        private IFluentRegistration AddToRegistrationFluent(RegistrationItem registrationItem)
        {
            Action registrationCallback = () =>
                                              {

                                                  // Set default reuse scope, if not user defined. (System default is <see cref="TransientLifecycle" />.
                                                  if (registrationItem.Lifecycle == null)
                                                  {
                                                      registrationItem.Lifecycle = this._defaultLifecycleFunction();
                                                  }

                                                  if (_activeRegistrationGroupsInternal != null &&
                                                      registrationItem.Group != null)
                                                  {
                                                      if (
                                                          !this._activeRegistrationGroupsInternal.Any(
                                                              g => g.Trim() == registrationItem.Group.Trim()))
                                                      {
                                                          // Do not add inactive registrationItem.
                                                          return;
                                                      }
                                                  }

                                                  if (_activeRegistrationGroupsInternal == null && registrationItem.Group != null)
                                                  {
                                                      // Do not add inactive registrationItem.
                                                      return;
                                                  }

                                                  _registrationContainer.AddRegistration(registrationItem);

                                              };

            _registrationCallbacks.Add(registrationCallback);

            // Return a new instance of <see cref="IFluentRegistration" /> for supporting a fluent interface for registration configuration.
            return new FluentRegistration(registrationItem);
        }

        /// <summary>
        /// Registers a contract with its implementationtype.
        /// 
        ///  Can be a generic contract (open generic types) with its implementationtype.
        /// e.g. builder.RegisterGeneric(typeof(IRepository{T}), typeof(Repository{T}));
        /// container.Resolve{IRepository{Foo}}();
        /// </summary>
        /// <typeparam name="TContract">The type of the contract.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation for the contract</typeparam>
        /// <returns>An instance of <see cref="IFluentRegistration"  /> that exposes a fluent interface for registration configuration.</returns>
        public IFluentRegistration Register<TContract, TImplementation>() where TImplementation : TContract
        {
            // Return a new instance of <see cref="IFluentRegistration" /> for supporting a fluent interface for registration configuration.
            return this.Register(typeof(TContract), typeof(TImplementation));
        }

        /// <summary>
        /// Registers a contract with its implementationtype.
        /// </summary>
        /// <param name="typeOfContract">The type of the contract.</param>
        /// <param name="typeOfImplementation">The type of the implementation for the contract</param>
        /// <returns>An instance of <see cref="IFluentRegistration"  /> that exposes a fluent interface for registration configuration.</returns>
        public IFluentRegistration Register(Type typeOfContract, Type typeOfImplementation)
        {
            if (!typeOfContract.GetTypeInfo().IsGenericTypeDefinition && !typeOfContract.IsAssignableFrom(typeOfImplementation))
            {
                throw new ContractNotImplementedByTypeException(
                    Resources.ContractNotImplementedByTypeFormat.FormatWith(typeOfContract, typeOfImplementation), typeOfContract, typeOfImplementation);
            }

            // Return a new instance of <see cref="IFluentRegistration" /> for supporting a fluent interface for registration configuration.
            return this.AddToRegistrationFluent(new RegistrationItem(typeOfContract)
            {
                Activator = new ReflectionActivator(
                                                            typeOfImplementation,
                                                            this._bootStrappingContainer.Resolve<IConstructorSelector>(),
                                                            this._bootStrappingContainer.Resolve<IArgumentCollector>()),
                Lifecycle = this._defaultLifecycleFunction(),
                ImplementationType = typeOfImplementation
            });
        }
    }
}