namespace SetlistViewer;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new MainPage())
        {
            BarBackgroundColor = (Color)Resources["PrimaryDark"],
            BarTextColor = Colors.Black
        };
    }
}
