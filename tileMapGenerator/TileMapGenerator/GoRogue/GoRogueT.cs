namespace GoRogueWrapper
{
    using System.Numerics;
    using CRandom;
    using Microsoft.VisualStudio.TestTools.UnitTesting;


    [TestClass]
    public class GoRogueTests
    {
        // [Fact]
        [TestMethod]
        public void GetSimpleDirectHallTest()
        {
            GoRogue.GetSimpleDirectHall(new Vector2(2, 2), new Vector2(9, 6));
        }

        [TestMethod]
        public void GetSimpleHorizontalVerticalHallTest()
        {
            GoRogue.GetSimpleHorizontalVerticalHall(new Vector2(2, 2), new Vector2(9, 6), "chicken", new CRandom(123));
        }

    }
}