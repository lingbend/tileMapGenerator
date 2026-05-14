using System.Diagnostics;
using System.Numerics;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Grid
{
    [TestClass]
    public class GridTests
    {
        [TestMethod]
        public void GridConstructor_0Constructor_Valid()
        {
            _ = new Grid(5, 5);
        }

        [TestMethod]
        public void GridConstructor_DefaultConstructor_Valid()
        {
            _ = new Grid(5, 5, 1);
        }

        [TestMethod]
        public void GridConstructor_2Border_Invalid()
        {
            Assert.Throws<ArgumentException>(() => new Grid(5, 5, 2));
        }

        [TestMethod]
        public void GridConstructor_NoSize_Invalid()
        {
            Assert.Throws<Exception>(() => new Grid(0,0));
        }

        [TestMethod]
        public void GridConstructor_100x100_ValidFullSize()
        {
            _ = new Grid(100, 100);
        }

        [TestMethod]
        public void GridConstructor_0Rows_Invalid()
        {
            Assert.Throws<Exception>(() => new Grid(0,5));
        }

        [TestMethod]
        public void GridConstructor_0Cols_Invalid()
        {
            Assert.Throws<Exception>(() => new Grid(5,0));
        }

        [TestMethod]
        public void GridChangeBorders_1SameOnEmpty_ValidNoChange()
        {
            Grid grid = new Grid(5, 5);
            grid.ChangeBorders(1);
        }

        [TestMethod]
        public void GridChangeBorders_0BorderOnEmpty_ValidAllZeroes()
        {
            Grid grid = new Grid(5, 5);
            grid.ChangeBorders(0);
        }

        [TestMethod]
        public void GridChangeBorders_2Border_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<ArgumentException>(() => grid.ChangeBorders(2));
        }

        [TestMethod]
        public void GridChangeBorders_1SameFullGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid.SetSlice(1, 1, 1, 5, 1);
            grid.SetSlice(2, 1, 2, 5, 1);
            grid.SetSlice(3, 1, 3, 5, 1);
            grid.SetSlice(4, 1, 4, 5, 1);
            grid.SetSlice(5, 1, 5, 5, 1);
            grid.ChangeBorders(1);
            Grid testGrid = new Grid(5, 5);
            testGrid._back = ToNumber(0b1_111_111_111_111_111_111_111_111_111_111_111_111_111_111_111_111);
            Assert.AreEqual(testGrid, grid, $"test grid: {NumberToLong(testGrid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");;
        }

        [TestMethod]
        public void GridChangeBorders_0FullGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid.SetSlice(1, 1, 1, 5, 1);
            grid.SetSlice(2, 1, 2, 5, 1);
            grid.SetSlice(3, 1, 3, 5, 1);
            grid.SetSlice(4, 1, 4, 5, 1);
            grid.SetSlice(5, 1, 5, 5, 1);
            grid.ChangeBorders(0);
            Grid testGrid = new Grid(5, 5);
            testGrid._back = ToNumber(0b0_000_000_011_111_001_111_100_111_110_011_111_001_111_100_000_000);
            Assert.AreEqual(testGrid, grid, $"test grid: {NumberToLong(testGrid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");;
        }

        [TestMethod]
        public void GridChangeBorders_2FullGraph_Invalid()
        {
            Grid grid = new Grid(5, 5);
            grid.SetSlice(1, 1, 1, 5, 1);
            grid.SetSlice(2, 1, 2, 5, 1);
            grid.SetSlice(3, 1, 3, 5, 1);
            grid.SetSlice(4, 1, 4, 5, 1);
            grid.SetSlice(5, 1, 5, 5, 1);
            Assert.Throws<ArgumentException>(()=>grid.ChangeBorders(2));
        }

        [TestMethod]
        public void GridGetSetCell_0MiddleCellEmptyGraph_0Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000101_1000001_1111111);
            grid.SetCell(2, 2, 0);
            Assert.AreEqual<uint>(0, grid.GetCell(2, 2));
        }

        [TestMethod]
        public void GridGetSetCell_1MiddleCellEmptyGraph_1Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            grid.SetCell(2, 2, 1);
            Assert.AreEqual<uint>(1, grid.GetCell(2, 2));
        }

        [TestMethod]
        public void GridSetCell_2MiddleCellEmptyGraph_Invalid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000101_1111111);
            Assert.Throws<ArgumentException>(()=>grid.SetCell(2, 2, 2));
        }

        [TestMethod]
        public void GridGetSetCell_0MiddleCellFullGraph_0Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.SetCell(2, 2, 0);
            Assert.AreEqual<uint>(0, grid.GetCell(2, 2));
        }

        [TestMethod]
        public void GridGetSetCell_1MiddleCellFullGraph_1Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.SetCell(2, 2, 1);
            Assert.AreEqual<uint>(1, grid.GetCell(2, 2));
        }

        [TestMethod]
        public void GridSetCell_2MiddleCellFullGraph_Invalid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            Assert.Throws<ArgumentException>(()=>grid.SetCell(2, 2, 2));
        }

        [TestMethod]
        public void GridGetSetCell_0CornerCellEmptyGraph_0Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000011_1111111);
            grid.SetCell(1, 1, 0);
            Assert.AreEqual<uint>(0, grid.GetCell(1, 1));
        }

        [TestMethod]
        public void GridGetSetCell_1CornerCellEmptyGraph_1Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            grid.SetCell(1, 1, 1);
            Assert.AreEqual<uint>(1, grid.GetCell(1, 1));
        }

        [TestMethod]
        public void GridSetCell_2CornerCellEmptyGraph_Invalid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            Assert.Throws<ArgumentException>(()=>grid.SetCell(1, 1, 2));
        }

        [TestMethod]
        public void GridGetCellNeighbors_All0NeighborsMiddle_0b00000000()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            Assert.AreEqual<uint>(0b000_000_000, grid.GetCellNeighbors(2, 2));
        }

        [TestMethod]
        public void GridGetCellNeighbors_EachDirectionAs1Middle_All0sExcept1()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000101_1000001_1111111);
            Assert.AreEqual<uint>(0b100_000_000, grid.GetCellNeighbors(2, 2), "self");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1001001_1000001_1111111);
            Assert.AreEqual<uint>(0b010_000_000, grid.GetCellNeighbors(2, 2), "right");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1001001_1111111);
            Assert.AreEqual<uint>(0b001_000_000, grid.GetCellNeighbors(2, 2), "upper right");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000101_1111111);
            Assert.AreEqual<uint>(0b000_100_000, grid.GetCellNeighbors(2, 2), "up");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000011_1111111);
            Assert.AreEqual<uint>(0b000_010_000, grid.GetCellNeighbors(2, 2), "upper left");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000011_1000001_1111111);
            Assert.AreEqual<uint>(0b000_001_000, grid.GetCellNeighbors(2, 2), "left");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000011_1000001_1000001_1111111);
            Assert.AreEqual<uint>(0b000_000_100, grid.GetCellNeighbors(2, 2), "lower left");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000101_1000001_1000000_1111111);
            Assert.AreEqual<uint>(0b000_000_010, grid.GetCellNeighbors(2, 2), "down");

            grid._back = ToNumber(0b1111111_1000001_1000001_1001001_1000001_1000001_1111111);
            Assert.AreEqual<uint>(0b000_000_001, grid.GetCellNeighbors(2, 2), "lower right");
        }

        [TestMethod]
        public void GridGetCellNeighbors_All1NeighborsMiddle_0b111111111()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1001111_1001111_1001111_1111111);
            Assert.AreEqual<uint>(0b111_111_111, grid.GetCellNeighbors(2, 2));
        }

        [TestMethod]
        public void GridGetCellNeighbors_All0NeighborsCorners_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            Assert.AreEqual<uint>(0b001_111_100, grid.GetCellNeighbors(1, 1), "upper left corner");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            Assert.AreEqual<uint>(0b011_110_001, grid.GetCellNeighbors(1, 5), "upper right corner");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            Assert.AreEqual<uint>(0b000_011_111, grid.GetCellNeighbors(5, 1), "lower left corner");

            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            Assert.AreEqual<uint>(0b011_000_111, grid.GetCellNeighbors(5, 5), "lower right corner");
        }

        [TestMethod]
        public void GridInsertRow_NormalSizeEmptyGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            grid.InsertRow(2);
            Grid test_grid = new Grid(6, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridInsertRow_NormalSizeAlternatingGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1010101_1111111);
            grid.InsertRow(2);
            Grid test_grid = new Grid(6, 5);
            test_grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1000001_1010101_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridInsertRow_NormalSizeFullGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.InsertRow(2);
            Grid test_grid = new Grid(6, 5);
            test_grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1000001_1111111_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridInsertRow_NormalSizeEmptyGraphSides_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            grid.InsertRow(1);
            Grid test_grid = new Grid(6, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridInsertRow_NormalSizeFullGraphSides_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.InsertRow(1);
            Grid test_grid = new Grid(6, 5);
            test_grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridInsertRow_NormalSizeBadIndexes_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(0));
            grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(6));
            grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(7));
        } 

        [TestMethod]
        public void GridInsertRow_1WideGrid_Valid()
        {
            Grid grid = new Grid(5, 1);
            grid.InsertRow(1);
            Grid test_grid = new Grid(6, 1);
            test_grid._back = ToNumber(0b111_101_101_101_101_101_101_111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        } 

        [TestMethod]
        public void GridInsertRow_1HighGrid_Valid()
        {
            Grid grid = new Grid(1, 5);
            grid.InsertRow(1);
            Grid test_grid = new Grid(2, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        } 

        [TestMethod]
        public void GridInsertRow_1WideGrid_Invalid()
        {
            Grid grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(6));
            grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(0));
            grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(7));
        } 

        [TestMethod]
        public void GridInsertRow_1HighGrid_Invalid()
        {
            Grid grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(0));
            grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(2));
            grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertRow(3));
        } 


        [TestMethod]
        public void GridInsertCol_NormalSizeEmptyGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            grid.InsertCol(2);
            Grid test_grid = new Grid(5, 6);
            test_grid._back = ToNumber(0b11111111_10000001_10000001_10000001_10000001_10000001_11111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridInsertCol_NormalSizeAlternatingGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1010101_1111111);
            grid.InsertCol(2);
            Grid test_grid = new Grid(5, 6);
            test_grid._back = ToNumber(0b11111111_10101001_10101001_10101001_10101001_10101001_11111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }


        [TestMethod]
        public void GridInsertCol_NormalSizeFullGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.InsertCol(2);
            Grid test_grid = new Grid(5, 6);
            test_grid._back = ToNumber(0b11111111_11111011_11111011_11111011_11111011_11111011_11111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridInsertCol_NormalSizeEmptyGraphSides_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            grid.InsertCol(1);
            Grid test_grid = new Grid(5, 6);
            test_grid._back = ToNumber(0b11111111_10000001_10000001_10000001_10000001_10000001_11111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridInsertCol_NormalSizeFullGraphSides_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.InsertCol(1);
            Grid test_grid = new Grid(5, 6);
            test_grid._back = ToNumber(0b11111111_11111101_11111101_11111101_11111101_11111101_11111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridInsertCol_NormalSizeBadIndexes_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(0));
            grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(6));
            grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(7));
        }

        [TestMethod]
        public void GridInsertCol_1WideGrid_Valid()
        {
            Grid grid = new Grid(5, 1);
            grid.InsertCol(1);
            Grid test_grid = new Grid(5, 2);
            test_grid._back = ToNumber(0b1111_1001_1001_1001_1001_1001_1111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        } 

        [TestMethod]
        public void GridInsertCol_1HighGrid_Valid()
        {
            Grid grid = new Grid(1, 5);
            grid.InsertCol(1);
            Grid test_grid = new Grid(1, 6);
            test_grid._back = ToNumber(0b11111111_10000001_11111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        } 

        [TestMethod]
        public void GridInsertCol_1WideGrid_Invalid()
        {
            Grid grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(0));
            grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(2));
            grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(3));
        } 

        [TestMethod]
        public void GridInsertCol_1HighGrid_Invalid()
        {
            Grid grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(0));
            grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(6));
            grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.InsertCol(7));
        }  

        [TestMethod]
        public void GridDeleteRow_NormalSizeEmptyGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            grid.DeleteRow(2);
            Grid test_grid = new Grid(4, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridDeleteRow_NormalSizeAlternatingGraph_Valid()
        {
            Grid grid = new Grid(4, 5);
            grid._back = ToNumber(0b1111111_1000001_1010101__1000001_1010101_1111111);
            grid.DeleteRow(2);
            Grid test_grid = new Grid(3, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1010101__1010101_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridDeleteRow_NormalSizeFullGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.DeleteRow(2);
            Grid test_grid = new Grid(4, 5);
            test_grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridDeleteRow_NormalSizeGraphSides_Valid()
        {
            Grid grid = new Grid(4, 5);
            grid._back = ToNumber(0b1111111_1000001_1010101__1000001_1010101_1111111);
            grid.DeleteRow(1);
            Grid test_grid = new Grid(3, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1010101__1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridDeleteRow_NormalSizeBadIndexes_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(0));
            grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(6));
            grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(7));
        } 
    
        [TestMethod]
        public void GridDeleteRow_1WideGrid_Valid()
        {
            Grid grid = new Grid(5, 1);
            grid.DeleteRow(1);
            Grid test_grid = new Grid(4, 1);
            test_grid._back = ToNumber(0b111_101_101_101_101_111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        } 

        [TestMethod]
        public void GridDeleteRow_1WideGrid_Invalid()
        {
            Grid grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(6));
            grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(0));
            grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(7));
        } 

        [TestMethod]
        public void GridDeleteRow_1HighGrid_Invalid()
        {
            Grid grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(0));
            grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(1));
            grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(2));
            grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteRow(3));
        } 

        [TestMethod]
        public void GridDeleteCol_NormalSizeEmptyGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            grid.DeleteCol(2);
            Grid test_grid = new Grid(5, 4);
            test_grid._back = ToNumber(0b111111_100001_100001_100001_100001_100001_111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridDeleteCol_NormalSizeAlternatingGraph_Valid()
        {
            Grid grid = new Grid(4, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101__1010101_1010101_1111111);
            grid.DeleteCol(2);
            Grid test_grid = new Grid(4, 4);
            test_grid._back = ToNumber(0b111111_101001_101001__101001_101001_111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }


        [TestMethod]
        public void GridDeleteCol_NormalSizeFullGraph_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.DeleteCol(2);
            Grid test_grid = new Grid(5, 4);
            test_grid._back = ToNumber(0b111111_111111_111111_111111_111111_111111_111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridDeleteCol_NormalSizeGraphSides_Valid()
        {
            Grid grid = new Grid(4, 5);
            grid._back = ToNumber(0b1111111_1000001_1010101__1000001_1010101_1111111);
            grid.DeleteCol(1);
            Grid test_grid = new Grid(4, 4);
            test_grid._back = ToNumber(0b111111_100001_101011__100001_101011_111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }


        [TestMethod]
        public void GridDeleteCol_NormalSizeBadIndexes_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(0));
            grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(6));
            grid = new Grid(5, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(7));
        } 

        [TestMethod]
        public void GridDeleteCol_1HighGrid_Valid()
        {
            Grid grid = new Grid(1, 5);
            grid.DeleteCol(1);
            Grid test_grid = new Grid(1, 4);
            test_grid._back = ToNumber(0b111111_100001_111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        } 

        [TestMethod]
        public void GridDeleteCol_1WideGrid_Invalid()
        {
            Grid grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(0));
            grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(1));
            grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(2));
            grid = new Grid(5, 1);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(3));
        } 

        [TestMethod]
        public void GridDeleteCol_1HighGrid_Invalid()
        {
            Grid grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(0));
            grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(6));
            grid = new Grid(1, 5);
            Assert.Throws<IndexOutOfRangeException>(()=>grid.DeleteCol(7));
        } 

        [TestMethod]
        public void GridSetSlice_0s_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid.SetSlice(1, 1, 5, 1, 1);
            Grid test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1000011_1000011_1000011_1000011_1000011_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

            grid = new Grid(5, 5);
            grid.SetSlice(2, 2, 4, 2, 1);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000101_1000101_1000101_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

            grid = new Grid(5, 5);
            grid.SetSlice(1, 1, 1, 5, 1);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1111111_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

            grid = new Grid(5, 5);
            grid.SetSlice(2, 2, 2, 4, 1);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1011101_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
    
            grid = new Grid(5, 5);
            grid.SetSlice(4, 2, 2, 2, 1);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000101_1000101_1000101_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

        }

        [TestMethod]
        public void GridSetSlice_1s_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.SetSlice(1, 1, 5, 1, 0);
            Grid test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1111101_1111101_1111101_1111101_1111101_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.SetSlice(2, 2, 4, 2, 0);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1111111_1111011_1111011_1111011_1111111_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.SetSlice(1, 1, 1, 5, 0);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.SetSlice(2, 2, 2, 4, 0);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1100011_1111111_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
    
            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.SetSlice(4, 2, 2, 2, 0);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1111111_1111011_1111011_1111011_1111111_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

        }

        [TestMethod]
        public void GridSetSlice_Mixed_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1010101_1111111);
            grid.SetSlice(1, 1, 5, 1, 1);
            Grid test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1010111_1010111_1010111_1010111_1010111_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1010101_1111111);
            grid.SetSlice(2, 2, 4, 2, 0);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1010101_1010001_1010001_1010001_1010101_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1010101_1111111);
            grid.SetSlice(1, 1, 1, 5, 0);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");

            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1010101_1111111);
            grid.SetSlice(2, 2, 2, 4, 0);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1000001_1010101_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        
            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1010101_1111111);
            grid.SetSlice(2, 4, 2, 2, 0);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1000001_1010101_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
        }

        [TestMethod]
        public void GridSetGetSlice_DiagonalIndices_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.SetSlice(1, 1, 5, 5, 1), "Grid should not accept diagonal slicing");

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSlice(1, 1, 5, 5), "Grid should not accept diagonal slicing");
        }

        [TestMethod]
        public void GridSetGetSlice_SameStartEnd_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid.SetSlice(1, 1, 1, 1, 1);
            Grid test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000011_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
            Assert.AreEqual(0b1U, grid.GetSlice(1, 1, 1, 1));

            grid = new Grid(5, 5);
            grid.SetSlice(3, 4, 3, 4, 1);
            test_grid = new Grid(5, 5);
            test_grid._back = ToNumber(0b1111111_1000001_1000001_1010001_1000001_1000001_1111111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
            Assert.AreEqual(0b1U, grid.GetSlice(3, 4, 3, 4));
        }

        [TestMethod]
        public void GridSetGetSlice_OutofBoundsIndices_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.SetSlice(0, 1, 4, 1, 1));
            Assert.Throws<Exception>(()=>grid.GetSlice(0, 1, 4, 1));
            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.SetSlice(4, 0, 4, 1, 1));
            Assert.Throws<Exception>(()=>grid.GetSlice(4, 0, 4, 1));
            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.SetSlice(4, 1, 0, 1, 1));
            Assert.Throws<Exception>(()=>grid.GetSlice(4, 1, 0, 1));
            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.SetSlice(0, 0, 4, 0, 1));
            Assert.Throws<Exception>(()=>grid.GetSlice(0, 0, 4, 0));
            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.SetSlice(6, 1, 6, 3, 1));
            Assert.Throws<Exception>(()=>grid.GetSlice(6, 1, 6, 3));
            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.SetSlice(4, 7, 4, 3, 1));
            Assert.Throws<Exception>(()=>grid.GetSlice(4, 7, 4, 3));
            }

        [TestMethod]
        public void GridSetGetSlice_1by1Grid_Valid()
        {
            Grid grid = new Grid(1, 1);
            grid.SetSlice(1, 1, 1, 1, 1);
            Grid test_grid = new Grid(1, 1);
            test_grid._back = ToNumber(0b111_111_111);
            Assert.AreEqual(test_grid, grid, $"test grid: {NumberToLong(test_grid._back).ToString("b")} grid: {NumberToLong(grid._back ).ToString("b")}");
            Assert.AreEqual(0b1U, grid.GetSlice(1, 1, 1, 1));
        }

        [TestMethod]
        public void GraphGetSliceOR_No1s_Valid0()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1000001_1111111);
            Assert.AreEqual<uint>(0, grid.GetSliceOR(1, 1, 1, 5));
            Assert.AreEqual<uint>(0, grid.GetSliceOR(1, 1, 5, 1));
        }

        [TestMethod]
        public void GraphGetSliceOR_1_1s_Valid1()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1001001_1111111);
            Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 1, 1, 5));
            Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 3, 5, 3));
        }

        [TestMethod]
        public void GraphGetSliceOR_No0s_Valid1()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1111111_1111111);
            Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 1, 1, 5));
            Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 2, 5, 2));
        }

        [TestMethod]
        public void GraphGetSliceOR_Diagonal_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 5, 5));
            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceOR(5, 1, 1, 5));
        }

        [TestMethod]
        public void GraphGetSliceOR_BadIndices_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 0, 1));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 6, 1));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 7, 1));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 1, 0));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 1, 6));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceOR(1, 1, 1, 7));
        }

        [TestMethod]
        public void GraphGetSliceOR_SameStartEnd_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1111111_1111111);
            Assert.AreEqual<uint>(1, grid.GetSliceOR(1, 1, 1, 1));
            Assert.AreEqual<uint>(0, grid.GetSliceOR(3, 3, 3, 3));
        }

        [TestMethod]
        public void GraphGetSliceOR_1by1_Valid()
        {
            Grid grid = new Grid(1, 1);
            Assert.AreEqual<uint>(0, grid.GetSliceOR(1, 1, 1, 1));
        }

        [TestMethod]
        public void GraphGetSliceAND_No1s_Valid0()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1000001_1111111);
            Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 1, 1, 5));
            Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 1, 5, 1));
        }

        [TestMethod]
        public void GraphGetSliceAND_1_1s_Valid0()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1001001_1111111);
            Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 1, 1, 5));
            Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 3, 5, 3));
        }

        [TestMethod]
        public void GraphGetSliceAND_No0s_Valid1()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1111111_1111111);
            Assert.AreEqual<uint>(1, grid.GetSliceAND(1, 1, 1, 5));
            Assert.AreEqual<uint>(1, grid.GetSliceAND(1, 2, 5, 2));
        }

        [TestMethod]
        public void GraphGetSliceAND_Diagonal_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 5, 5));
            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceAND(5, 1, 1, 5));
        }

        [TestMethod]
        public void GraphGetSliceAND_BadIndices_Invalid()
        {
            Grid grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 0, 1));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 6, 1));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 7, 1));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 1, 0));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 1, 6));

            grid = new Grid(5, 5);
            Assert.Throws<Exception>(()=>grid.GetSliceAND(1, 1, 1, 7));
        }

        [TestMethod]
        public void GraphGetSliceAND_SameStartEnd_Valid()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1111111_1111111);
            Assert.AreEqual<uint>(1, grid.GetSliceAND(1, 1, 1, 1));
            Assert.AreEqual<uint>(0, grid.GetSliceAND(3, 3, 3, 3));
        }

        [TestMethod]
        public void GraphGetSliceAND_1by1_Valid()
        {
            Grid grid = new Grid(1, 1);
            Assert.AreEqual<uint>(0, grid.GetSliceAND(1, 1, 1, 1));
        }

        private Backing ToNumber(long num)
        {
            return new Backing((ulong) num);
        }

        private long NumberToLong(Backing binNum)
        {
            return (long) binNum.ToULong();
        }


        [TestMethod]
        public void GraphToBMP_ByHand()
        {
            Grid grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1001111_1001111_1001111_1111111);
            grid.ToBMP();
            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1010101_1010101_1010101_1010101_1111111_1111111);
            grid.ToBMP();
            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1000001_1000001_1000001_1000001_1000001_1111111);
            grid.ToBMP();
            grid = new Grid(5, 5);
            grid._back = ToNumber(0b1111111_1111111_1111111_1111111_1111111_1111111_1111111);
            grid.ToBMP();
        }
    
    }


}
