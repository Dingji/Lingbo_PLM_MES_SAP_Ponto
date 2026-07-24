using PlmMesSync.Services;

namespace PlmMesSync.Tests.Services;

public class TriggerStoreTests
{
    [Fact]
    public void Add_ShouldIncrementSequence()
    {
        var store = new TriggerStore(null);
        var r1 = store.Add(100, "INSERT", "BOM");
        var r2 = store.Add(101, "UPDATE", "BOM");

        Assert.Equal(1, r1.Sequence);
        Assert.Equal(2, r2.Sequence);
        Assert.Equal(2, store.TotalCount);
    }

    [Fact]
    public void GetRecent_ShouldReturnMostRecentFirst()
    {
        var store = new TriggerStore(null);
        store.Add(1, "INSERT", "BOM");
        store.Add(2, "UPDATE", "BOM");
        store.Add(3, "DELETE", "BOM");

        var recent = store.GetRecent(2);
        Assert.Equal(2, recent.Count);
        Assert.Equal(3, recent[0].BomId);
        Assert.Equal(2, recent[1].BomId);
    }

    [Fact]
    public void Add_ShouldNotExceedMaxRecords()
    {
        var store = new TriggerStore(null);
        for (int i = 0; i < 2500; i++)
            store.Add(i, "INSERT", "BOM");

        Assert.Equal(2500, store.TotalCount);
        var recent = store.GetRecent(5000);
        Assert.True(recent.Count <= 2000);
    }

    [Fact]
    public void Add_ShouldStoreCorrectValues()
    {
        var store = new TriggerStore(null);
        var record = store.Add(42, "UPDATE", "FILES");

        Assert.Equal(1, record.Sequence);
        Assert.Equal(42, record.BomId);
        Assert.Equal("UPDATE", record.Action);
        Assert.Equal("FILES", record.Table);
        Assert.NotEqual(default, record.ReceivedAt);
    }
}
