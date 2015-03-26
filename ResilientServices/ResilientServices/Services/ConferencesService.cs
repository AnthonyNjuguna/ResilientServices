using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Akavache;

using Connectivity.Plugin;

using Fusillade;

using Polly;

using TekConf.Mobile.Core.Dtos;

namespace ResilientServices.Services
{
    
    public class ConferencesService : IConferencesService
    {
        private readonly IApiService apiService;

        public ConferencesService(IApiService apiService)
        {
            this.apiService = apiService;
        }

        public async Task<List<ConferenceDto>> GetConferences(Priority priority)
        {
            var cache = BlobCache.LocalMachine;
            var cachedConferences = cache.GetAndFetchLatest("conferences", () => GetRemoteConferencesAsync(priority));

            var conferences = await cachedConferences.FirstOrDefaultAsync();

            return conferences ?? new List<ConferenceDto> { new ConferenceDto { Name = "Example" } };
        }

        public async Task<ConferenceDto> GetConference(Priority priority, string slug)
        {
            var cachedConference = BlobCache.LocalMachine.GetAndFetchLatest(slug, () => GetRemoteConference(priority, slug), offset =>
            {
                TimeSpan elapsed = DateTimeOffset.Now - offset;
                return elapsed > new TimeSpan(hours: 0, minutes: 30, seconds: 0);
            });

            var conference = await cachedConference.FirstOrDefaultAsync();

            return conference;
        }


        private async Task<List<ConferenceDto>> GetRemoteConferencesAsync(Priority priority)
        {
            List<ConferenceDto> conferences = null;
            Task<List<ConferenceDto>> getConferencesTask;
            switch (priority)
            {
                case Priority.Background:
                    getConferencesTask = apiService.Background.GetConferences();
                    break;
                case Priority.UserInitiated:
                    getConferencesTask = apiService.UserInitiated.GetConferences();
                    break;
                case Priority.Speculative:
                    getConferencesTask = apiService.Speculative.GetConferences();
                    break;
                default:
                    getConferencesTask = apiService.UserInitiated.GetConferences();
                    break;
            }
            
            //TODO: why does the emulator thinks it not connected (browser works)
            //if (CrossConnectivity.Current.IsConnected)
            {
                conferences = await Policy
                    .Handle<WebException>()
                    .WaitAndRetryAsync
                    (
                        retryCount: 5,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (ex, timespan) => { }
                    )
                    .ExecuteAsync(async () => await getConferencesTask);
            }
            return conferences;
        }

        private async Task<ConferenceDto> GetRemoteConference(Priority priority, string slug)
        {
            ConferenceDto conference = null;

            Task<ConferenceDto> getConferenceTask;
            switch (priority)
            {
                case Priority.Background:
                    getConferenceTask = apiService.Background.GetConference(slug);
                    break;
                case Priority.UserInitiated:
                    getConferenceTask = apiService.UserInitiated.GetConference(slug);
                    break;
                case Priority.Speculative:
                    getConferenceTask = apiService.Speculative.GetConference(slug);
                    break;
                default:
                    getConferenceTask = apiService.UserInitiated.GetConference(slug);
                    break;
            }

            //TODO: why does the emulator thinks it not connected (browser works)
            //if (CrossConnectivity.Current.IsConnected)
            {
                conference = await Policy
                    .Handle<Exception>()
                    .RetryAsync(retryCount: 5)
                    .ExecuteAsync(async () => await getConferenceTask);
            }

            return conference;
        }
    }
}