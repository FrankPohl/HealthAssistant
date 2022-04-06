using HealthAssistant.ViewModels;

namespace HealthAssistant.Views;

public partial class SpeechInputPage : ContentPage
{
	SpeechInputViewModel vm;
	public SpeechInputPage()
	{
		InitializeComponent();
		this.BindingContext = vm = new SpeechInputViewModel();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		vm.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

}

