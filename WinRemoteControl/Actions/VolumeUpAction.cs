using FluentResults;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Serilog;

namespace WinRemoteControl.Actions
{
    class VolumeUpAction : IAction
    {
        [DllImport("user32.dll")]
        static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private Form ReferenceToForm;

        public VolumeUpAction(Form ReferenceToForm)
        {
            this.ReferenceToForm = ReferenceToForm;
        }

        public Result DoAction()
        {
            Log.Information("Volume UP");
            this.ReferenceToForm.BeginInvoke((MethodInvoker)delegate
            {
                SendMessageW(this.ReferenceToForm.Handle, Constants.WM_APPCOMMAND, this.ReferenceToForm.Handle, (IntPtr)Constants.APPCOMMAND_VOLUME_UP);
            });
            return Result.Ok();
        }
    }
}
