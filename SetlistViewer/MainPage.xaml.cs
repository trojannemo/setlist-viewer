using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;

namespace SetlistViewer
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<SongData> Songs { get; set; }
        private bool isAscending = true;
        private int backPressCount = 0;
        private string _searchQuery = "";
        private List<SongData> _allSongs = new();
        public ICommand SortCommand { get; }
        private static readonly string queueFile = Path.Combine(FileSystem.AppDataDirectory, "queue.dat");
        private ObservableCollection<SongData>? queueList { get; set; }

        public MainPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            BindingContext = this;
           
            Songs = new ObservableCollection<SongData>();

            string lastSetlistPath = Preferences.Get("LastSetlistPath", string.Empty);
            if (!string.IsNullOrEmpty(lastSetlistPath) && File.Exists(lastSetlistPath))
            {
                LoadSetlistFromFile(lastSetlistPath);
            }

            if (!string.IsNullOrEmpty(queueFile) && File.Exists(queueFile))
            {
                LoadQueueFromFile();
            }

            SortCommand = new Command<string>(SortSongs);
            ResizeFonts();
        }

        private void ResizeFonts()
        {
            lblSetlistViewer.FontSize = GetScaledFontSize(GetLabelFontSize(lblSetlistViewer));
            lblFileName.FontSize = GetScaledFontSize(GetLabelFontSize(lblFileName));
            lblSongCount.FontSize = GetScaledFontSize(GetLabelFontSize(lblSongCount));
            lblSearchActive.FontSize = GetScaledFontSize(GetLabelFontSize(lblSearchActive));            
            lblHeaderArtist.FontSize = GetScaledFontSize(GetLabelFontSize(lblHeaderArtist));
            lblHeaderTitle.FontSize = GetScaledFontSize(GetLabelFontSize(lblHeaderTitle));
            lblHeaderLength.FontSize = GetScaledFontSize(GetLabelFontSize(lblHeaderLength));
            lblHeaderRating.FontSize = GetScaledFontSize(GetLabelFontSize(lblHeaderRating));
        }

          public static double GetScaledFontSize(double baseSize)
        {
            double screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            double scaleFactor = screenWidth / 470; // 470px is an arbitrary reference width

            return Math.Min(baseSize * scaleFactor, baseSize * 1.00);
        }

        public static double GetLabelFontSize(Label label)
        {
            double defaultFontSize = (double)Label.FontSizeProperty.DefaultValue;
            return label.FontSize > 0 ? label.FontSize : defaultFontSize;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = e.NewTextValue.ToLower();
            FilterSongs();
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            string result = await DisplayPromptAsync("Filter", "Enter artist or song name to filter by:");

            if (!string.IsNullOrWhiteSpace(result))
            {
                _searchQuery = result.ToLower();
                FilterSongs();
            }
        }

        private void OnClearSearchClicked(object sender, EventArgs e)
        {
            _searchQuery = "";
            FilterSongs();
            lblSearchActive.IsVisible = false;
            btnClearSearch.IsVisible = false;
        }

        private void FilterSongs()
        {
            var index = 0;
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                Songs.Clear();
                
                foreach (var song in _allSongs)
                {
                    index++;
                    song.SongIndex = index;
                    Songs.Add(song);
                }                    

                lblSearchActive.IsVisible = false;
                btnClearSearch.IsVisible = false;
            }
            else
            {                
                var filtered = _allSongs
                    .Where(s => s.Name.ToLower().Contains(_searchQuery.ToLower().Trim()) || s.Artist.ToLower().Contains(_searchQuery.ToLower().Trim()))
                    .ToList();

                Songs.Clear();
                foreach (var song in filtered)
                {
                    index++;
                    song.SongIndex = index;
                    Songs.Add(song);
                }

                lblSearchActive.Text = $"{filtered.Count} song(s)";
                lblSearchActive.IsVisible = true;
                btnClearSearch.IsVisible = true;
            }
        }

        private void SortSongs(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy) || Songs.Count == 0)
                return;

            isAscending = !isAscending;

            var sortedList = Songs.OrderBy(s => GetSortValue(s, sortBy)).ToList();
            if (!isAscending)
                sortedList.Reverse();

            Songs.Clear();
            var index = 0;
            foreach (var song in sortedList)
            {
                index++;
                song.SongIndex = index;
                Songs.Add(song);
            }
        }

        private object GetSortValue(SongData song, string sortBy)
        {
            return sortBy switch
            {
                "Artist" => song.Artist,
                "Name" => song.Name,
                "Duration" => ConvertDurationToSeconds(song.Duration),
                "Rating" => song.Rating,
                _ => song.Name
            };
        }
               
        private int ConvertDurationToSeconds(string duration)
        {
            if (TimeSpan.TryParseExact(duration, @"m\:ss", null, out TimeSpan time))
            {
                return (int)time.TotalSeconds;
            }
            return 0;
        }

        private async void LoadSetlistFromFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return;

                using var stream = File.OpenRead(filePath);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var setlistData = JsonSerializer.Deserialize<SetlistWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (setlistData?.Setlist != null)
                {
                    //await DisplayAlert("Debugging", $"Loaded {setlistData.Setlist.Count} songs", "OK");
                    
                    try
                    {      
                        var tempSongs = await Task.Run(() =>
                        {
                            List<SongData> temp = new List<SongData>();
                            int index = 0;
                            foreach (var song in setlistData.Setlist)
                            {
                                song.SongIndex = index++;
                                temp.Add(song);
                            }
                            return temp;
                        });
                                                
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                listViewSetlist.ItemsSource = null;
                                _allSongs = setlistData.Setlist;
                                Songs.Clear();
                                foreach (var song in tempSongs)
                                {
                                    Songs.Add(song);
                                }
                                
                                lblFileName.Text = Path.GetFileNameWithoutExtension(filePath);
                                lblSongCount.Text = $"Songs: {Songs.Count}";
                                listViewSetlist.ItemsSource = Songs;
                            }
                            catch (Exception uiEx)
                            {
                                DisplayAlert("Error", $"Failed to update UI:\n{uiEx.Message}", "OK");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to load songs:\n{ex.Message}", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Error", "No valid song data found in that JSON file", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load file: {ex.Message}", "OK");
            }
        }       

        public async void OnSingleTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is SongData song)
            {
                if (song == null)
                {
                    //await DisplayAlert("Debugging", "Single-tap detected, but song is null", "OK");
                    return;
                }

                //await DisplayAlert("Debugging", $"Single-tapped song: {song.Name}", "OK");
                Vibrate();
                await Navigation.PushAsync(new DetailsPage(song));                
            }                       
        }

        private void Vibrate()
        {
            try
            {
                HapticFeedback.Perform(HapticFeedbackType.Click);
            }
            catch (FeatureNotSupportedException)
            {
                Console.WriteLine("⚠ Haptic feedback not supported on this device.");
            }
        }

        private async void AddToQueue(SongData song)
        {
            await LoadQueueFromFile();
            queueList.Add(song);
            await SaveQueueToFile();
        }

        private async Task SaveQueueToFile()
        {
            try
            {
                string json = JsonSerializer.Serialize(queueList);
                await File.WriteAllTextAsync(queueFile, json);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save queue: {ex.Message}", "OK");
            }
        }

        private async Task LoadQueueFromFile()
        {
            try
            {
                if (File.Exists(queueFile))
                {
                    string json = await File.ReadAllTextAsync(queueFile);
                    queueList = JsonSerializer.Deserialize<ObservableCollection<SongData>>(json) ?? new ObservableCollection<SongData>();
                }
            }
            catch
            {
                queueList = new ObservableCollection<SongData>();
            }
        }

        private async void OnQueueClicked(object sender, EventArgs e)
        {
            if (queueList == null)
            {
                queueList = new ObservableCollection<SongData>();
            }
            await Navigation.PushAsync(new QueuePage(queueList));
        }

        private async void OnLoadSetlistClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a Setlist JSON file",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/json", ".json" } },
                { DevicePlatform.iOS, new[] { "public.json" } }
            })
                });

                if (result == null) return;

                Preferences.Set("LastSetlistPath", result.FullPath);

                LoadSetlistFromFile(result.FullPath);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"File selection failed: {ex.Message}", "OK");
            }
        }

        private async void OnAboutClicked(object sender, EventArgs e)
        {
            var message = "Using Nautilus, export your Setlist to JSON using the 'EVERYTHING'" +
                " and 'Use tier names' settings, and (somehow) get that JSON file to your device\n\nClick 'Load' to find and load your setlist" +
                " JSON file\n\nSingle tap on a song to open the Song Details page\nDouble tap on a song to add it to the queue" +
                "\n\nIn the Song Details page, Setlist Viewer will try to download album art and lyrics for your song - if they're available, " +
                "the album art will show and a 'Lyrics' label will appear, click on that to view the song lyrics in the Lyrics page\nClick on the album" +
                " art to view it full screen\nSetlist Viewer will also try to find a matching YouTube video and if it finds out, will display" +
                " a link so you can click to open on the YouTube app\n\nBack on the Main page, click on 'Queue' to open the Queue page - see that About button for instructions" +
                "\nClick on 'Search' to search through Artists and Song Titles and filter by your search pattern\nClick on 'Clear' to reset your filter" +
                " and show all songs\nYou can sort the information displayed by clicking on the headers, click once for Ascending order and click " +
                "again for Descending order" +
                "\n\nAlbum Art API by last.fm" +
                "\nLyrics API by Lyrics.ovh" +
                "\nYouTube API by Google" +
                "\n\nDeveloped by Nemo © 2025";
            Vibrate();
            await Navigation.PushAsync(new AboutPage(message));            
        }
                
        private async void OnHeaderTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is string column)
            {
                //await DisplayAlert("Debugging", $"Tapped on header: {column}", "OK");
                SortList(column);
            }
        }

        private void SortList(string column)
        {
            if (Songs.Count == 0) return;
            if (column == "Title")
            {
                column = "Name";
            }

            isAscending = !isAscending;

            var sortedList = Songs.OrderBy(s => GetSortValue(s, column)).ToList();
            if (!isAscending)
                sortedList.Reverse();

            Songs.Clear();
            var index = 0;
            foreach (var song in sortedList)
            {
                index++;
                song.SongIndex = index;
                Songs.Add(song);
            }
        }

        public async void OnDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is Grid grid && grid.BindingContext is SongData song)
            {
                if (song == null)
                {
                    await DisplayAlert("Debugging", "⚠ OnDoubleTapped received null data!", "OK");
                    return;
                }
                else
                {
                    //await DisplayAlert("Debugging", $"OnDoubleTapped received: {song?.Artist} - {song?.Name}", "OK");
                    AddToQueue(song);
                    Vibrate();
                    await Toast.Make("Added song to queue", CommunityToolkit.Maui.Core.ToastDuration.Short).Show();
                }                               
            }            
        }

        protected override bool OnBackButtonPressed()
        {
            if (backPressCount == 0)
            {
                backPressCount++;
                Toast.Make("Press back again to exit",CommunityToolkit.Maui.Core.ToastDuration.Short).Show();
                Task.Delay(2000).ContinueWith(_ => backPressCount = 0);
                return true;
            }

            return base.OnBackButtonPressed();
        }
    }
}
