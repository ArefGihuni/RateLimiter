﻿using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace RateLimiter.Tests
{
    public class ComposedAwaitableConstraintTest
    {
        private readonly IAwaitableConstraint _IAwaitableConstraint1;
        private readonly IAwaitableConstraint _IAwaitableConstraint2;
        private readonly IDisposable _Diposable1;
        private readonly IDisposable _Diposable2;
        private readonly ComposedAwaitableConstraint _Composed;

        public ComposedAwaitableConstraintTest()
        {
            _IAwaitableConstraint1 = Substitute.For<IAwaitableConstraint>();
            _IAwaitableConstraint2 = Substitute.For<IAwaitableConstraint>();
            _Diposable1 = Substitute.For<IDisposable>();
            _Diposable2 = Substitute.For<IDisposable>();
            _IAwaitableConstraint1.WaitForReadiness(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_Diposable1));
            _IAwaitableConstraint2.WaitForReadiness(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_Diposable2));
            _Composed = new ComposedAwaitableConstraint(_IAwaitableConstraint1, _IAwaitableConstraint2);
        }

        [Fact]
        public async Task WaitForReadiness_Call_ComposingElementsWaitForReadiness()
        {
            await _Composed.WaitForReadiness(CancellationToken.None);

            await _IAwaitableConstraint1.Received(1).WaitForReadiness(CancellationToken.None);
            await _IAwaitableConstraint2.Received(1).WaitForReadiness(CancellationToken.None);
        }

        [Fact]
        public async Task WaitForReadiness_Block()
        {
            await _Composed.WaitForReadiness(CancellationToken.None);
            var timedOut = await WaitForReadinessHasTimeOut();
            timedOut.Should().BeTrue();
        }

        [Fact]
        public async Task WaitForReadiness_WhenCancelled_DoNotBlock()
        {
            var cancellation = new CancellationToken(true);
            try
            {
                await _Composed.WaitForReadiness(cancellation);
            }
            catch
            {
            }
            var timedOut = await WaitForReadinessHasTimeOut();
            timedOut.Should().BeFalse();
        }

        [Fact]
        public void WaitForReadiness_WhenCancelled_ThrowException()
        {
            var cancellation = new CancellationToken(true);
            Func<Task> act = async () => await _Composed.WaitForReadiness(cancellation);
            act.Should().Throw<TaskCanceledException>();
        }

        [Fact]
        public async Task WaitForReadiness_BlockUntillDisposeIsCalled()
        {
            var disp = await _Composed.WaitForReadiness(CancellationToken.None);
            disp.Dispose();

            var timedOut = await WaitForReadinessHasTimeOut();
            timedOut.Should().BeFalse();
        }

        private async Task<bool> WaitForReadinessHasTimeOut()
        {
            var task = _Composed.WaitForReadiness(CancellationToken.None);
            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(200));

            var taskcomplete = await Task.WhenAny(task, timeoutTask);
            return taskcomplete == timeoutTask;
        }

        [Fact]
        public async Task Execute_Call_ComposingElementsExecute()
        {
            var disp = await _Composed.WaitForReadiness(CancellationToken.None);
            disp.Dispose();

            _Diposable1.Received(1).Dispose();
            _Diposable2.Received(1).Dispose();
        }
    }
}
