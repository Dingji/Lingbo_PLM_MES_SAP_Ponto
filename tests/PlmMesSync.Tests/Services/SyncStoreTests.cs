using PlmMesSync.Services;

namespace PlmMesSync.Tests.Services;

public class SyncStoreTests
{
    [Fact]
    public void Add_ShouldCreateRecord()
    {
        var store = new SyncStore(null);
        var now = DateTime.Now;
        store.Add(1, 100, "ITEM-001", now, now, "SUCCESS", logFile: "test.log", mesStatus: "OK");

        var recent = store.GetRecent(10);
        Assert.Single(recent);
        var r = recent[0];
        Assert.Equal(1, r.Sequence);
        Assert.Equal(1, r.BomId);
        Assert.Equal(100, r.ItemId);
        Assert.Equal("ITEM-001", r.ItemNumber);
        Assert.Equal("SUCCESS", r.Status);
        Assert.Equal("test.log", r.LogFile);
        Assert.Equal("OK", r.MesStatus);
    }

    [Fact]
    public void Add_MultipleRecords_ReturnsLatestFirst()
    {
        var store = new SyncStore(null);
        store.Add(1, null, null, DateTime.Now.AddSeconds(-10), DateTime.Now, "SUCCESS");
        store.Add(2, null, null, DateTime.Now, DateTime.Now, "FAILED", error: "err");

        var recent = store.GetRecent(5);
        Assert.Equal(2, recent.Count);
        Assert.Equal("FAILED", recent[0].Status);
        Assert.Equal("SUCCESS", recent[1].Status);
    }

    [Fact]
    public void TotalCount_ShouldTrackAdds()
    {
        var store = new SyncStore(null);
        Assert.Equal(0, store.TotalCount);

        for (int i = 0; i < 100; i++)
            store.Add(i, null, null, DateTime.Now, DateTime.Now, "SUCCESS");

        Assert.Equal(100, store.TotalCount);
    }
}
