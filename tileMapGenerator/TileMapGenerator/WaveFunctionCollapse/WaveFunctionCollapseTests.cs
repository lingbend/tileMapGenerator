namespace WaveFunctionCollapse;

[TestClass]
public class WaveFunctionCollapseTests
{

    Dictionary<int, int> degree_weights = new Dictionary<int, int>([KeyValuePair.Create(1, 10), KeyValuePair.Create(2, 30), KeyValuePair.Create(3, 30), KeyValuePair.Create(4, 35)]);

    [TestMethod]
    public void WaveFunctionCollapse_Valid()
    {
        //  <overlapping name="NotKnot" N="3" periodic="True" periodicInput="False"/>
        var WaveSettings = new WaveFunctionCollapseSettings("Knots");
        WaveSettings.Periodic = true;
        WaveSettings.InitializeSimple("Standard");
        var WaveGenerator = new WaveFunctionCollapse("Knots");
        WaveGenerator.Run();
    }
}