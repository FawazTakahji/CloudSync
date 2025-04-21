namespace CloudSync.Models;

public class BoxButton
{
    public BoxButton(string text, Action action, bool exitOnClick = true)
    {
        Text = text;
        Action = action;
        ExitOnClick = exitOnClick;
    }

    public void RunAction()
    {
        Action.Invoke();
    }

    public string Text { get; set; }
    public Action Action { get; set; }
    public bool ExitOnClick { get; set; }
}