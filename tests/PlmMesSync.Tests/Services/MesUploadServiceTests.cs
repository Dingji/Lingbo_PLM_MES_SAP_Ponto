using PlmMesSync.Models.Dto;
using PlmMesSync.Services;

namespace PlmMesSync.Tests.Services;

public class MesUploadServiceTests
{
    [Fact]
    public void BuildRequest_ShouldMapFieldsCorrectly()
    {
        var version = new RevVersionOutput
        {
            RevNumber = "R01",
            IsLatest = true,
            Lines = new List<BomLineOutput>
            {
                new()
                {
                    Level = 1,
                    FindNumber = 10,
                    ItemNumber = "MTRL-001",
                    Quantity = "4",
                    Description = "Resistor",
                    RefDesig = "R1,R2",
                    ComponentRev = "R02",
                    SubstituteGroup = null,
                    SubstitutePriority = null
                }
            }
        };

        var method = typeof(MesUploadService)
            .GetMethod("BuildRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(null, new object[] { "PROD-001", version }) as MesUploadRequest;

        Assert.NotNull(result);
        Assert.Equal("BS_BOM", result.DocType);
        Assert.Equal("UPDATE", result.UpdateType);
        Assert.Single(result.Data);
        Assert.Equal("3701", result.Data[0].OrgCode);
        Assert.Equal("PROD-001", result.Data[0].ProdCode);
        Assert.Equal("R01", result.Data[0].BomVer);
        Assert.Equal("y", result.Data[0].IsDef);
        Assert.Equal("y", result.Data[0].IsValid);
        Assert.Single(result.Data[0].BsBomMtrl);
        Assert.Equal("MTRL-001", result.Data[0].BsBomMtrl[0].MtrlCode);
        Assert.Equal("y", result.Data[0].BsBomMtrl[0].IsMain);
        Assert.Equal("MTRL-001", result.Data[0].BsBomMtrl[0].MainCode);
        Assert.Equal(4m, result.Data[0].BsBomMtrl[0].Dosage);
    }

    [Fact]
    public void BuildRequest_SubstituteGroup_ShouldSetIsMainN()
    {
        var version = new RevVersionOutput
        {
            RevNumber = "R01",
            IsLatest = true,
            Lines = new List<BomLineOutput>
            {
                new()
                {
                    ItemNumber = "MTRL-001",
                    Quantity = "2",
                    SubstituteGroup = "GRP-A",
                    SubstitutePriority = "1"
                },
                new()
                {
                    ItemNumber = "MTRL-002",
                    Quantity = "2",
                    SubstituteGroup = "GRP-A",
                    SubstitutePriority = "2"
                }
            }
        };

        var method = typeof(MesUploadService)
            .GetMethod("BuildRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
        var result = method.Invoke(null, new object[] { "PROD-001", version }) as MesUploadRequest;

        Assert.NotNull(result);
        var lines = result.Data[0].BsBomMtrl;
        Assert.Equal(2, lines.Count);

        var main = lines[0];
        Assert.Equal("MTRL-001", main.MtrlCode);
        Assert.Equal("y", main.IsMain);
        Assert.Equal("MTRL-001", main.MainCode);

        var alt = lines[1];
        Assert.Equal("MTRL-002", alt.MtrlCode);
        Assert.Equal("n", alt.IsMain);
        Assert.Equal("MTRL-001", alt.MainCode);
    }

    [Fact]
    public void BuildRequest_NoLines_ShouldStillBuild()
    {
        var version = new RevVersionOutput
        {
            RevNumber = "R01",
            IsLatest = true,
            Lines = new List<BomLineOutput>()
        };

        var method = typeof(MesUploadService)
            .GetMethod("BuildRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
        var result = method.Invoke(null, new object[] { "PROD-001", version }) as MesUploadRequest;

        Assert.NotNull(result);
        Assert.Empty(result.Data[0].BsBomMtrl);
    }
}
