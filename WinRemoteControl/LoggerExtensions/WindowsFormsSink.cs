using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.IO;
using System.Windows.Forms;

namespace WinRemoteControl.LoggerExtensions
{
    class WindowsFormsSink : ILogEventSink
    { 
        private readonly ITextFormatter Formatter;
        private readonly MainForm ReferenceToForm;

        public WindowsFormsSink(ITextFormatter Formatter, MainForm ReferenceToForm)
        {
            this.Formatter = Formatter;
            this.ReferenceToForm = ReferenceToForm;
        }

        public void Emit(LogEvent logEvent)
        {
            using var buffer = new StringWriter();
            this.Formatter.Format(logEvent, buffer);
            this.ReferenceToForm.BeginInvoke((MethodInvoker)delegate
            {
                this.ReferenceToForm.textBoxLog.AppendText(buffer.ToString());
            });
        }
    }

    public static class WindowsFormsSinkExtensions
    {
        const string DefaultDebugOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        public static LoggerConfiguration WindowsFormsSink(
                  this LoggerSinkConfiguration sinkConfiguration,
                  MainForm ReferenceToForm,
                  string outputTemplate = DefaultDebugOutputTemplate, 
                  IFormatProvider? formatProvider = null)
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            return sinkConfiguration.Sink(new WindowsFormsSink(formatter, ReferenceToForm));
        }
    }
}
