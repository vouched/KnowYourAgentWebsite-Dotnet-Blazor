using KnowYourAgentWebsite.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace KnowYourAgentWebsite.Tests.Hosting;

[TestFixture]
public sealed class ApplicationRestarterTests
{
	static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Test]
	public void A_restart_request_is_consumed_exactly_once()
	{
		var coordinator = new ApplicationRestartCoordinator();

		coordinator.RequestRestart();

		using (Assert.EnterMultipleScope())
		{
			Assert.That(coordinator.TryConsumeRestartRequest(), Is.True);
			Assert.That(coordinator.TryConsumeRestartRequest(), Is.False);
			Assert.That(coordinator.IsRestartRequested, Is.False);
		}
	}

	[Test]
	public async Task Stops_the_host_after_the_delay_and_asks_for_a_new_one()
	{
		var timeProvider = new FakeTimeProvider();
		var coordinator = new ApplicationRestartCoordinator();
		using var lifetime = new FakeHostApplicationLifetime();
		var restarter = new ApplicationRestarter(coordinator, lifetime, timeProvider, NullLogger<ApplicationRestarter>.Instance);

		restarter.ScheduleRestart(TimeSpan.FromSeconds(1));
		timeProvider.Advance(TimeSpan.FromMilliseconds(999));

		Assert.That(lifetime.StopApplicationCalled.IsCompleted, Is.False);

		timeProvider.Advance(TimeSpan.FromMilliseconds(1));
		await lifetime.StopApplicationCalled.WaitAsync(_timeout);

		Assert.That(coordinator.IsRestartRequested, Is.True);
	}

	[Test]
	public async Task A_normal_shutdown_cancels_a_pending_restart()
	{
		var timeProvider = new FakeTimeProvider();
		var coordinator = new ApplicationRestartCoordinator();
		using var lifetime = new FakeHostApplicationLifetime();
		var restarter = new ApplicationRestarter(coordinator, lifetime, timeProvider, NullLogger<ApplicationRestarter>.Instance);

		restarter.ScheduleRestart(TimeSpan.FromSeconds(1));
		await lifetime.SimulateCtrlCAsync();
		timeProvider.Advance(TimeSpan.FromSeconds(5));
		await Task.Delay(TimeSpan.FromMilliseconds(100));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(coordinator.IsRestartRequested, Is.False);
			Assert.That(lifetime.StopApplicationCalled.IsCompleted, Is.False);
		}
	}

	[Test]
	public void Rejects_a_negative_delay()
	{
		using var lifetime = new FakeHostApplicationLifetime();
		var restarter = new ApplicationRestarter(new ApplicationRestartCoordinator(), lifetime, new FakeTimeProvider(), NullLogger<ApplicationRestarter>.Instance);

		Assert.Throws<ArgumentOutOfRangeException>(() => restarter.ScheduleRestart(TimeSpan.FromSeconds(-1)));
	}

	sealed class FakeHostApplicationLifetime : IHostApplicationLifetime, IDisposable
	{
		readonly CancellationTokenSource _stopping = new();
		readonly TaskCompletionSource _stopApplicationCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public CancellationToken ApplicationStarted => CancellationToken.None;

		public CancellationToken ApplicationStopping => _stopping.Token;

		public CancellationToken ApplicationStopped => CancellationToken.None;

		public Task StopApplicationCalled => _stopApplicationCalled.Task;

		public void StopApplication() => _stopApplicationCalled.TrySetResult();

		public Task SimulateCtrlCAsync() => _stopping.CancelAsync();

		public void Dispose() => _stopping.Dispose();
	}
}
