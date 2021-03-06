﻿using FluentAssertions;
using System.Threading;
using Xunit;

namespace LightCore.Tests.Lifecycle.ThreadSingletonLifecycle
{
    public class WhenRetrieveInstanceInLifecycleIsCalled : LifecycleFixture
    {

        [Fact(Skip = "Not yet threadd save")]
        public void WithActivationFunction_DifferentObjectsPerThreadAreReturned()
        {
            var lifecycle = new LightCore.Lifecycle.ThreadSingletonLifecycle();
            var factory = this.GetActivationFactory();

            var threadData = new ThreadData(lifecycle, factory);
            var thread = new Thread(threadData.ResolveFoosWithLifecycle);

            var threadDataTwo = new ThreadData(lifecycle, factory);
            var threadTwo = new Thread(threadDataTwo.ResolveFoosWithLifecycle);

            thread.Start();
            threadTwo.Start();

            thread.Join();
            threadTwo.Join();

            threadData.FooOne.Should().Be(threadData.FooTwo);
            threadData.FooOne.Should().NotBe(threadDataTwo.FooOne);
        }
    }
}