using HealthAssistant.ViewModels;

namespace HealthAssistant.Views;

public partial class SpeechInputPage : ContentPage
{
	int count = 0;
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

    private void OnTextInput_Clicked(object sender, EventArgs e)
    {
        vm.SetInputAsText(this.TextInput.Text);
    }
}

