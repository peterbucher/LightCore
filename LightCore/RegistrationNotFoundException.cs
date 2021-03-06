﻿using System;

namespace LightCore
{
    /// <summary>
    /// Thrown when a mapping not found for resolving a type.
    /// </summary>
    public class RegistrationNotFoundException : Exception
    {
        /// <summary>
        /// The contract type.
        /// </summary>
        public Type ContractType { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationNotFoundException"/> type.
        /// </summary>
        public RegistrationNotFoundException()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationNotFoundException"/> type.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public RegistrationNotFoundException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationNotFoundException"/> type.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="contractType">The contract type.</param>
        public RegistrationNotFoundException(string message, Type contractType)
            : base(message)
        {
            ContractType = contractType;
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationNotFoundException"/> type.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception</param>
        public RegistrationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }


    }
}