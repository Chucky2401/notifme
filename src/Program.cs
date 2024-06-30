// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using Microsoft.Toolkit.Uwp.Notifications;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using Windows.Foundation.Collections;
using Windows.Media.Capture;

public class Options {
    [Option('p', "prompt", Required = false, HelpText = "Set title of the toast notification")]
    public string Prompt { get; set; }

    [Option('m', "message", Required = true, HelpText = "Set message of the toast notification")]
    public string Message { get; set; }

    [Option('t', "type", Required = false, HelpText = "Set icon of the toast notification")]
    public string Type { get; set; }

    [Option('e', "expiration", Required = false, HelpText = "Number of seconds before the OS mark this notification as expired. (Default: 86400 seconds ; 1 day")]
    public double Expiration { get; set; }

    [Option('d', "duration", Required = false, HelpText = "Make the toast appears longer")]
    public bool Duration { get; set; }

    [Option('s', "Sticky", Required = false, HelpText = "Make the toast appears longer")]
    public bool Sticky { get; set; }
}

internal class Program {

    static void Main(string[] args) {
        ToastNotificationManagerCompat.OnActivated += toastArgs => {
            //Get the activation args, if you need those.
            ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
            //Get user input if there's any and if you need those.
            ValueSet userInput = toastArgs.UserInput;
            //if the app instance just started after clicking on a notification 
            if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated()) {
                ToastNotificationManagerCompat.History.Clear();
                Application.Exit();
            }
        };

        ParserResult<Options> parserResult = Parser.Default.ParseArguments<Options>(args);
        parserResult
            .WithParsed(opts => RunOptions(opts))
            .WithNotParsed(errs => HandleParseError(parserResult, errs));
    }

    static void RunOptions(Options opts) {
        Dictionary<string, string> image = new Dictionary<string, string>() {
            { "error"   , "img\\error.png" },
            { "info"    , "img\\info.png" },
            { "question", "img\\question.png" },
            { "success" , "img\\success.png" },
            { "warn"    , "img\\warn.png" }
        };

        string iconPath = opts.Type;
        string title = opts.Prompt;
        if (String.IsNullOrEmpty(title)) {
            title = string.Empty;
        }

        string message = opts.Message;

        double expiration = 86400;
        if (opts.Expiration > 0) {
            expiration = opts.Expiration;
        }

        expiration = 0;

        ToastContentBuilder toast = new ToastContentBuilder();

        if (!String.IsNullOrEmpty(iconPath)) {
            string icon = Path.GetFullPath(image[opts.Type]);
            toast.AddAppLogoOverride(new Uri(icon));
        }

        toast.AddText(title);
        toast.AddText(message);

        if (opts.Duration) {
            toast.SetToastDuration(ToastDuration.Long);
        }

        if (opts.Sticky) {
            toast.SetToastScenario(ToastScenario.Reminder);
            toast.AddButton(new ToastButton().SetContent("OK").SetDismissActivation());
        }

        toast.Show(toast => {
            toast.ExpirationTime = DateTime.Now.AddSeconds(expiration);
        });
    }

    static void HandleParseError(ParserResult<Options> parserResult, IEnumerable<Error> errs) {
        HelpText helpText = HelpText.AutoBuild(parserResult, h => {
            h.AdditionalNewLineAfterOption = false;
            h.AutoVersion = false;
            h.Heading = "NotifMe v1.0.0";
            h.Copyright = "Copyright © 2024 - The Black Wizard";
            h.MaximumDisplayWidth = 160;
            return HelpText.DefaultParsingErrorsHandler(parserResult, h);
        }, e => e);

        if (errs.Any(x => x is HelpRequestedError)) {
            MessageBox.Show(helpText, "NotifMe Help", MessageBoxButtons.OK);
        }


        if (errs.Any(x => x is MissingRequiredOptionError)) {
            MessageBox.Show(helpText, "NotifMe Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}