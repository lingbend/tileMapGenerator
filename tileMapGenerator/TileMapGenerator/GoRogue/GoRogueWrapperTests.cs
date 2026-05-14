namespace GoRogueWrapper
{
    using System.Numerics;
    using ConcRandom;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GoRogueWrapperTests
    {
        [TestMethod]
        public void GetSimpleDirectHallTest()
        {
            GoRogueWrapper.GetSimpleDirectHall(new Vector2(2, 2), new Vector2(9, 6));
        }

        [TestMethod]
        public void GetSimpleHorizontalVerticalHallTest()
        {
            GoRogueWrapper.GetSimpleHorizontalVerticalHall(new Vector2(2, 2), new Vector2(9, 6), "chicken", new ConcRandom(123));
        }

    }
}