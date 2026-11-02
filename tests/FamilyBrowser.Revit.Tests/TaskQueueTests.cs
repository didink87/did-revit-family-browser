using Autodesk.Revit.UI;
using NUnit.Framework;

namespace FamilyBrowser.Revit.Tests;

[TestFixture]
public class TaskQueueTests
{
    [Test]
    public void RunNext_executes_one_item_in_order(UIApplication uiapp)
    {
        var q = new RevitTaskQueue();
        var log = new List<int>();
        q.Enqueue(_ => log.Add(1));
        q.Enqueue(_ => log.Add(2));

        q.RunNext(uiapp);

        Assert.That(log, Is.EqualTo(new[] { 1 }));
        Assert.That(q.Pending, Is.EqualTo(1));
    }

    [Test]
    public void Failing_item_raises_Failed_and_queue_continues(UIApplication uiapp)
    {
        var q = new RevitTaskQueue();
        Exception? seen = null;
        var ran = false;
        q.Failed += e => seen = e;
        q.Enqueue(_ => throw new InvalidOperationException("x"));
        q.Enqueue(_ => ran = true);

        q.RunNext(uiapp);
        q.RunNext(uiapp);

        Assert.That(seen, Is.TypeOf<InvalidOperationException>());
        Assert.That(ran, Is.True);
    }
}
