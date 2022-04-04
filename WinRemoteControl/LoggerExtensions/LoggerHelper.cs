using FluentResults;

namespace WinRemoteControl.LoggerExtensions
{
    public class LoggerHelper
    {
        public static string outputTemplate = "{Timestamp:MM/dd/yy H:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        public static void SetupNONUILogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: outputTemplate)
                .WriteTo.Debug(outputTemplate: outputTemplate)
                .WriteTo.File("logs/log.txt",
                    rollingInterval: RollingInterval.Month,
                    outputTemplate: outputTemplate)
                .CreateLogger();
        }

        public static void SetupCompleteLogger(MainForm form)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: outputTemplate)
                .WriteTo.Debug(outputTemplate: outputTemplate)
                .WriteTo.File("logs/log.txt",
                    rollingInterval: RollingInterval.Month,
                    outputTemplate: outputTemplate)
                .WriteTo.WindowsFormsSink(form, outputTemplate: outputTemplate)
                .CreateLogger();
        }

        public static string ResultErrorsToString(List<IError> e)
        {
            return string.Join(",", e.Select(e => e.Message));
        }
    }
}
