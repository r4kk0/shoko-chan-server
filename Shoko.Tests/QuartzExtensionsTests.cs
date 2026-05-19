#nullable enable

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Shoko.Server.Scheduling;
using Shoko.Server.Scheduling.Attributes;
using Shoko.Server.Scheduling.GenericJobBuilder;
using Shoko.Server.Settings;
using Shoko.Server.Utilities;
using Xunit;

namespace Shoko.Tests;

public class QuartzExtensionsTests
{
    [Fact]
    public async Task DuplicateJobWithHigherPriorityPromotesExistingTrigger()
    {
        using var settingsScope = new SettingsScope();
        var scheduler = await CreateScheduler();
        try
        {
            await scheduler.StartJob<PriorityPromotionTestJob>(job => job.Id = 1);
            await scheduler.StartJob<PriorityPromotionTestJob>(job => job.Id = 1, prioritize: true);

            var trigger = await GetSingleTrigger(scheduler, 1);

            Assert.Equal(10, trigger.Priority);
        }
        finally
        {
            await scheduler.Shutdown(false);
        }
    }

    [Fact]
    public async Task DuplicateJobWithLowerPriorityDoesNotDemoteExistingTrigger()
    {
        using var settingsScope = new SettingsScope();
        var scheduler = await CreateScheduler();
        try
        {
            await scheduler.StartJob<PriorityPromotionTestJob>(job => job.Id = 2, prioritize: true);
            await scheduler.StartJob<PriorityPromotionTestJob>(job => job.Id = 2);

            var trigger = await GetSingleTrigger(scheduler, 2);

            Assert.Equal(10, trigger.Priority);
        }
        finally
        {
            await scheduler.Shutdown(false);
        }
    }

    private static async Task<IScheduler> CreateScheduler()
    {
        var properties = new NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = $"{nameof(QuartzExtensionsTests)}-{Guid.NewGuid()}",
            ["quartz.scheduler.instanceId"] = "AUTO",
            ["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz",
            ["quartz.threadPool.threadCount"] = "1",
            ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz",
        };
        return await new StdSchedulerFactory(properties).GetScheduler();
    }

    private static async Task<ITrigger> GetSingleTrigger(IScheduler scheduler, int id)
    {
        var jobKey = JobKeyBuilder<PriorityPromotionTestJob>.Create()
            .UsingJobData(job => job.Id = id)
            .Build();
        var triggers = await scheduler.GetTriggersOfJob(jobKey);
        return Assert.Single(triggers);
    }

    public class PriorityPromotionTestJob : IJob
    {
        [JobKeyMember]
        public int Id { get; set; }

        public ValueTask Execute(IJobExecutionContext context)
            => ValueTask.CompletedTask;
    }

    private sealed class SettingsScope : IDisposable
    {
        private readonly ISettingsProvider _previous;

        public SettingsScope()
        {
            _previous = Utils.SettingsProvider;
            Utils.SettingsProvider = new TestSettingsProvider(new ServerSettings
            {
                Quartz =
                {
                    BatchMaxInsertSize = 1,
                    BatchInsertTimeoutInMS = 0,
                },
            });
        }

        public void Dispose()
        {
            Utils.SettingsProvider = _previous;
        }
    }

    private sealed class TestSettingsProvider : ISettingsProvider
    {
        private readonly IServerSettings _settings;

        public TestSettingsProvider(IServerSettings settings)
        {
            _settings = settings;
        }

        public IServerSettings GetSettings(bool copy = false)
            => _settings;

        public void SaveSettings(IServerSettings settings)
        {
        }

        public void SaveSettings()
        {
        }

        public void DebugSettingsToLog()
        {
        }
    }
}
