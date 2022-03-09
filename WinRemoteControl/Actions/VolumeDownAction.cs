using FluentResults;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Serilog;

namespace WinRemoteControl.Actions
{
    class VolumeDownAction : IAction
    {
        [DllImport("user32.dll")]
        static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private Form ReferenceToForm;

        public VolumeDownAction(Form ReferenceToForm)
        {
            this.ReferenceToForm = ReferenceToForm;
        }

        public Result DoAction()
        {
            Log.Information("Volume DOWN");
            this.ReferenceToForm.BeginInvoke((MethodInvoker)delegate
            {
                SendMessageW(this.ReferenceToForm.Handle, Constants.WM_APPCOMMAND, this.ReferenceToForm.Handle, (IntPtr)Constants.APPCOMMAND_VOLUME_DOWN);
            });
            return Result.Ok();
        }
    }
}
