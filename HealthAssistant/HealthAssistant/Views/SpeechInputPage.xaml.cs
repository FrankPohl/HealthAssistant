using HealthAssistant.ViewModels;
using System.Diagnostics;

namespace HealthAssistant.Views;

public partial class SpeechInputPage : ContentPage
{
    private SpeechInputViewModel vm;

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

    // This handler is necessary to see scrolling in CollectionView
    // Mimik behavior of ItemsUpdatingScrollMode="KeepLastItemInView" which doesn't work as expected
    private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        Debug.WriteLine($"Scrolled Event");

    }

    // This handler triggers scrolling whenever an item is added. Necessary for scrolling to new elements.
    // Mimik behavior of ItemsUpdatingScrollMode="KeepLastItemInView" which doesn't work as expected
    private void OnItemAdded(object sender, ElementEventArgs e)
    {
        if (vm.Messages.Count < 1)
            return;
        this.ChatList.ScrollTo(vm.Messages.Count - 1);
        Debug.WriteLine($"AddedItem called and scroll to {vm.Messages.Count-1}");
    }
}