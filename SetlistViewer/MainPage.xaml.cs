using System.Collections.Generic;
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
        private List<SongData> _allSongs = new List<SongData> { };
        public ICommand SortCommand { get; }
        private static readonly string queueFile = Path.Combine(FileSystem.AppDataDirectory, "queue.dat");
        private ObservableCollection<SongData>? queueList { get; set; }
        private List<SongData> loadedSongs = new List<SongData> { };

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
            if (Songs.Count == 0) return;

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

        private async Task LoadJSON(string jsonPath)
        {
            using var stream = File.OpenRead(jsonPath);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var setlistData = JsonSerializer.Deserialize<SetlistWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            loadedSongs = setlistData.Setlist;            
        }

        public string GetConfigString(string raw_line)
        {
            if (string.IsNullOrWhiteSpace(raw_line)) return "";
            var line = raw_line;
            try
            {
                var index = line.IndexOf("=", StringComparison.Ordinal) + 1;
                line = line.Substring(index, line.Length - index);
            }
            catch (Exception)
            {
                line = "";
            }
            return line.Trim();
        }

        private static string RemoveControlCharsFromString(string line)
        {
            return new string(line.Where(c => !char.IsControl(c)).ToArray());
        }

        private string ConvertRatingToString(short rating)
        {
            return rating switch
            {
                1 => "FF",
                2 => "SR",
                3 => "M",
                4 => "NR",
                _ => "NR" // Default to "NR" if out of range
            };
        }

        private string ConvertMillisecondsToDuration(long milliseconds)
        {
            TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";

        }
        private async Task LoadSetlist(string setlistPath)
        {
            var SongsGrabbed = new List<SongData>();

            var line = "";
            var linenum = 5;
            var sr = new StreamReader(setlistPath, System.Text.Encoding.UTF8);
            try
            {
                int songcount;
                string format;
                sr.ReadLine();
                sr.ReadLine();
                songcount = Convert.ToInt16(GetConfigString(sr.ReadLine()));
                sr.ReadLine();
                format = GetConfigString(sr.ReadLine()).ToLowerInvariant();

                for (var i = 0; i < songcount; i++)
                {
                    try
                    {
                        SongsGrabbed.Add(new SongData());
                        var index = SongsGrabbed.Count - 1;

                        //all Setlist Manager cache formats
                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].Artist = RemoveControlCharsFromString(GetConfigString(line));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].Name = RemoveControlCharsFromString(GetConfigString(line));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].Album = RemoveControlCharsFromString(GetConfigString(line));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].TrackNumber = Convert.ToInt32(GetConfigString(line));
                        if (SongsGrabbed[index].TrackNumber == 65535) //Clone Hero bug???
                        {
                            SongsGrabbed[index].TrackNumber = 1;
                        }

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].Master = line.Contains("True");

                        line = sr.ReadLine();
                        linenum++;
                        var year = Convert.ToInt16(GetConfigString(line));
                        SongsGrabbed[index].YearRecorded = year < 0 || year > 2100 ? 0 : year;

                        line = sr.ReadLine();
                        linenum++;
                        year = Convert.ToInt16(GetConfigString(line));
                        SongsGrabbed[index].YearReleased = year < 0 || year > 2100 ? 0 : year;

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].Genre = GetConfigString(line);

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].Rating = ConvertRatingToString(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].Gender = GetConfigString(line);

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].VocalParts = Convert.ToInt16(GetConfigString(line));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].DrumsDiff = SongsGrabbed[index].GetInstrumentDifficultyName(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].BassDiff = SongsGrabbed[index].GetInstrumentDifficultyName(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].ProBassDiff = SongsGrabbed[index].GetInstrumentDifficultyName(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].GuitarDiff = SongsGrabbed[index].GetInstrumentDifficultyName(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].ProGuitarDiff = SongsGrabbed[index].GetInstrumentDifficultyName(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].KeysDiff = SongsGrabbed[index].GetInstrumentDifficultyName(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].ProKeysDiff = SongsGrabbed[index].GetInstrumentDifficultyName(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].VocalDiff = SongsGrabbed[index].GetInstrumentDifficultyName(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].BandDiff = SongsGrabbed[index].GetInstrumentDifficultyName(Convert.ToInt16(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].Duration = ConvertMillisecondsToDuration(Convert.ToInt64(GetConfigString(line)));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].ShortName = GetConfigString(line);

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].SongId = Convert.ToInt32(GetConfigString(line));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].Source = GetConfigString(line);
                        if (string.IsNullOrWhiteSpace(SongsGrabbed[index].Source))
                        {
                            SongsGrabbed[index].Source = "dlc";
                        }

                        if (!format.Contains("2") && !format.Contains("3") && !format.Contains("4") && !format.Contains("5")) continue;
                        //Setlist Manager cache format 2-4, both RB3 and Blitz
                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].FilePath = GetConfigString(line);

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].PreviewStart = Convert.ToInt32(GetConfigString(line));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].PreviewEnd = Convert.ToInt32(GetConfigString(line));

                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].GameVersion = Convert.ToInt16(GetConfigString(line));

                        if (!format.ToLowerInvariant().Contains("blitz"))
                        {
                            //Setlist Manager cache format 2-4, only RB3 
                            line = sr.ReadLine();
                            linenum++;
                            SongsGrabbed[index].ScrollSpeed = Convert.ToInt16(GetConfigString(line));

                            line = sr.ReadLine();
                            linenum++;
                            SongsGrabbed[index].TonicNote = Convert.ToInt16(GetConfigString(line));

                            line = sr.ReadLine();
                            linenum++;
                            SongsGrabbed[index].Tonality = Convert.ToInt16(GetConfigString(line));

                            line = sr.ReadLine();
                            linenum++;
                            SongsGrabbed[index].PercussionBank = GetConfigString(line);

                            line = sr.ReadLine();
                            linenum++;
                            SongsGrabbed[index].DrumBank = GetConfigString(line);
                        }

                        if (!format.Contains("3") && !format.Contains("4") && !format.Contains("5")) continue;
                        //Setlist Manager cache format 3-4, both RB3 and Blitz
                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].DoNotExport = line.Contains("True");

                        if (!format.Contains("4") && !format.Contains("5")) continue;
                        if (!format.ToLowerInvariant().Contains("blitz"))
                        {
                            //Setlist Manager cache format 4-5, only RB3
                            line = sr.ReadLine();
                            linenum++;
                            SongsGrabbed[index].ProBassTuning = GetConfigString(line);

                            //Setlist Manager cache format 4-5, only RB3
                            line = sr.ReadLine();
                            linenum++;
                            SongsGrabbed[index].ProGuitarTuning = GetConfigString(line);
                        }

                        if (!format.Contains("5")) continue;
                        //Setlist Manager cache format 5, RB3 and Blitz
                        line = sr.ReadLine();
                        linenum++;
                        SongsGrabbed[index].SongLink = GetConfigString(line);

                        //add further checks for newer cache versions here
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", "There was a problem loading song #" + (i + 1) + " in Setlist file:\n'" + Path.GetFileName(setlistPath) + "'\n\nThe error says:\n'" +
                            ex.Message + "'\n\nLine:\t'" + line + "'\nLine #:\t" + linenum + "\n\nSkipping this song...", "OK");
                        SongsGrabbed.RemoveAt(SongsGrabbed.Count - 1);

                        //calculate how many lines until next song, then read/skip those lines
                        var lines = (5 + ((i + 1) * 34)) - linenum;
                        for (var x = 0; x < lines; x++)
                        {
                            sr.ReadLine();
                        }
                    }
                }
                sr.Dispose();                
            }
            catch (Exception ex)
            {
                sr.Dispose();
                SongsGrabbed.Clear();
                await DisplayAlert("Error", "There was a problem loading Setlist '" + Path.GetFileName(setlistPath) + "'\n\nThe error says:\n'" +
                                ex.Message + "'\n\nTry re-importing if this problem continues", "OK");                
            }

            loadedSongs = SongsGrabbed;
        }

        private async void LoadSetlistFromFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return;                               

                if (Path.GetExtension(filePath) == ".json")
                {
                    await LoadJSON(filePath);
                }
                else if (Path.GetExtension(filePath) == ".setlist")
                {
                    await LoadSetlist(filePath);
                }
                else
                {
                    await DisplayAlert("Invalid Selection", "That's not a valid .setlist or .json file, try again", "OK");
                    return;
                }
                
                if (loadedSongs != null && loadedSongs.Count > 0)
                {
                    //await DisplayAlert("Debugging", $"Loaded {setlistDataCount} songs", "OK");
                    
                    try
                    {      
                        var tempSongs = await Task.Run(() =>
                        {
                            List<SongData> temp = new List<SongData>();
                            int index = 0;
                            foreach (var song in loadedSongs)
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
                                _allSongs = tempSongs;
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
                    await DisplayAlert("Error", "No valid song data found in that file", "OK");
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
                var allFilesType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "*/*" } }, // Allow all file types
                    { DevicePlatform.iOS, new[] { "public.item" } }, // Allow all file types
                    { DevicePlatform.WinUI, new[] { "*" } } // Windows: "*" allows all extensions
                });

                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a .setlist or .json file",
                    FileTypes = allFilesType
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
            var message = "This is a companion app to Nautilus' Setlist Manager\n\nYou can either work with your existing .setlist file (which contains" +
                " everything Setlist Manager has for your Setlist) or you can export your Setlist to JSON using the 'EVERYTHING'" +
                " and 'Use tier names' settings, and (somehow) get that .setlist or .json file to your device\n\nClick the load file icon to find and load " +
                "your setlist file\n\nSingle tap on a song to open the Song Details page\nDouble tap on a song to add it to the queue" +
                "\n\nIn the Song Details page, Setlist Viewer will try to download album art and lyrics for your song - if they're available, " +
                "the album art will load and a 'Lyrics' label will appear, click on that to view the song lyrics in the Lyrics page\nClick on the album" +
                " art to view it full screen\nSetlist Viewer will also try to find a matching YouTube video and if it finds one, it will display" +
                " a link so you can open it on the YouTube app\n\nBack on the Main page, click on the queue icon to open the Queue page - see " +
                "that question mark icon for further instructions" +
                "\nClick on the search icon to search through Artists and Song Titles and filter by your search pattern\nClick on the red X icon to" +
                " reset your search filter and show all songs available\nYou can sort the songs displayed by clicking on the headers, click once " +
                "for Ascending order and click again for Descending order" +
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
