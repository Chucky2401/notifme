// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using Microsoft.Toolkit.Uwp.Notifications;
using NotifMe;
using Windows.Foundation.Collections;

internal class Program {

    static int Main(string[] args) {
        // Configuration du gestionnaire de notifications Toast
        ToastNotificationManagerCompat.OnActivated += toastArgs => {
            ToastArguments toastArguments = ToastArguments.Parse(toastArgs.Argument);
            ValueSet userInput = toastArgs.UserInput;
            if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated()) {
                ToastNotificationManagerCompat.History.Clear();
                Environment.Exit(0);
            }
        };

        // Configuration et parsing des arguments de ligne de commande
        var config = new CommandLineConfig();
        var rootCommand = config.CreateRootCommand();

        // Parse les arguments
        ParseResult parseResult = rootCommand.Parse(args);

        // Extraire les valeurs
        var title = parseResult.GetValue(config.TitleOption);
        var message = parseResult.GetValue(config.MessageOption);
        var type = parseResult.GetValue(config.TypeOption);
        var expiration = parseResult.GetValue(config.ExpirationOption);
        var duration = parseResult.GetValue(config.DurationOption);
        var sticky = parseResult.GetValue(config.StickyOption);

        // Vérifier les erreurs
        if (parseResult.Errors.Count > 0) {
            foreach (var error in parseResult.Errors) {
                Console.Error.WriteLine(error.Message);
            }
            return 1;
        }

        // Créer l'objet arguments
        var arguments = new ProgramArguments {
            Title = title ?? string.Empty,
            Message = message ?? string.Empty,
            Type = type,
            Expiration = expiration,
            Duration = duration,
            Sticky = sticky
        };

        // Afficher la notification
        ShowToastNotification(arguments);

        return 0;
    }

    static void ShowToastNotification(ProgramArguments opts) {
        Dictionary<MessageType, string> images = new() {
            { MessageType.Error,   "img\\error.png" },
            { MessageType.Info,    "img\\info.png" },
            { MessageType.Warning, "img\\warn.png" },
            { MessageType.Success, "img\\success.png" }
        };

        ToastContentBuilder toast = new();

        if (images.TryGetValue(opts.Type, out string? value)) {
            string iconPath = Path.GetFullPath(value);
            if (File.Exists(iconPath)) {
                toast.AddAppLogoOverride(new Uri(iconPath));
            }
        }

        if (!string.IsNullOrEmpty(opts.Title)) {
            toast.AddText(opts.Title);
        }

        toast.AddText(opts.Message);

        if (opts.Duration) {
            toast.SetToastDuration(ToastDuration.Long);
        }

        if (opts.Duration) {
            toast.SetToastDuration(ToastDuration.Long);
        }

        if (opts.Sticky) {
            toast.SetToastScenario(ToastScenario.Reminder);
            toast.AddButton(new ToastButton().SetContent("OK").SetDismissActivation());
        }

        toast.Show(t => {
            if (opts.Expiration > 0) {
                t.ExpirationTime = DateTime.Now.AddSeconds(opts.Expiration);
            }
        });
    }
}