using FluentResults;

namespace WinRemoteControl.Actions;

class KeyPressAction : IAction
{
    private readonly string key;

    public KeyPressAction(string key)
    {
        this.key = key;
    }

    public Result DoAction()
    {
        Log.Information("Pressing key {Key}", key);
        SendKeys.SendWait(key);
        return Result.Ok();
    }
}
