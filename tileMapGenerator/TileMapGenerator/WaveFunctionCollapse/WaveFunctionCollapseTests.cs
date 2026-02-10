namespace WaveFunctionCollapse;

[TestClass]
public class WaveFunctionCollapseTests
{

    [TestMethod]
    public void WaveFunctionCollapse_Valid()
    {
        var WaveSettings = new WaveFunctionCollapseSettings("Knots");
        WaveSettings.Periodic = true;
        WaveSettings.InitializeSimple("Standard");
        var WaveGenerator = new WaveFunctionCollapse("Knots");
        WaveGenerator.Run();
    }
}