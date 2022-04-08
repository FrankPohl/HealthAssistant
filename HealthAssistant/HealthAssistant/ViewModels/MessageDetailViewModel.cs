using CommunityToolkit.Mvvm.ComponentModel;

namespace HealthAssistant.ViewModels
{
    public enum MessageSender
    {
        Server,
        User
    };

    public class MessageDetailViewModel : ObservableObject
    {
        private InputItemViewModel item = new();
        private DateTime messageDateTime;
        private string message;
        private MessageSender sender;

        public DateTime MessageDateTime
        {
            get => messageDateTime;
            set => SetProperty(ref messageDateTime, value);
        }

        public MessageSender Sender
        {
            get => sender;
            set => SetProperty(ref sender, value);
        }

        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }

        public InputItemViewModel Item
        {
            get => item;
            set => SetProperty(ref item, value);
        }
    }
}