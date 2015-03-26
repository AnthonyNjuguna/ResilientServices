using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Akavache;

using Cirrious.MvvmCross.ViewModels;

using Fusillade;

using PropertyChanged;

using ResilientServices.Services;

using TekConf.Mobile.Core.Dtos;

namespace ResilientServices.ViewModels
{
    [ImplementPropertyChanged]
    public class MainViewModel : MvxViewModel
    {
        private readonly IConferencesService conferencesService;

        public MainViewModel(IConferencesService conferencesService)
        {
            this.conferencesService = conferencesService;
        }

        public List<ConferenceDto> Conferences { get; set; }
        
        public bool IsLoading { get; set; }

        public MvxCommand RefreshCommand
        {
            get { return new MvxCommand(() =>
            {
                BlobCache.LocalMachine.Invalidate("conferences");
                GetConferences();
            }); }
        }

        public override async void Start()
        {
            base.Start();

            await GetConferences();
        }

        private async Task GetConferences()
        {
            IsLoading = true;

            var conferences = await conferencesService
                .GetConferences(Priority.UserInitiated);

            CacheConferences(conferences);

            Conferences = conferences;

            IsLoading = false;
        }

        private void CacheConferences(List<ConferenceDto> conferences)
        {
            foreach (var slug in conferences.Where(c => c.Name != "Example").Select(x => x.Slug))
            {
                conferencesService.GetConference(Priority.Speculative, slug);
            }
        }
    }
}
