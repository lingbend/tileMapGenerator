
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Generator = TileMapGenerator.TileMapGenerator;

[TestClass]
public class TileMapGeneratorTests
{
    [TestMethod]
    public void Generic_Test()
    {
        // Generator.NodeTreeGeneratorSettings.Random = new System.Random(1234);
        var (grid, doors) = Generator.GenerateMap();
        grid.ToBMP("TileMapGeneratorTests");
    }
}