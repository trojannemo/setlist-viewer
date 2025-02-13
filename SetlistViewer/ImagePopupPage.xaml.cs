namespace SetlistViewer;

public partial class ImagePopupPage : ContentPage
{
    public ImagePopupPage(ImageSource imageSource)
    {
        InitializeComponent();
        FullSizeImage.Source = imageSource;
    }

    private async void OnCloseTapped(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}