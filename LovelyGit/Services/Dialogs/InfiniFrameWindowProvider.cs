using InfiniFrame;

namespace ExpressThat.LovelyGit.Services.Dialogs;

public class InfiniFrameWindowProvider
{
    public IInfiniFrameWindow? Window { get; private set; }

    public void SetWindow(IInfiniFrameWindow window)
    {
        Window = window;
    }
}
