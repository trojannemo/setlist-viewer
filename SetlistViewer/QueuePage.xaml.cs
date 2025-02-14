using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace SetlistViewer
{
    public partial class QueuePage : ContentPage
    {
        private static readonly string queueFile = Path.Combine(FileSystem.AppDataDirectory, "queue.dat");
        private ObservableCollection<SongData> queueList { get; set; }

        public QueuePage(ObservableCollection<SongData> queue)
        {
            InitializeComponent();
            if (queue == null)
            {
                queue = new ObservableCollection<SongData>();
            }
            queueList = queue;
            LoadQueueFromFile();
            UpdateRowIndexes();
            ResizeFonts();
        }

        private void ResizeFonts()
        {
            lblQueue.FontSize = GetScaledFontSize(GetLabelFontSize(lblQueue));
            lblQueueCount.FontSize = GetScaledFontSize(GetLabelFontSize(lblQueueCount));
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

        private void UpdateRowIndexes()
        {
            if (queueList == null || queueList.Count == 0) return;
            for (int i = 0; i < queueList.Count; i++)
            {
                queueList[i].SongIndex = i;
            }
        }

        private void RefreshQueue()
        {
            lblQueueCount.Text = $"Songs: {queueList.Count}";
            QueueCollectionView.ItemsSource = null;
            UpdateRowIndexes();
            QueueCollectionView.ItemsSource = queueList;
            SaveQueueToFile();
        }

        private async void LoadQueueFromFile()
        {
            try
            {
                if (File.Exists(queueFile))
                {
                    string json = await File.ReadAllTextAsync(queueFile);
                    queueList = JsonSerializer.Deserialize<ObservableCollection<SongData>>(json) ?? new ObservableCollection<SongData>();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load queue: {ex.Message}", "OK");
                queueList = new ObservableCollection<SongData>();
            }
            RefreshQueue();
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

        private string FormatQueueForSharing()
        {
            if (queueList == null || !queueList.Any())
                return "Song queue is empty";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🎵 Song Queue 🎵");
            sb.AppendLine("==================");

            foreach (var song in queueList)
            {
                sb.AppendLine($"• {song.Artist} - {song.Name} ({song.Duration})");
            }

            return sb.ToString();
        }

        private async void OnCopyClicked(object sender, EventArgs e)
        {
            string text = FormatQueueForSharing();
            if (queueList == null || !queueList.Any())
            {
                await DisplayAlert("Setlist Viewer", text, "OK");
                return;
            }            
            await Clipboard.Default.SetTextAsync(text);

        }
        private async Task ShareQueueAsync()
        {
            string shareText = FormatQueueForSharing();
            if (queueList == null || !queueList.Any())
            {
                await DisplayAlert("Setlist Viewer", shareText, "OK");
                return;
            }
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = shareText,
                Title = "Share Song Queue"
            });
        }

        private async void OnShareClicked(object sender, EventArgs e)
        {
            await ShareQueueAsync();
        }

        public async void OnDoubleTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is SongData song)
            {
                if (song == null)
                {
                    await DisplayAlert("Debugging", "Double-tap detected, but song is null", "OK");
                    return;
                }

                //await DisplayAlert("Debugging", $"Double-tapped song: {song.Name}", "OK");
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

        public async void OnSingleTapped(object sender, TappedEventArgs e)
        {
            Vibrate();

            if (e.Parameter is SongData song)
            {
                if (song == null)
                {
                    await DisplayAlert("Debugging", "Double-tap detected, but song is null", "OK");
                    return;
                }

                //await DisplayAlert("Debugging", $"Double-tapped song: {song.Name}", "OK");
                Vibrate();
                string action = await DisplayActionSheet(
                        "Queue actions:",
                        "Cancel",
                        null,
                        "Move up", "Move to top", "Move down", "Move to bottom", "Remove");

                HandleQueueAction(action, song);
            }
        }
               
        private void HandleQueueAction(string action, SongData song)
        {
            if (string.IsNullOrEmpty(action) || action == "Cancel") return;

            int index = queueList.IndexOf(song);
            if (index == -1) return;

            switch (action)
            {
                case "Move up":
                    if (index > 0)
                    {
                        queueList.Move(index, index - 1);
                    }
                    break;

                case "Move to top":
                    if (index > 0)
                    {
                        queueList.Move(index, 0);
                    }
                    break;

                case "Move down":
                    if (index < queueList.Count - 1)
                    {
                        queueList.Move(index, index + 1);
                    }
                    break;

                case "Move to bottom":
                    if (index < queueList.Count - 1)
                    {
                        queueList.Move(index, queueList.Count - 1);
                    }
                    break;

                case "Remove":
                    queueList.Remove(song);
                    break;
            }
            RefreshQueue();
        }

        private async void OnAboutClicked(object sender, EventArgs e)
        {
            var message = "This is a very simple page.\nSongs you added on the Main Page show up here in the order in which they were added.\n" +
                "To change the order of the songs or to remove a song from your queue, tap on the song once and choose from the dropdown menu.\n" +
                "Double tap on a song to view its details on the Detail Page.\n\nClick on the Share button to share your queue with others." +
                "\nClick on the Clipboard button to copy your queue to your clipboard.\n\nThat's all... for now.";
            Vibrate();
            await Navigation.PushAsync(new AboutPage(message));            
        }
    }
}