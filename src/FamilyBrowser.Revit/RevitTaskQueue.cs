using System.Collections.Concurrent;
using Autodesk.Revit.UI;

namespace FamilyBrowser.Revit;

/// <summary>Runs queued work inside a valid Revit API context, one item per ExternalEvent so the UI can repaint in between.</summary>
public sealed class RevitTaskQueue : IExternalEventHandler
{
    private readonly ConcurrentQueue<Action<UIApplication>> queue = new();
    private readonly ExternalEvent externalEvent;

    public RevitTaskQueue() => externalEvent = ExternalEvent.Create(this);

    public int Pending => queue.Count;

    public event Action<Exception>? Failed;

    public void Enqueue(Action<UIApplication> work)
    {
        queue.Enqueue(work);
        externalEvent.Raise();
    }

    internal void RunNext(UIApplication app)
    {
        if (!queue.TryDequeue(out var work)) return;
        try { work(app); }
        catch (Exception ex) { Failed?.Invoke(ex); }
    }

    public void Execute(UIApplication app)
    {
        RunNext(app);
        if (!queue.IsEmpty) externalEvent.Raise();
    }

    public string GetName() => "FamilyBrowser.RevitTaskQueue";
}
