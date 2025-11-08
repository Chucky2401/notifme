using System.CommandLine;

namespace NotifMe {

    public enum MessageType {
        Info,
        Warning,
        Error,
        Success
    }

    public class ProgramArguments {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public MessageType Type { get; set; }
        public double Expiration { get; set; }
        public bool Duration { get; set; }
        public bool Sticky { get; set; }
    }

    // Classe pour configurer les arguments
    public class CommandLineConfig {
        public Option<string> TitleOption { get; }
        public Option<string> MessageOption { get; }
        public Option<MessageType> TypeOption { get; }
        public Option<double> ExpirationOption { get; }
        public Option<bool> DurationOption { get; }
        public Option<bool> StickyOption { get; }

        public CommandLineConfig() {
            TitleOption = new("-p", "--title") {
                Description = "Set title of the toast notification",
                Required = false,
                DefaultValueFactory = parseResult => ""
            };

            MessageOption = new("-m", "--message") {
                Description = "Set message of the toast notification",
                Required = true
            };

            TypeOption = new("-t", "--type") {
                Description = "Set icon of the toast notification",
                Required = false,
                DefaultValueFactory = parseResult => MessageType.Info
            };

            ExpirationOption = new("-e", "--expiration") {
                Description = "Number of seconds before the OS mark this notification as expired. (Default: 86400 seconds ; 1 day)",
                Required = false,
                DefaultValueFactory = parseResult => 86400
            };

            DurationOption = new("-d", "--duration") {
                Description = "Make the toast appears longer",
                Required = false,
                DefaultValueFactory = parseResult => false
            };

            StickyOption = new("-s", "--sticky") {
                Description = "Make the toast appears longer",
                Required = false,
                DefaultValueFactory = parseResult => false
            };
        }

        public RootCommand CreateRootCommand() {
            var rootCommand = new RootCommand("NotifMe") {
                TitleOption,
                MessageOption,
                TypeOption,
                ExpirationOption,
                DurationOption,
                StickyOption
            };

            return rootCommand;
        }
    }
}
