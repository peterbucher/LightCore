﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace LightCore.Lifecycle
{
    /// <summary>
    /// Represents a singleton per thread lifecycle.
    /// (One instance is shared within one thread).
    /// </summary>
    public class ThreadSingletonLifecycle : ILifecycle
    {
        /// <summary>
        /// Contains the lock object for instance creation.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Holds an map with instances for different threads.
        /// </summary>
        private readonly IDictionary<int, WeakReference> _instanceMap;

        /// <summary>
        /// Initializes a new instance of <see cref="ThreadSingletonLifecycle" />.
        /// </summary>
        public ThreadSingletonLifecycle()
        {
            _instanceMap = new Dictionary<int, WeakReference>();
        }

        /// <summary>
        /// Handle the reuse of instances.
        /// </summary>
        /// <param name="newInstanceResolver">The function for lazy get an instance.</param>
        public object ReceiveInstanceInLifecycle(Func<object> newInstanceResolver)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;

            lock (_lock)
            {
                if (_instanceMap.ContainsKey(threadId))
                {
                    return _instanceMap[threadId];
                }

                var instance = newInstanceResolver();
                _instanceMap.Add(threadId, new WeakReference(instance));

                return instance;
            }
        }
    }
}