namespace SetlistViewer;

public partial class LyricsPage : ContentPage
{
    public LyricsPage(SongData song, string lyrics, ImageSource albumArt)
    {
        InitializeComponent();

        lblTitle.Text = song.Name;
        lblArtist.Text = song.Artist;
        imgCover.Source = albumArt;

        var lines = lyrics.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        LyricsCollectionView.ItemsSource = lines;
    }

    private async void OnImageTapped(object sender, EventArgs e)
    {
        if (imgCover.Source != null)
        {
            await Navigation.PushModalAsync(new ImagePopupPage(imgCover.Source));
        }
    }
}