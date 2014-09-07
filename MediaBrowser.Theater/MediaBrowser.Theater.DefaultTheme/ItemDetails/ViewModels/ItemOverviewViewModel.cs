﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Theater.Api.Library;
using MediaBrowser.Theater.Api.Navigation;
using MediaBrowser.Theater.Api.Playback;
using MediaBrowser.Theater.Api.Session;
using MediaBrowser.Theater.Api.UserInterface;
using MediaBrowser.Theater.DefaultTheme.Core.ViewModels;
using MediaBrowser.Theater.Presentation.Controls;
using MediaBrowser.Theater.Presentation.ViewModels;
using MediaBrowser.Theater.DefaultTheme.ItemList;

namespace MediaBrowser.Theater.DefaultTheme.ItemDetails.ViewModels
{
    public class ItemOverviewViewModel
        : BaseViewModel, IItemDetailSection, IKnownSize
    {
        private readonly BaseItemDto _item;

        public ItemArtworkViewModel PosterArtwork { get; set; }
        public ItemArtworkViewModel BackgroundArtwork { get; set; }
        public ItemInfoViewModel Info { get; set; }

        public ICommand PlayCommand { get; set; }
        public ICommand EnqueueCommand { get; set; }
        public bool CanPlay { get; set; }

        public ICommand PlayAllCommand { get; set; }
        public ICommand EnqueueAllCommand { get; set; }
        public bool CanPlayAll { get; set; }

        public ICommand ResumeCommand { get; set; }

        public ICommand BrowseAllCommand { get; set; }

        public int SortOrder
        {
            get { return 0; }
        }

        public bool ShowInfo
        {
            get { return (!_item.IsFolder && _item.Type != "Person") || !string.IsNullOrEmpty(_item.Overview); }
        }

        public ItemOverviewViewModel(BaseItemDto item, IApiClient apiClient, IImageManager imageManager, IPlaybackManager playbackManager, ISessionManager sessionManager, INavigator navigator)
        {
            _item = item;

            Info = new ItemInfoViewModel(item) {
                ShowDisplayName = false,
                ShowParentText = false
            };

            PosterArtwork = new ItemArtworkViewModel(item, apiClient, imageManager) { DesiredImageHeight = 700 };

            if (item.Type == "Episode")
                PosterArtwork.PreferredImageTypes = new[] { ImageType.Screenshot, ImageType.Art, ImageType.Primary };

            PosterArtwork.PropertyChanged += (s, e) => {
                if (e.PropertyName == "Size") {
                    OnPropertyChanged("Size");
                }
            };

            BackgroundArtwork = new ItemArtworkViewModel(item, apiClient, imageManager) {
                DesiredImageWidth = 800,
                PreferredImageTypes = new[] { ImageType.Backdrop, ImageType.Art, ImageType.Banner, ImageType.Screenshot, ImageType.Primary }
            };

            PlayCommand = new RelayCommand(o => playbackManager.Play(new PlayOptions(item) { GoFullScreen = true, EnableCustomPlayers = true, Resume = false }));
            ResumeCommand = new RelayCommand(o => playbackManager.Play(new PlayOptions(item) { GoFullScreen = true, EnableCustomPlayers = true, Resume = true }));
            PlayAllCommand = new RelayCommand(async o => {
                var items = await ItemChildren.GetChildren(item, apiClient, sessionManager, recursive: true, fields: new[] { ItemFields.MediaSources }, includeItemTypes: new[] { "Movie", "Episode", "Audio" } );
                if (items.Items.Length > 0) {
                    await playbackManager.Play(new PlayOptions(items.Items) { EnableCustomPlayers = true, GoFullScreen = true });
                }
            });

            BrowseAllCommand = new RelayCommand(o => navigator.Navigate(Go.To.ItemList(new ItemListParameters {
                Items = ItemChildren.GetChildren(item, apiClient, sessionManager, true),
                Title = item.Name
            })));
        }

        public Size Size
        {
            get
            {
                if (ShowInfo)
                    return new Size(800 + 20 + 250, 700);

                var artWidth = Math.Min(1200, PosterArtwork.ActualWidth);
                return new Size(artWidth + 20 + 250, 700);
            }
        }
    }

    public class ItemOverviewSectionGenerator
        : IItemDetailSectionGenerator
    {
        private readonly IApiClient _apiClient;
        private readonly IImageManager _imageManager;
        private readonly IPlaybackManager _playbackManager;
        private readonly ISessionManager _sessionManager;
        private readonly INavigator _navigator;

        public ItemOverviewSectionGenerator(IApiClient apiClient, IImageManager imageManager, IPlaybackManager playbackManager, ISessionManager sessionManager, INavigator navigator)
        {
            _apiClient = apiClient;
            _imageManager = imageManager;
            _playbackManager = playbackManager;
            _sessionManager = sessionManager;
            _navigator = navigator;
        }

        public bool HasSection(BaseItemDto item)
        {
            return item != null;
        }

        public Task<IEnumerable<IItemDetailSection>> GetSections(BaseItemDto item)
        {
            IItemDetailSection section = new ItemOverviewViewModel(item, _apiClient, _imageManager, _playbackManager, _sessionManager, _navigator);
            return Task.FromResult<IEnumerable<IItemDetailSection>>(new[] { section });
        }
    }
}