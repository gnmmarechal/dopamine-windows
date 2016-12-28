﻿using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Dopamine.Common.Services.Dialog;
using Dopamine.Common.Services.Provider;
using Dopamine.Common.Services.Scrobbling;
using Dopamine.Common.Base;
using Dopamine.Common.Prism;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Dopamine.SettingsModule.ViewModels
{
    public class SettingsOnlineViewModel : BindableBase
    {
        #region Private
        private IUnityContainer container;
        private IProviderService providerService;
        private IDialogService dialogService;
        private IEventAggregator eventAggregator;
        private ObservableCollection<SearchProvider> searchProviders;
        private SearchProvider selectedSearchProvider;
        private IScrobblingService scrobblingService;
        private bool isLastFmSignInInProgress;
        private bool checkBoxDownloadArtistInformationChecked;
        private bool checkBoxDownloadLyricsChecked;
        #endregion

        #region Commands
        public DelegateCommand AddCommand { get; set; }
        public DelegateCommand EditCommand { get; set; }
        public DelegateCommand RemoveCommand { get; set; }
        public DelegateCommand LastfmSignInCommand { get; set; }
        public DelegateCommand LastfmSignOutCommand { get; set; }
        public DelegateCommand CreateLastFmAccountCommand { get; set; }
        #endregion

        #region Properties
        public ObservableCollection<SearchProvider> SearchProviders
        {
            get { return this.searchProviders; }
            set
            {
                SetProperty<ObservableCollection<SearchProvider>>(ref this.searchProviders, value);
                this.SelectedSearchProvider = null;
            }
        }

        public SearchProvider SelectedSearchProvider
        {
            get { return this.selectedSearchProvider; }
            set
            {
                SetProperty<SearchProvider>(ref this.selectedSearchProvider, value);
                this.EditCommand.RaiseCanExecuteChanged();
                this.RemoveCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CheckBoxDownloadArtistInformationChecked
        {
            get { return this.checkBoxDownloadArtistInformationChecked; }
            set
            {
                SettingsClient.Set<bool>("Lastfm", "DownloadArtistInformation", value);
                SetProperty<bool>(ref this.checkBoxDownloadArtistInformationChecked, value);
            }
        }

        public bool CheckBoxDownloadLyricsChecked
        {
            get { return this.checkBoxDownloadLyricsChecked; }
            set
            {
                SettingsClient.Set<bool>("Lyrics", "DownloadLyrics", value);
                SetProperty<bool>(ref this.checkBoxDownloadLyricsChecked, value);
                this.eventAggregator.GetEvent<SettingDownloadLyricsChanged>().Publish(value);
            }
        }

        public bool IsLastFmSignedIn
        {
            get { return this.scrobblingService.SignInState == SignInState.SignedIn; }
        }

        public string LastFmUsername
        {
            get { return this.scrobblingService.Username; }
            set
            {
                this.scrobblingService.Username = value;
            }
        }

        public bool IsLastFmSigningIn
        {
            get { return this.isLastFmSignInInProgress; }
            set
            {
                SetProperty<bool>(ref this.isLastFmSignInInProgress, value);
            }
        }

        public bool IsLastFmSignInError
        {
            get { return this.scrobblingService.SignInState == SignInState.Error; }
        }
        #endregion

        #region Construction
        public SettingsOnlineViewModel(IUnityContainer container, IProviderService providerService, IDialogService dialogService, IScrobblingService scrobblingService, IEventAggregator eventAggregator)
        {
            this.container = container;
            this.providerService = providerService;
            this.dialogService = dialogService;
            this.scrobblingService = scrobblingService;
            this.eventAggregator = eventAggregator;

            this.scrobblingService.SignInStateChanged += (_) =>
            {
                this.IsLastFmSigningIn = false;
                OnPropertyChanged(() => this.IsLastFmSignedIn);
                OnPropertyChanged(() => this.LastFmUsername);
                OnPropertyChanged(() => this.IsLastFmSignInError);
            };

            this.AddCommand = new DelegateCommand(() => this.AddSearchProvider());
            this.EditCommand = new DelegateCommand(() => { this.EditSearchProvider(); }, () => { return this.SelectedSearchProvider != null; });
            this.RemoveCommand = new DelegateCommand(() => { this.RemoveSearchProvider(); }, () => { return this.SelectedSearchProvider != null; });
            this.LastfmSignInCommand = new DelegateCommand(async () =>
            {
                this.IsLastFmSigningIn = true;
                await this.scrobblingService.SignIn();

            });
            this.LastfmSignOutCommand = new DelegateCommand(() => this.scrobblingService.SignOut());
            this.CreateLastFmAccountCommand = new DelegateCommand(() => Actions.TryOpenLink(Constants.LastFmJoinLink));

            this.GetSearchProvidersAsync();

            this.providerService.SearchProvidersChanged += (_, __) => this.GetSearchProvidersAsync();

            this.GetCheckBoxesAsync();
        }
        #endregion

        #region Private
        private async void GetSearchProvidersAsync()
        {
            var providersList = await this.providerService.GetSearchProvidersAsync();
            var localProviders = new ObservableCollection<SearchProvider>();

            foreach (SearchProvider provider in providersList)
            {
                localProviders.Add(provider);
            }

            this.SearchProviders = localProviders;
        }

        private void AddSearchProvider()
        {
            SettingsOnlineAddEditSearchProvider view = this.container.Resolve<SettingsOnlineAddEditSearchProvider>();
            view.DataContext = this.container.Resolve<SettingsOnlineAddEditSearchProviderViewModel>(new DependencyOverride(typeof(SearchProvider), new SearchProvider()));

            string dialogTitle = ResourceUtils.GetStringResource("Language_Add");

            this.dialogService.ShowCustomDialog(
                0xe104,
                14,
                dialogTitle,
                view,
                450,
                0,
                false,
                true,
                true,
                true,
                ResourceUtils.GetStringResource("Language_Ok"),
                ResourceUtils.GetStringResource("Language_Cancel"),
                ((SettingsOnlineAddEditSearchProviderViewModel)view.DataContext).AddSearchProviderAsync);
        }

        private void EditSearchProvider()
        {
            SettingsOnlineAddEditSearchProvider view = this.container.Resolve<SettingsOnlineAddEditSearchProvider>();
            view.DataContext = this.container.Resolve<SettingsOnlineAddEditSearchProviderViewModel>(new DependencyOverride(typeof(SearchProvider), this.selectedSearchProvider));

            string dialogTitle = ResourceUtils.GetStringResource("Language_Edit");

            this.dialogService.ShowCustomDialog(
                0xe104,
                14,
                dialogTitle,
                view,
                450,
                0,
                false,
                true,
                true,
                true,
                ResourceUtils.GetStringResource("Language_Ok"),
                ResourceUtils.GetStringResource("Language_Cancel"),
                ((SettingsOnlineAddEditSearchProviderViewModel)view.DataContext).UpdateSearchProviderAsync);
        }

        private void RemoveSearchProvider()
        {
            if (this.dialogService.ShowConfirmation(0xe11b, 16, ResourceUtils.GetStringResource("Language_Remove"), ResourceUtils.GetStringResource("Language_Confirm_Remove_Online_Search_Provider").Replace("%provider%", this.selectedSearchProvider.Name), ResourceUtils.GetStringResource("Language_Yes"), ResourceUtils.GetStringResource("Language_No")))
            {
                var isRemoveSuccess = this.providerService.RemoveSearchProvider(this.selectedSearchProvider);

                if (!isRemoveSuccess)
                {
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetStringResource("Language_Error"),
                        ResourceUtils.GetStringResource("Language_Error_Removing_Online_Search_Provider").Replace("%provider%", this.selectedSearchProvider.Name),
                        ResourceUtils.GetStringResource("Language_Ok"),
                        true,
                        ResourceUtils.GetStringResource("Language_Log_File"));
                }
            }
        }

        private async void GetCheckBoxesAsync()
        {
            await Task.Run(() =>
            {
                this.CheckBoxDownloadArtistInformationChecked = SettingsClient.Get<bool>("Lastfm", "DownloadArtistInformation");
                this.CheckBoxDownloadLyricsChecked = SettingsClient.Get<bool>("Lyrics", "DownloadLyrics");
            });
        }
        #endregion
    }
}
