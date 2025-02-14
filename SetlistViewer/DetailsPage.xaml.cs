using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SetlistViewer
{
    public partial class DetailsPage : ContentPage
    {
        public ObservableCollection<SongDetailItem> SongDetails { get; set; } = new ObservableCollection<SongDetailItem>();
        private readonly SongData? Song;
        private string? _youtubeUrl;

        public DetailsPage(SongData song)
        {
            InitializeComponent();
            BindingContext = this;
            
            if (song == null)
            {
                DisplayAlert("Error", "Invalid song data. Returning to main page.", "OK");
                Navigation.PopAsync();
                return;
            }

            Song = song;

            try
            {
                lblArtist.Text = song.Artist ?? "Unknown Artist";
                lblTitle.Text = song.Name ?? "Unknown Title";

                SongDetails.Clear();
                foreach (var item in LoadSongDetails(song))
                {
                    SongDetails.Add(item);
                }
                OnPropertyChanged(nameof(SongDetails));  // Force UI to rebind
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Failed to load song:\n{ex.Message}", "OK");
            }
            ResizeFonts();
            PopulateSongDetails();
        }

        private void ResizeFonts()
        {
            lblArtist.FontSize = GetScaledFontSize(GetLabelFontSize(lblArtist));
            lblTitle.FontSize = GetScaledFontSize(GetLabelFontSize(lblTitle));
        }

        private void PopulateSongDetails()
        {
            detailsContainer.Children.Clear(); // Remove old content

            string GetFullRating(string rating) => rating switch
            {
                "SR" => "Supervision Recommended",
                "FF" => "Family Friendly",
                "M" => "Mature",
                "NR" => "Not Rated",
                _ => rating ?? """N/AbandonedMutexException"""
            };

            int rowIndex = 0;

            AddRow("Album:", Song.Album ?? "", rowIndex++);
            AddRow("Track Number:", Song.TrackNumber > 0 ? Song.TrackNumber.ToString() : "", rowIndex++);
            AddRow("Genre:", !string.IsNullOrEmpty(Song.Genre) ? Song.Genre : "", rowIndex++);
            AddRow("Subgenre:", !string.IsNullOrEmpty(Song.Subgenre) ? Song.Subgenre : "", rowIndex++);
            AddRow("Duration:", Song.Duration == "" || Song.Duration == "00:00" ? "" : Song.Duration, rowIndex++);
            AddRow("Rating:", GetFullRating(Song.Rating), rowIndex++);

            AddVocalPartsRow("Vocal Parts:", Song.VocalParts, rowIndex++);
            AddDifficultyRow("Drums Difficulty:", Song.DrumsDiff, rowIndex++);
            AddDifficultyRow("Bass Difficulty:", Song.BassDiff, rowIndex++);
            AddDifficultyRow("Pro Bass Difficulty:", Song.ProBassDiff, rowIndex++);
            AddDifficultyRow("Guitar Difficulty:", Song.GuitarDiff, rowIndex++);
            AddDifficultyRow("Pro Guitar Difficulty:", Song.ProGuitarDiff, rowIndex++);
            AddDifficultyRow("Keys Difficulty:", Song.KeysDiff, rowIndex++);
            AddDifficultyRow("Pro Keys Difficulty:", Song.ProKeysDiff, rowIndex++);
            AddDifficultyRow("Vocals Difficulty:", Song.VocalDiff, rowIndex++);
            AddDifficultyRow("Band Difficulty:", Song.BandDiff, rowIndex++);

            AddRow("Master Recording:", Song.Master ? "Yes" : "No", rowIndex++);
        }

        private void AddVocalPartsRow(string key, int vocalParts, int rowIndex)
        {
            var row = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
        },
                BackgroundColor = (rowIndex % 2 == 0) ? Colors.LightGray : Color.Parse("#E0F7FA"),
                HeightRequest = 50
            };

            var keyLabel = new Label
            {
                Text = key,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(10, 5)
            };

            string imagePath = vocalParts switch
            {
                1 => "mic1.png",
                2 => "mic2.png",
                3 => "mic3.png",
                _ => "micX.png"
            };

            var vocalImage = new Image
            {
                Source = imagePath,                
                HeightRequest = 36,
                Aspect = Aspect.AspectFit,
                VerticalOptions = LayoutOptions.Center
            };

            /*var valueLabel = new Label
            {
                Text = $"({vocalParts})",
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(5, 5, 10, 5)
            };*/

            row.Add(keyLabel, 0, 0);
            row.Add(vocalImage, 1, 0);
            //row.Add(valueLabel, 2, 0);

            detailsContainer.Children.Add(row);
        }


        private void AddRow(string key, string value, int rowIndex)
        {
            var row = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },  // Key (auto width)
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } // Value (fills remaining space)
        },
                BackgroundColor = (rowIndex % 2 == 0) ? Colors.LightGray : Color.Parse("#E0F7FA"),
                HeightRequest = 50
            };

            var keyLabel = new Label
            {
                Text = key,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(10, 5) // Small margin for alignment
            };

            var valueLabel = new Label
            {
                Text = value,
                FontSize = 16,
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(5, 5, 10, 5)
            };

            row.Add(keyLabel, 0, 0);
            row.Add(valueLabel, 1, 0);

            detailsContainer.Children.Add(row);
        }

        private void AddDifficultyRow(string key, string difficultyText, int rowIndex)
        {
            if (string.IsNullOrEmpty(difficultyText) || difficultyText == "N/A")
            {
                difficultyText = "No Part";
            }

            var row = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
        },
                BackgroundColor = (rowIndex % 2 == 0) ? Colors.LightGray : Color.Parse("#E0F7FA"),
                HeightRequest = 50
            };

            var keyLabel = new Label
            {
                Text = key,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(10, 5)
            };

            string imagePath = difficultyText switch
            {
                "No Part" => "diff0.png",
                "Warmup" => "diff1.png",
                "Apprentice" => "diff2.png",
                "Solid" => "diff3.png",
                "Moderate" => "diff4.png",
                "Challenging" => "diff5.png",
                "Nightmare" => "diff6.png",
                "Impossible" => "diff7.png",
                _ => null
            };

            var difficultyImage = new Image
            {
                Source = imagePath,
                WidthRequest = 90,
                HeightRequest = 30,
                Aspect = Aspect.AspectFit,
                VerticalOptions = LayoutOptions.Center
            };

            var valueLabel = new Label
            {
                Text = $"({difficultyText})",
                FontSize = 16,
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(5, 5, 10, 5)
            };

            row.Add(keyLabel, 0, 0);
            if (imagePath != null) row.Add(difficultyImage, 1, 0);
            if (!difficultyText.Equals("No Part") && imagePath != null)
            {
                row.Add(valueLabel, 2, 0);
            }

            detailsContainer.Children.Add(row);
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
        public async void LoadYouTubeVideo(string artist, string title)
        {
            _youtubeUrl = await GetYouTubeMusicVideoUrl(artist, title);

            if (!string.IsNullOrEmpty(_youtubeUrl))
            {
                imgYouTube.IsVisible = true;
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

        private async void OnYouTubeTapped(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_youtubeUrl))
            {
                Vibrate();
                try
                {                    
                    await Launcher.OpenAsync($"vnd.youtube:{_youtubeUrl.Replace("https://www.youtube.com/watch?v=", "")}");
                }
                catch
                {
                    await Launcher.OpenAsync(_youtubeUrl);
                }                
            }
        }
        private ObservableCollection<SongDetailItem> LoadSongDetails(SongData song)
        {
            var details = new ObservableCollection<SongDetailItem>();

            string GetFullRating(string rating) => rating switch
            {
                "SR" => "Supervision Recommended",
                "FF" => "Family Friendly",
                "M" => "Mature",
                "NR" => "Not Rated",
                _ => rating ?? "N/A"
            };

            var detailItems = new List<KeyValuePair<string, string>>
    {
        new ("Album:", song.Album ?? "N/A"),
        new ("Track Number:", song.TrackNumber > 0 ? song.TrackNumber.ToString() : "N/A"),
        new ("Genre:", !string.IsNullOrEmpty(song.Genre) ? song.Genre : "N/A"),
        new ("Subgenre:", !string.IsNullOrEmpty(song.Subgenre) ? song.Subgenre : "N/A"),
        new ("Duration:", !string.IsNullOrEmpty(song.Duration) ? song.Duration : "N/A"),
        new ("Rating:", GetFullRating(song.Rating)),
        new ("Vocal Parts:", song.VocalParts > 0 ? song.VocalParts.ToString() : "N/A"),
        new ("Drums Difficulty:", !string.IsNullOrEmpty(song.DrumsDiff) ? song.DrumsDiff : "N/A"),
        new ("Bass Difficulty:", !string.IsNullOrEmpty(song.BassDiff) ? song.BassDiff : "N/A"),
        new ("Pro Bass Difficulty:", !string.IsNullOrEmpty(song.ProBassDiff) ? song.ProBassDiff : "N/A"),
        new ("Guitar Difficulty:", !string.IsNullOrEmpty(song.GuitarDiff) ? song.GuitarDiff : "N/A"),
        new ("Pro Guitar Difficulty:", !string.IsNullOrEmpty(song.ProGuitarDiff) ? song.ProGuitarDiff : "N/A"),
        new ("Keys Difficulty:", !string.IsNullOrEmpty(song.KeysDiff) ? song.KeysDiff : "N/A"),
        new ("Pro Keys Difficulty:", !string.IsNullOrEmpty(song.ProKeysDiff) ? song.ProKeysDiff : "N/A"),
        new ("Vocals Difficulty:", !string.IsNullOrEmpty(song.VocalDiff) ? song.VocalDiff : "N/A"),
        new ("Band Difficulty:", !string.IsNullOrEmpty(song.BandDiff) ? song.BandDiff : "N/A"),
        new ("Master Recording:", song.Master ? "Yes" : "No")
    };

            for (int i = 0; i < detailItems.Count; i++)
            {
                details.Add(new SongDetailItem
                {
                    RowIndex = i,
                    Key = detailItems[i].Key,
                    Value = detailItems[i].Value
                });
            }

            return details;
        }


        private async void OnLyricsTapped(object sender, EventArgs e)
        {
            if (imgLyrics.BindingContext is string lyrics)
            {
                Vibrate();
                await Navigation.PushAsync(new LyricsPage(Song, lyrics, imgCover.Source));                
            }
        }

        private async void OnImageTapped(object sender, EventArgs e)
        {
            if (imgCover.Source != null)
            {
                Vibrate();
                await Navigation.PushModalAsync(new ImagePopupPage(imgCover.Source));                
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadLyricsAsync(Song.Artist, Song.Name);
            LoadYouTubeVideo(Song.Artist, Song.Name);

            if (!string.IsNullOrWhiteSpace(Song.Artist) && !string.IsNullOrWhiteSpace(Song.Album))
            {
                LoadAlbumArt(Song.Artist, Song.Album);
            }
        }

        private async void LoadLyricsAsync(string artist, string title)
        {
            string lyrics = await GetLyricsAsync(artist, title);
            if (!string.IsNullOrEmpty(lyrics))
            {
                imgLyrics.IsVisible = true;
                imgLyrics.BindingContext = lyrics;
            }
        }

        public async Task<string> GetLyricsAsync(string artist, string title)
        {
            string apiUrl = $"https://api.lyrics.ovh/v1/{Uri.EscapeDataString(artist)}/{Uri.EscapeDataString(title)}";

            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var lyricsData = JsonSerializer.Deserialize<LyricsResponse>(json);
                return lyricsData?.Lyrics ?? string.Empty;
            }

            return string.Empty;
        }

        private async void LoadAlbumArt(string artist, string album)
        {
            string imageUrl = await FetchAlbumArtAsync(artist, album);

            if (!string.IsNullOrEmpty(imageUrl))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    imgCover.Source = ImageSource.FromUri(new Uri(imageUrl));
                });
            }
            else
            {                
                imgCover.Source = "default_cover.png";
            }
        }

        public async Task<string> GetYouTubeMusicVideoUrl(string artist, string title)
        {
            try
            {
                string apiKey = "AIzaSyCXot_3Xst_jWQ94gB0P2ZBRMg3z52SG2Y"; //please get your own
                string query = Uri.EscapeDataString($"{artist} {title} music");
                string url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&q={query}&key={apiKey}";

                using HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(response);
                JsonElement root = doc.RootElement;

                string videoId = root.GetProperty("items")[0].GetProperty("id").GetProperty("videoId").GetString();
                return $"https://www.youtube.com/watch?v={videoId}";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"❌ YouTube API Error: {ex.Message}", "OK");
                return null;
            }
        }

        private async Task<string> FetchAlbumArtAsync(string artist, string album)
        {
            try
            {
                string apiKey = "6b579e4522f9846c88cf88b6a44564f5"; //please get your own
                string url = $"https://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={apiKey}&artist={Uri.EscapeDataString(artist)}&album={Uri.EscapeDataString(album)}&format=json";

                using HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(response);
                JsonElement root = doc.RootElement;

                var images = root.GetProperty("album").GetProperty("image").EnumerateArray();

                string imageUrl = null;
                foreach (var image in images)
                {
                    if (image.GetProperty("size").GetString() == "mega")
                    {
                        imageUrl = image.GetProperty("#text").GetString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(imageUrl))
                {
                    imageUrl = images.Last().GetProperty("#text").GetString();
                }

                return string.IsNullOrEmpty(imageUrl) ? null : imageUrl;
            }
            catch (Exception ex)
            {
                //await DisplayAlert("Debugging", $"Failed to fetch album art: {ex.Message}", "OK");
                imgCover.Source = "default_cover.png";
                return null;
            }
        }
    }

    public class LyricsResponse
    {
        [JsonPropertyName("lyrics")]
        public string Lyrics { get; set; }
    }

    public class SongDetailItem
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public int RowIndex { get; set; }
    }
}
