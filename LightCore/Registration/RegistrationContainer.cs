﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LightCore.Registration.RegistrationSource;

namespace LightCore.Registration
{
    /// <summary>
    /// Represents an accessor interface for registration containers.
    /// </summary>
    internal class RegistrationContainer : IRegistrationContainer
    {
        /// <summary>
        /// Contains the lock object for all registrations.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Contains the duplicate registrations, e.g. plugins.
        /// </summary>
        private readonly List<RegistrationItem> _duplicateRegistrations;

        /// <summary>
        /// Containes the unique registrations.
        /// </summary>
        private readonly IDictionary<Type, RegistrationItem> _registrations;

        /// <summary>
        /// Initializes a new instance of <see cref="RegistrationContainer" />.
        /// </summary>
        internal RegistrationContainer()
        {
            _duplicateRegistrations = new List<RegistrationItem>();
            _registrations = new Dictionary<Type, RegistrationItem>();

            RegistrationSources = new List<IRegistrationSource>();
        }

        /// <summary>
        /// Gets all registrations. (Without IRegistrationSource{T}).
        /// </summary>
        public IEnumerable<RegistrationItem> AllRegistrations
        {
            get
            {
                lock (_lock)
                {
                    return new ReadOnlyCollection<RegistrationItem>(
                        _registrations.Values.Concat(_duplicateRegistrations).ToList());
                }
            }
        }

        /// <summary>
        /// Contains all registration sources.
        /// </summary>
        public IList<IRegistrationSource> RegistrationSources { get; set; }

        /// <summary>
        /// Try get a registration based upon a contract type.
        /// </summary>
        /// <param name="contractType">The contract type.</param>
        /// <param name="registrationItem">The registration item.</param>
        /// <returns>The registrationitem.</returns>
        public bool TryGetRegistration(Type contractType, out RegistrationItem registrationItem)
        {
            return _registrations.TryGetValue(contractType, out registrationItem);
        }

        /// <summary>
        /// Try get a registration based upon a contract type.
        /// </summary>
        /// <param name="predicate">the predicate.</param>
        /// <returns>The registrationitem.</returns>
        public RegistrationItem GetRegistration(Func<RegistrationItem, bool> predicate)
        {
            return _registrations.Values.SingleOrDefault(predicate);
        }

        /// <summary>
        /// Determines whether a contracttype is registered / supported by the container, or not.
        ///  Search on all locations.
        /// </summary>
        /// <param name="contractType">The type of the contract.</param>
        /// <returns><value>true</value> if a registration with the contracttype found, or supported. Otherwise <value>false</value>.</returns>
        public bool HasRegistration(Type contractType)
        {
            return AllRegistrations.Any(registration => registration.ContractType == contractType);
        }

        /// <summary>
        /// Determines whether a contracttype is registered as duplicate (many of them).
        /// </summary>
        /// <param name="contractType">The contract type.</param>
        /// <returns><value>true</value> if this type is registered many times.</returns>
        public bool HasDuplicateRegistration(Type contractType)
        {
            return _duplicateRegistrations.Any(registration => registration.ContractType == contractType);
        }

        /// <summary>
        /// Adds a registration.
        /// </summary>
        /// <param name="registration">The registration.</param>
        public void AddRegistration(RegistrationItem registration)
        {
            lock (_lock)
            {
                if (!HasRegistration(registration.ContractType))
                {
                    _registrations.Add(registration.ContractType, registration);
                }
                else
                {
                    // Duplicate registration for enumerable requests.
                    RegistrationItem duplicateItem;

                    _registrations.TryGetValue(registration.ContractType, out duplicateItem);

                    if (duplicateItem != null)
                    {
                        _duplicateRegistrations.Add(duplicateItem);
                        RemoveRegistration(duplicateItem.ContractType);
                    }

                    _duplicateRegistrations.Add(registration);
                }
            }
        }

        /// <summary>
        /// Removes a registration.
        /// </summary>
        /// <param name="contractType"></param>
        public void RemoveRegistration(Type contractType)
        {
            lock (_lock)
            {
                _registrations.Remove(contractType);
            }
        }

        /// <summary>
        /// Determines whether a contracttype is supported by registration sources.
        /// </summary>
        /// <param name="contractType">The type of the contract.</param>
        /// <returns><value>true</value> if the type is supported by a registration source, otherwise <value>false</value>.</returns>
        public bool IsSupportedByRegistrationSource(Type contractType)
        {
            return RegistrationSources
                .Select(registrationSource => registrationSource.SourceSupportsTypeSelector)
                .Any(sourceSupportsTypeSelector => sourceSupportsTypeSelector(contractType));
        }
    }
}