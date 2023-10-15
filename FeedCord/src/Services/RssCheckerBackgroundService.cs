﻿using FeedCord.src.Common;
using FeedCord.src.Common.Interfaces;
using FeedCord.src.DiscordNotifier;
using FeedCord.src.RssReader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FeedCord.src.Services
{
    internal class RssCheckerBackgroundService : BackgroundService
    {
        private readonly ILogger<RssCheckerBackgroundService> logger;
        private readonly IFeedProcessor feedProcessor;
        private readonly INotifier notifier;
        private int delayTime;

        public RssCheckerBackgroundService(
            ILogger<RssCheckerBackgroundService> logger,
            IFeedProcessor feedProcessor,
            INotifier notifier,
            Config config)
        {
            this.logger = logger;
            this.feedProcessor = feedProcessor;
            this.notifier = notifier;
            this.delayTime = config.RssCheckIntervalMinutes;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Starting Background Process at {CurrentTime}", DateTime.Now);
                await RunRoutineBackgroundProcessAsync();
                logger.LogInformation("Finished Background Process at {CurrentTime}", DateTime.Now);
                await Task.Delay(TimeSpan.FromMinutes(delayTime), stoppingToken);
            }
        }

        private async Task RunRoutineBackgroundProcessAsync()
        {
            try
            {
                var posts = await feedProcessor.CheckForNewPostsAsync();

                if (posts.Count > 0)
                {
                    logger.LogInformation("Found {PostCount} new posts at {CurrentTime}", posts.Count, DateTime.Now);
                    await notifier.SendNotificationsAsync(posts);
                }
                else
                {
                    logger.LogInformation("Found no new posts at {CurrentTime}. Ending background process.", DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while checking for new posts at {CurrentTime}.", DateTime.Now);
            }
        }
    }
}
