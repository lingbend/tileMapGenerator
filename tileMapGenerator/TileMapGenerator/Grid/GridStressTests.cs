using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grid
{
#pragma warning disable MSTEST0045

    [TestClass]
    public class GridStressTests
    {


        [Timeout(1000)]
        [TestMethod]
        public void GridConstructor_200_Valid()
        {
            _ = new Grid(200, 200);
        }

        [Timeout(1000)]
        [TestMethod]
        public void GridConstructor_400_Valid()
        {
            _ = new Grid(400, 400);
        }

        [Timeout(1000)]
        [TestMethod]
        public void GridConstructor_800_Valid()
        {
            _ = new Grid(800, 800);
        }

        [Timeout(1000)]
        [TestMethod]
        public void GridConstructor_1600_Valid()
        {
            _ = new Grid(1600, 1600);
        }

        [Timeout(1000)]
        [TestMethod]
        public void GridConstructor_3200_Valid()
        {
            _ = new Grid(3200, 3200);
        }

        [Timeout(10000)]
        [TestMethod]
        public void GridGetSlice_Huge_Valid()
        {
            Grid grid = new Grid(800, 800);
            for (uint i = 0; i < 1000; i++)
            {
                _ = grid.GetSlice((i%800)+1, (i%800)+1, (i%800)+1, (uint) ((i*.5)%800)+1);
            }
        }

        [Timeout(10000)]
        [TestMethod]
        public void GridSetSlice_Huge_Valid()
        {
            Grid grid = new Grid(800, 800);
            for (uint i = 0; i < 1000; i++)
            {
                grid.SetSlice((i%800)+1, (i%800)+1, (i%800)+1, (uint) ((i*.25)%800)+1, 1);
            }
        }

        [Timeout(10000)]
        [TestMethod]
        public void GridInsertRowCol_Huge_Valid()
        {
            Grid grid = new Grid(800, 800);
            for (uint i = 0; i < 1000; i++)
            {
                grid.InsertRow((i % 800) + 1);
                grid.InsertRow((i % 800) + 1);
            }
        }

        [Timeout(10000)]
        [TestMethod]
        public void GridGetSliceORAND_Huge_Valid()
        {
            Grid grid = new Grid(800, 800);
            for (uint i = 0; i < 1000; i++)
            {
                _ = grid.GetSliceOR((i%800)+1, (i%800)+1, (i%800)+1, (uint) ((i*.5)%800)+1);
                _ = grid.GetSliceAND((i%800)+1, (i%800)+1, (i%800)+1, (uint) ((i*.5)%800)+1);
            }
        }

        [Timeout(10000)]
        [TestMethod]
        public void GridDeleteRowCol_Huge_Valid()
        {
            Grid grid = new Grid(800, 800);
            for (uint i = 0; i < 799; i++)
            {
                grid.DeleteRow(800 - i);
                grid.DeleteCol(800 - i);
            }
        }
    
    }
}