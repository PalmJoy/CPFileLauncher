using CPClipboard.Core.Model;
using CPClipboard.Core.Storage;
namespace CPClipboard.Tests;

public sealed class HistoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
    [Fact]
    public void SettingsClampAndKeepOldTriggers()
    {
        var s = new AppSettings { History = new() { MaxItems = 5000, AutoClearMinutes = 0 } };
        s.Normalize();
        Assert.Equal(1000, s.History.MaxItems); Assert.Equal(1, s.History.AutoClearMinutes);
        Assert.True(s.Triggers.DoubleTap.Enabled); Assert.False(s.HistoryTriggers.DoubleTap.Enabled);
        Assert.Empty(s.HistoryTriggers.Mouse);
        s.History.MaxItems = -1; s.Normalize(); Assert.Equal(1, s.History.MaxItems);
    }
    [Fact]
    public void NewestFirstDeduplicatesAndTrims()
    {
        var book = new HistoryBook(); var settings = new HistorySettings { MaxItems = 2 };
        book.Add("first\r\nline", Now, settings);
        var id = book.Entries[0].Id;
        book.Add("second", Now.AddSeconds(1), settings);
        book.Add("first\nline", Now.AddSeconds(2), settings);
        Assert.Equal(id, book.Entries[0].Id); Assert.Equal(2, book.Entries.Count);
        book.Add("third", Now.AddSeconds(3), settings);
        Assert.Equal(new[] { "third", "first\nline" }, book.Entries.Select(e => e.Text));
        Assert.Equal("first\nline", Assert.Single(book.Filter("FIRST")).Text);
    }
    [Fact]
    public void ExpiryIsPerEntryAndWorksAtBoundaryWithLargeMinutes()
    {
        var book = new HistoryBook(); var s = new HistorySettings { AutoClear = true, AutoClearMinutes = 10 };
        book.Add("old", Now, s); book.Add("new", Now.AddMinutes(1), s);
        Assert.True(book.Prune(Now.AddMinutes(10), s));
        Assert.Equal("new", Assert.Single(book.Entries).Text);
        s.AutoClearMinutes = int.MaxValue; Assert.False(book.Prune(Now.AddYears(100), s));
        s.AutoClear = false; Assert.False(book.Prune(Now.AddYears(200), s));
    }
    [Fact]
    public void BlankAndOversizedTextAreIgnoredAndPersistenceRetainsOrder()
    {
        var book = new HistoryBook(); var s = new HistorySettings();
        Assert.False(book.Add(" \n", Now, s));
        Assert.False(book.Add(new string('x', HistoryBook.MaxTextLength + 1), Now, s));
        book.Add("persist", Now, s);
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var store = new VersionedJsonStore<HistoryBook>(Path.Combine(dir, "history.json"), 1, () => new());
            store.Save(book);
            Assert.Equal(book.Entries[0], Assert.Single(store.Load().Value.Entries));
        }
        finally { Directory.Delete(dir, true); }
    }
}
