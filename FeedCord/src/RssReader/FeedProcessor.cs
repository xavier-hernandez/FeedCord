﻿using FeedCord.src.Common;
using FeedCord.src.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FeedCord.src.RssReader
{
    internal class FeedProcessor : IFeedProcessor
    {
        private readonly Config config;
        private readonly HttpClient httpClient;
        private readonly IRssProcessorService rssProcessorService;
        private readonly ILogger<FeedProcessor> logger;
        private Dictionary<string, DateTime> rssFeedData = new();

        public FeedProcessor(
            Config config,
            IHttpClientFactory httpClientFactory,
            IRssProcessorService rssProcessorService,
            ILogger<FeedProcessor> logger)
        {
            this.config = config;
            this.httpClient = httpClientFactory.CreateClient();
            this.rssProcessorService = rssProcessorService;
            this.logger = logger;

            InitializeUrls();
        }

        public async Task<List<Post>> CheckForNewPostsAsync()
        {
            List<Post> newPosts = new();

            foreach (var rssFeed in rssFeedData)
            {
                var post = await CheckFeedForUpdatesAsync(rssFeed.Key);

                if (post.PublishDate > rssFeed.Value)
                {
                    rssFeedData[rssFeed.Key] = post.PublishDate;
                    newPosts.Add(post);
                    logger.LogInformation("Found new post for Url: {RssFeedKey}", rssFeed.Key);
                }
            }

            return newPosts;
        }

        private void InitializeUrls()
        {
            foreach (var url in config.Urls)
            {
                if (!rssFeedData.ContainsKey(url))
                    rssFeedData[url] = DateTime.MinValue;
            }

            logger.LogInformation("Set initial datetime for {UrlCount} Urls on first run", config.Urls.Length);
        }

        private async Task<Post> CheckFeedForUpdatesAsync(string url)
        {
            var response = await httpClient.GetAsync(url);
            string xmlContent = await response.Content.ReadAsStringAsync();
            return await rssProcessorService.ParseRssFeedAsync(xmlContent);
        }
    }
}
