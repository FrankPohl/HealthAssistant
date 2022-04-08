using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace HealthAssistant.ViewModels
{
    public class AboutViewModel : ObservableObject
    {
        public AboutViewModel()
        {
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://aka.ms/xamarin-quickstart"));
        }

        public ICommand OpenWebCommand { get; }
    }
}