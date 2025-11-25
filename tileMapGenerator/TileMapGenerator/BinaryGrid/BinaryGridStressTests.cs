namespace BinaryGrid;
#pragma warning disable MSTEST0045 // Use 'CooperativeCancellation = true' with '[Timeout]'

[TestClass]
public class BinaryGridStressTests
{


    [Timeout(1000)]
    [TestMethod]
    public void BinaryGridConstructor_200_Valid()
    {
        _ = new BinaryGrid(200, 200);
    }

    [Timeout(1000)]
    [TestMethod]
    public void BinaryGridConstructor_400_Valid()
    {
        _ = new BinaryGrid(400, 400);
    }

    [Timeout(1000)]
    [TestMethod]
    public void BinaryGridConstructor_800_Valid()
    {
        _ = new BinaryGrid(800, 800);
    }

    [Timeout(1000)]
    [TestMethod]
    public void BinaryGridConstructor_1600_Valid()
    {
        _ = new BinaryGrid(1600, 1600);
    }

    [Timeout(1000)]
    [TestMethod]
    public void BinaryGridConstructor_3200_Valid()
    {
        _ = new BinaryGrid(3200, 3200);
    }

    [Timeout(10000)]
    [TestMethod]
    public void BinaryGridGetSlice_Huge_Valid()
    {
        BinaryGrid grid = new BinaryGrid(800, 800);
        for (uint i = 0; i < 1000; i++)
        {
            _ = grid.GetSlice((i%800)+1, (i%800)+1, (i%800)+1, (uint) ((i*.5)%800)+1);
        }
    }

    [Timeout(10000)]
    [TestMethod]
    public void BinaryGridSetSlice_Huge_Valid()
    {
        BinaryGrid grid = new BinaryGrid(800, 800);
        for (uint i = 0; i < 1000; i++)
        {
            grid.SetSlice((i%800)+1, (i%800)+1, (i%800)+1, (uint) ((i*.25)%800)+1, 1);
        }
    }

    [Timeout(10000)]
    [TestMethod]
    public void BinaryGridInsertRowCol_Huge_Valid()
    {
        BinaryGrid grid = new BinaryGrid(800, 800);
        for (uint i = 0; i < 1000; i++)
        {
            grid.InsertRow((i % 800) + 1);
            grid.InsertRow((i % 800) + 1);
        }
    }

    [Timeout(10000)]
    [TestMethod]
    public void BinaryGridGetSliceORAND_Huge_Valid()
    {
        BinaryGrid grid = new BinaryGrid(800, 800);
        for (uint i = 0; i < 1000; i++)
        {
            _ = grid.GetSliceOR((i%800)+1, (i%800)+1, (i%800)+1, (uint) ((i*.5)%800)+1);
            _ = grid.GetSliceAND((i%800)+1, (i%800)+1, (i%800)+1, (uint) ((i*.5)%800)+1);
        }
    }

    [Timeout(10000)]
    [TestMethod]
    public void BinaryGridDeleteRowCol_Huge_Valid()
    {
        BinaryGrid grid = new BinaryGrid(800, 800);
        for (uint i = 0; i < 799; i++)
        {
            grid.DeleteRow(800 - i);
            grid.DeleteCol(800 - i);
        }
    }
    
}