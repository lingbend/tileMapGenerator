using System.Diagnostics;
using System.Numerics;

namespace BinaryGrid;

[TestClass]
public class BinaryGridTests
{
    // TODO: Add non-square grid tests
    // TODO: Add huge grid tests
    [TestMethod]
    public void BinaryGridConstructor_0Constructor_Valid()
    {
        _ = new BinaryGrid(5, 5);
    }

    [TestMethod]
    public void BinaryGridConstructor_DefaultConstructor_Valid()
    {
        _ = new BinaryGrid(5, 5, 1);
    }

    [TestMethod]
    public void BinaryGridConstructor_2Border_Invalid()
    {
        Assert.Throws<ArgumentException>(() => new BinaryGrid(5, 5, 2));
    }

    [TestMethod]
    public void BinaryGridConstructor_NoSize_Invalid()
    {
        Assert.Throws<Exception>(() => new BinaryGrid(0,0));
    }

    [TestMethod]
    public void BinaryGridConstructor_100x100_ValidFullSize()
    {
        _ = new BinaryGrid(100, 100);
    }

    [TestMethod]
    public void BinaryGridConstructor_0Rows_Invalid()
    {
        Assert.Throws<Exception>(() => new BinaryGrid(0,5));
    }

    [TestMethod]
    public void BinaryGridConstructor_0Cols_Invalid()
    {
        Assert.Throws<Exception>(() => new BinaryGrid(5,0));
    }

    [TestMethod]
    public void BinaryGridChangeBorders_1SameOnEmpty_ValidNoChange()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid.ChangeBorders(1);
    }

    [TestMethod]
    public void BinaryGridChangeBorders_0BorderOnEmpty_ValidAllZeroes()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid.ChangeBorders(0);
    }

    [TestMethod]
    public void BinaryGridChangeBorders_2Border_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<ArgumentException>(() => grid.ChangeBorders(2));
    }

    [TestMethod]
    public void BinaryGridChangeBorders_1SameFullGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid.SetSlice(1, 1, 1, 5, 1);
        grid.SetSlice(2, 1, 2, 5, 1);
        grid.SetSlice(3, 1, 3, 5, 1);
        grid.SetSlice(4, 1, 4, 5, 1);
        grid.SetSlice(5, 1, 5, 5, 1);
        grid.ChangeBorders(1);
        BinaryGrid testGrid = new BinaryGrid(5, 5);
        testGrid._grid = 0b1_111_111_111_111_111_111_111_111_111_111_111_111_111_111_111_111;
        Assert.AreEqual(testGrid,grid);
    }

    [TestMethod]
    public void BinaryGridChangeBorders_0FullGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid.SetSlice(1, 1, 1, 5, 1);
        grid.SetSlice(2, 1, 2, 5, 1);
        grid.SetSlice(3, 1, 3, 5, 1);
        grid.SetSlice(4, 1, 4, 5, 1);
        grid.SetSlice(5, 1, 5, 5, 1);
        grid.ChangeBorders(0);
        BinaryGrid testGrid = new BinaryGrid(5, 5);
        testGrid._grid = 0b0_000_000_011_111_001_111_100_111_110_011_111_001_111_100_000_000;
        Assert.AreEqual(testGrid,grid);
    }

    [TestMethod]
    public void BinaryGridChangeBorders_2FullGraph_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid.SetSlice(1, 1, 1, 5, 1);
        grid.SetSlice(2, 1, 2, 5, 1);
        grid.SetSlice(3, 1, 3, 5, 1);
        grid.SetSlice(4, 1, 4, 5, 1);
        grid.SetSlice(5, 1, 5, 5, 1);
        Assert.Throws<ArgumentException>(()=>grid.ChangeBorders(2));
    }

    [TestMethod]
    public void BinaryGridGetSetCell_0MiddleCellEmptyGraph_0Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000101_1000001_1111111;
        grid.SetCell(2, 2, 0);
        Assert.AreEqual<BigInteger>(0, grid.GetCell(2, 2));
    }

    [TestMethod]
    public void BinaryGridGetSetCell_1MiddleCellEmptyGraph_1Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        grid.SetCell(2, 2, 1);
        Assert.AreEqual<BigInteger>(1, grid.GetCell(2, 2));
    }

    [TestMethod]
    public void BinaryGridSetCell_2MiddleCellEmptyGraph_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000101_1111111;
        Assert.Throws<ArgumentException>(()=>grid.SetCell(2, 2, 2));
    }

    [TestMethod]
    public void BinaryGridGetSetCell_0MiddleCellFullGraph_0Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.SetCell(2, 2, 0);
        Assert.AreEqual<BigInteger>(0, grid.GetCell(2, 2));
    }

    [TestMethod]
    public void BinaryGridGetSetCell_1MiddleCellFullGraph_1Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.SetCell(2, 2, 1);
        Assert.AreEqual<BigInteger>(1, grid.GetCell(2, 2));
    }

    [TestMethod]
    public void BinaryGridSetCell_2MiddleCellFullGraph_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        Assert.Throws<ArgumentException>(()=>grid.SetCell(2, 2, 2));
    }

    [TestMethod]
    public void BinaryGridGetSetCell_0CornerCellEmptyGraph_0Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000011_1111111;
        grid.SetCell(1, 1, 0);
        Assert.AreEqual<BigInteger>(0, grid.GetCell(1, 1));
    }

    [TestMethod]
    public void BinaryGridGetSetCell_1CornerCellEmptyGraph_1Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        grid.SetCell(1, 1, 1);
        Assert.AreEqual<BigInteger>(1, grid.GetCell(1, 1));
    }

    [TestMethod]
    public void BinaryGridSetCell_2CornerCellEmptyGraph_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        Assert.Throws<ArgumentException>(()=>grid.SetCell(1, 1, 2));
    }

    [TestMethod]
    public void BinaryGridGetCellNeighbors_All0NeighborsMiddle_0b00000000()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b000_000_000, grid.GetCellNeighbors(2, 2));
    }

    [TestMethod]
    public void BinaryGridGetCellNeighbors_EachDirectionAs1Middle_All0sExcept1Binary()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000101_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b100_000_000, grid.GetCellNeighbors(2, 2), "self");

        grid._grid = 0b1111111_1000001_1000001_1000001_1001001_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b010_000_000, grid.GetCellNeighbors(2, 2), "right");

        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1001001_1111111;
        Assert.AreEqual<BigInteger>(0b001_000_000, grid.GetCellNeighbors(2, 2), "upper right");

        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000101_1111111;
        Assert.AreEqual<BigInteger>(0b000_100_000, grid.GetCellNeighbors(2, 2), "up");

        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000011_1111111;
        Assert.AreEqual<BigInteger>(0b000_010_000, grid.GetCellNeighbors(2, 2), "upper left");

        grid._grid = 0b1111111_1000001_1000001_1000001_1000011_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b000_001_000, grid.GetCellNeighbors(2, 2), "left");

        grid._grid = 0b1111111_1000001_1000001_1000011_1000001_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b000_000_100, grid.GetCellNeighbors(2, 2), "lower left");

        grid._grid = 0b1111111_1000001_1000001_1000101_1000001_1000000_1111111;
        Assert.AreEqual<BigInteger>(0b000_000_010, grid.GetCellNeighbors(2, 2), "down");

        grid._grid = 0b1111111_1000001_1000001_1001001_1000001_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b000_000_001, grid.GetCellNeighbors(2, 2), "lower right");
    }

    [TestMethod]
    public void BinaryGridGetCellNeighbors_All1NeighborsMiddle_0b111111111()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1001111_1001111_1001111_1111111;
        Assert.AreEqual<BigInteger>(0b111_111_111, grid.GetCellNeighbors(2, 2));
    }

    [TestMethod]
    public void BinaryGridGetCellNeighbors_All0NeighborsCorners_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b001_111_100, grid.GetCellNeighbors(1, 1), "upper left corner");

        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b011_110_001, grid.GetCellNeighbors(1, 5), "upper right corner");

        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b000_011_111, grid.GetCellNeighbors(5, 1), "lower left corner");

        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        Assert.AreEqual<BigInteger>(0b011_000_111, grid.GetCellNeighbors(5, 5), "lower right corner");
    }

    [TestMethod]
    public void BinaryGridInsertRow_NormalSizeEmptyGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        grid.InsertRow(2);
        BinaryGrid test_grid = new BinaryGrid(6, 5);
        test_grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridInsertRow_NormalSizeAlternatingGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1010101_1111111;
        grid.InsertRow(2);
        BinaryGrid test_grid = new BinaryGrid(6, 5);
        test_grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1000001_1010101_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridInsertRow_NormalSizeFullGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.InsertRow(2);
        BinaryGrid test_grid = new BinaryGrid(6, 5);
        test_grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1000001_1111111_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridInsertRow_NormalSizeEmptyGraphSides_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        grid.InsertRow(1);
        BinaryGrid test_grid = new BinaryGrid(6, 5);
        test_grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridInsertRow_NormalSizeFullGraphSides_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.InsertRow(1);
        BinaryGrid test_grid = new BinaryGrid(6, 5);
        test_grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridInsertRow_NormalSizeBadIndexes_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(0));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(6));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(7));
    } 

    [TestMethod]
    public void BinaryGridInsertRow_1WideGrid_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 1);
        grid.InsertRow(1);
        BinaryGrid test_grid = new BinaryGrid(6, 1);
        test_grid._grid = 0b111_101_101_101_101_101_101_111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    } 

    [TestMethod]
    public void BinaryGridInsertRow_1HighGrid_Valid()
    {
        BinaryGrid grid = new BinaryGrid(1, 5);
        grid.InsertRow(1);
        BinaryGrid test_grid = new BinaryGrid(2, 5);
        test_grid._grid = 0b1111111_1000001_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    } 

    [TestMethod]
    public void BinaryGridInsertRow_1WideGrid_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(6));
        grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(0));
        grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(7));
    } 

    [TestMethod]
    public void BinaryGridInsertRow_1HighGrid_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(0));
        grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(2));
        grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(3));
    } 


    [TestMethod]
    public void BinaryGridInsertCol_NormalSizeEmptyGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        grid.InsertCol(2);
        BinaryGrid test_grid = new BinaryGrid(5, 6);
        test_grid._grid = 0b11111111_10000001_10000001_10000001_10000001_10000001_11111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridInsertCol_NormalSizeAlternatingGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1010101_1111111;
        grid.InsertCol(2);
        BinaryGrid test_grid = new BinaryGrid(5, 6);
        test_grid._grid = 0b11111111_10101001_10101001_10101001_10101001_10101001_11111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }


    [TestMethod]
    public void BinaryGridInsertCol_NormalSizeFullGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.InsertCol(2);
        BinaryGrid test_grid = new BinaryGrid(5, 6);
        test_grid._grid = 0b11111111_11111011_11111011_11111011_11111011_11111011_11111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridInsertCol_NormalSizeEmptyGraphSides_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        grid.InsertCol(1);
        BinaryGrid test_grid = new BinaryGrid(5, 6);
        test_grid._grid = 0b11111111_10000001_10000001_10000001_10000001_10000001_11111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridInsertCol_NormalSizeFullGraphSides_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.InsertCol(1);
        BinaryGrid test_grid = new BinaryGrid(5, 6);
        test_grid._grid = 0b11111111_11111101_11111101_11111101_11111101_11111101_11111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridInsertCol_NormalSizeBadIndexes_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(0));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(6));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(7));
    }

    [TestMethod]
    public void BinaryGridInsertCol_1WideGrid_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 1);
        grid.InsertCol(1);
        BinaryGrid test_grid = new BinaryGrid(5, 2);
        test_grid._grid = 0b1111_1001_1001_1001_1001_1001_1111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    } 

    [TestMethod]
    public void BinaryGridInsertCol_1HighGrid_Valid()
    {
        BinaryGrid grid = new BinaryGrid(1, 5);
        grid.InsertCol(1);
        BinaryGrid test_grid = new BinaryGrid(1, 6);
        test_grid._grid = 0b11111111_10000001_11111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    } 

    [TestMethod]
    public void BinaryGridInsertCol_1WideGrid_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(0));
        grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(2));
        grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(3));
    } 

    [TestMethod]
    public void BinaryGridInsertCol_1HighGrid_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(0));
        grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(6));
        grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(7));
    }  

    [TestMethod]
    public void BinaryGridDeleteRow_NormalSizeEmptyGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        grid.DeleteRow(2);
        BinaryGrid test_grid = new BinaryGrid(4, 5);
        test_grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridDeleteRow_NormalSizeAlternatingGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(4, 5);
        grid._grid = 0b1111111_1000001_1010101__1000001_1010101_1111111;
        grid.DeleteRow(2);
        BinaryGrid test_grid = new BinaryGrid(3, 5);
        test_grid._grid = 0b1111111_1000001_1010101__1010101_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridDeleteRow_NormalSizeFullGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.DeleteRow(2);
        BinaryGrid test_grid = new BinaryGrid(4, 5);
        test_grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridDeleteRow_NormalSizeGraphSides_Valid()
    {
        BinaryGrid grid = new BinaryGrid(4, 5);
        grid._grid = 0b1111111_1000001_1010101__1000001_1010101_1111111;
        grid.DeleteRow(1);
        BinaryGrid test_grid = new BinaryGrid(3, 5);
        test_grid._grid = 0b1111111_1000001_1010101__1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridDeleteRow_NormalSizeBadIndexes_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(0));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(6));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(7));
    } 
    
    [TestMethod]
    public void BinaryGridDeleteRow_1WideGrid_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 1);
        grid.DeleteRow(1);
        BinaryGrid test_grid = new BinaryGrid(4, 1);
        test_grid._grid = 0b111_101_101_101_101_111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    } 

    [TestMethod]
    public void BinaryGridDeleteRow_1WideGrid_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(6));
        grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(0));
        grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(7));
    } 

    [TestMethod]
    public void BinaryGridDeleteRow_1HighGrid_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(0));
        grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(1));
        grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(2));
        grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(3));
    } 

    [TestMethod]
    public void BinaryGridDeleteCol_NormalSizeEmptyGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000001_1111111;
        grid.DeleteCol(2);
        BinaryGrid test_grid = new BinaryGrid(5, 4);
        test_grid._grid = 0b111111_100001_100001_100001_100001_100001_111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridDeleteCol_NormalSizeAlternatingGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(4, 5);
        grid._grid = 0b1111111_1010101_1010101__1010101_1010101_1111111;
        grid.DeleteCol(2);
        BinaryGrid test_grid = new BinaryGrid(4, 4);
        test_grid._grid = 0b111111_101001_101001__101001_101001_111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }


    [TestMethod]
    public void BinaryGridDeleteCol_NormalSizeFullGraph_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.DeleteCol(2);
        BinaryGrid test_grid = new BinaryGrid(5, 4);
        test_grid._grid = 0b111111_111111_111111_111111_111111_111111_111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridDeleteCol_NormalSizeGraphSides_Valid()
    {
        BinaryGrid grid = new BinaryGrid(4, 5);
        grid._grid = 0b1111111_1000001_1010101__1000001_1010101_1111111;
        grid.DeleteCol(1);
        BinaryGrid test_grid = new BinaryGrid(4, 4);
        test_grid._grid = 0b111111_100001_101011__100001_101011_111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }


    [TestMethod]
    public void BinaryGridDeleteCol_NormalSizeBadIndexes_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(0));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(6));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(7));
    } 

    [TestMethod]
    public void BinaryGridDeleteCol_1HighGrid_Valid()
    {
        BinaryGrid grid = new BinaryGrid(1, 5);
        grid.DeleteCol(1);
        BinaryGrid test_grid = new BinaryGrid(1, 4);
        test_grid._grid = 0b111111_100001_111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    } 

    [TestMethod]
    public void BinaryGridDeleteCol_1WideGrid_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(0));
        grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(1));
        grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(2));
        grid = new BinaryGrid(5, 1);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(3));
    } 

    [TestMethod]
    public void BinaryGridDeleteCol_1HighGrid_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(0));
        grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(6));
        grid = new BinaryGrid(1, 5);
        Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(7));
    } 

    [TestMethod]
    public void BinaryGridSetSlice_0s_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid.SetSlice(1, 1, 5, 1, 1);
        BinaryGrid test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1000011_1000011_1000011_1000011_1000011_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

        grid = new BinaryGrid(5, 5);
        grid.SetSlice(2, 2, 4, 2, 1);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1000001_1000101_1000101_1000101_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

        grid = new BinaryGrid(5, 5);
        grid.SetSlice(1, 1, 1, 5, 1);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1111111_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

        grid = new BinaryGrid(5, 5);
        grid.SetSlice(2, 2, 2, 4, 1);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1000001_1000001_1000001_1011101_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    
        grid = new BinaryGrid(5, 5);
        grid.SetSlice(4, 2, 2, 2, 1);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1000001_1000101_1000101_1000101_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

    }

    [TestMethod]
    public void BinaryGridSetSlice_1s_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.SetSlice(1, 1, 5, 1, 0);
        BinaryGrid test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1111101_1111101_1111101_1111101_1111101_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("B")} grid: {((ulong)grid._grid).ToString("B")}");

        grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.SetSlice(2, 2, 4, 2, 0);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1111111_1111011_1111011_1111011_1111111_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

        grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.SetSlice(1, 1, 1, 5, 0);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

        grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.SetSlice(2, 2, 2, 4, 0);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1111111_1111111_1111111_1100011_1111111_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    
        grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1111111_1111111_1111111_1111111_1111111_1111111;
        grid.SetSlice(4, 2, 2, 2, 0);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1111111_1111011_1111011_1111011_1111111_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

    }

    [TestMethod]
    public void BinaryGridSetSlice_Mixed_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1010101_1111111;
        grid.SetSlice(1, 1, 5, 1, 1);
        BinaryGrid test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1010111_1010111_1010111_1010111_1010111_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

        grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1010101_1111111;
        grid.SetSlice(2, 2, 4, 2, 0);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1010101_1010001_1010001_1010001_1010101_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

        grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1010101_1111111;
        grid.SetSlice(1, 1, 1, 5, 0);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");

        grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1010101_1111111;
        grid.SetSlice(2, 2, 2, 4, 0);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1010101_1010101_1010101_1000001_1010101_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
        
        grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1010101_1111111;
        grid.SetSlice(2, 4, 2, 2, 0);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1010101_1010101_1010101_1000001_1010101_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
    }

    [TestMethod]
    public void BinaryGridSetGetSlice_DiagonalIndices_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.SetSlice(1, 1, 5, 5, 1), "Grid should not accept diagonal slicing");

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSlice(1, 1, 5, 5), "Grid should not accept diagonal slicing");
    }

    [TestMethod]
    public void BinaryGridSetGetSlice_SameStartEnd_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid.SetSlice(1, 1, 1, 1, 1);
        BinaryGrid test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1000001_1000001_1000001_1000001_1000011_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
        Assert.AreEqual(0b1, grid.GetSlice(1, 1, 1, 1));

        grid = new BinaryGrid(5, 5);
        grid.SetSlice(3, 4, 3, 4, 1);
        test_grid = new BinaryGrid(5, 5);
        test_grid._grid = 0b1111111_1000001_1000001_1010001_1000001_1000001_1111111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
        Assert.AreEqual(0b1, grid.GetSlice(3, 4, 3, 4));
    }

    [TestMethod]
    public void BinaryGridSetGetSlice_OutofBoundsIndices_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.SetSlice(0, 1, 4, 1, 1));
        Assert.Throws<Exception>(()=>grid.GetSlice(0, 1, 4, 1));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.SetSlice(4, 0, 4, 1, 1));
        Assert.Throws<Exception>(()=>grid.GetSlice(4, 0, 4, 1));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.SetSlice(4, 1, 0, 1, 1));
        Assert.Throws<Exception>(()=>grid.GetSlice(4, 1, 0, 1));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.SetSlice(0, 0, 4, 0, 1));
        Assert.Throws<Exception>(()=>grid.GetSlice(0, 0, 4, 0));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.SetSlice(6, 1, 6, 3, 1));
        Assert.Throws<Exception>(()=>grid.GetSlice(6, 1, 6, 3));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.SetSlice(4, 7, 4, 3, 1));
        Assert.Throws<Exception>(()=>grid.GetSlice(4, 7, 4, 3));
        }

    [TestMethod]
    public void BinaryGridSetGetSlice_1by1Grid_Valid()
    {
        BinaryGrid grid = new BinaryGrid(1, 1);
        grid.SetSlice(1, 1, 1, 1, 1);
        BinaryGrid test_grid = new BinaryGrid(1, 1);
        test_grid._grid = 0b111_111_111;
        Assert.AreEqual(test_grid, grid, $"test grid: {((ulong)test_grid._grid).ToString("b")} grid: {((ulong)grid._grid).ToString("b")}");
        Assert.AreEqual(0b1, grid.GetSlice(1, 1, 1, 1));
    }

    [TestMethod]
    public void BinaryGraphGetSliceOR_No1s_Valid0()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1000001_1111111;
        Assert.AreEqual<uint>(0, grid.GetSliceOR(1, 1, 1, 5));
        Assert.AreEqual<uint>(0, grid.GetSliceOR(1, 1, 5, 1));
    }

    [TestMethod]
    public void BinaryGraphGetSliceOR_1_1s_Valid1()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1001001_1111111;
        Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 1, 1, 5));
        Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 3, 5, 3));
    }

    [TestMethod]
    public void BinaryGraphGetSliceOR_No0s_Valid1()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1111111_1111111;
        Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 1, 1, 5));
        Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 2, 5, 2));
    }

    [TestMethod]
    public void BinaryGraphGetSliceOR_Diagonal_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 5, 5));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceOR(5, 1, 1, 5));
    }

    [TestMethod]
    public void BinaryGraphGetSliceOR_BadIndices_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 0, 1));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 6, 1));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 7, 1));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 1, 0));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 1, 6));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 1, 7));
    }

    [TestMethod]
    public void BinaryGraphGetSliceOR_SameStartEnd_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1111111_1111111;
        Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 1, 1, 1));
        Assert.AreEqual<uint>(0, grid.GetSliceOR(3, 3, 3, 3));
    }

    [TestMethod]
    public void BinaryGraphGetSliceOR_1by1_Valid()
    {
        BinaryGrid grid = new BinaryGrid(1, 1);
        Assert.AreEqual<uint>(0, grid.GetSliceOR(1, 1, 1, 1));
    }

    [TestMethod]
    public void BinaryGraphGetSliceAND_No1s_Valid0()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1000001_1111111;
        Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 1, 1, 5));
        Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 1, 5, 1));
    }

    [TestMethod]
    public void BinaryGraphGetSliceAND_1_1s_Valid0()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1001001_1111111;
        Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 1, 1, 5));
        Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 3, 5, 3));
    }

    [TestMethod]
    public void BinaryGraphGetSliceAND_No0s_Valid1()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1111111_1111111;
        Assert.AreEqual<uint>(1, grid.GetSliceAND(1, 1, 1, 5));
        Assert.AreEqual<uint>(1, grid.GetSliceAND(1, 2, 5, 2));
    }

    [TestMethod]
    public void BinaryGraphGetSliceAND_Diagonal_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 5, 5));
        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceAND(5, 1, 1, 5));
    }

    [TestMethod]
    public void BinaryGraphGetSliceAND_BadIndices_Invalid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 0, 1));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 6, 1));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 7, 1));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 1, 0));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 1, 6));

        grid = new BinaryGrid(5, 5);
        Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 1, 7));
    }

    [TestMethod]
    public void BinaryGraphGetSliceAND_SameStartEnd_Valid()
    {
        BinaryGrid grid = new BinaryGrid(5, 5);
        grid._grid = 0b1111111_1010101_1010101_1010101_1010101_1111111_1111111;
        Assert.AreEqual<uint>(1, grid.GetSliceAND(1, 1, 1, 1));
        Assert.AreEqual<uint>(0, grid.GetSliceAND(3, 3, 3, 3));
    }

    [TestMethod]
    public void BinaryGraphGetSliceAND_1by1_Valid()
    {
        BinaryGrid grid = new BinaryGrid(1, 1);
        Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 1, 1, 1));
    }

    [TestMethod]
    public void BinaryGridGetCellOR_All0NeighborsMiddle_Valid0()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellOR_EachDirectionAs1Middle_Valid1()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellOR_All1NeighborsMiddle_Valid1()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellOR_All0NeighborsCorners_Valid0()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellOR_EachDirectionAs1Corner_Valid1()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellOR_All1NeighborsCorner_Valid1()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellAND_All0NeighborsMiddle_Valid0()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellAND_EachDirectionAs1Middle_Valid0()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellAND_All1NeighborsMiddle_Valid1()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellAND_All0NeighborsCorners_Valid0()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellAND_EachDirectionAs1Corner_Valid0()
    {
        
    }

    [TestMethod]
    public void BinaryGridGetCellAND_All1NeighborsCorner_Valid1()
    {
        
    }

}