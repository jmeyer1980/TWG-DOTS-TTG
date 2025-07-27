using NUnit.Framework;

namespace TinyWalnutGames.TTG.TerrainGeneration.Tests
{
    /// <summary>
    /// Simple test to verify ECSTestsFixture works correctly.
    /// </summary>
    [TestFixture]
    public class ECSTestsFixtureVerificationTests : ECSTestsFixture
    {
        [Test]
        public void ECSTestsFixture_CanCreateEntities()
        {
            // Test that we can create entities
            var entity = CreateEntity();
            Assert.AreNotEqual(default, entity);
            Assert.IsTrue(Manager.Exists(entity));
        }

        [Test]
        public void ECSTestsFixture_WorldIsValid()
        {
            // Test that the World is properly set up
            Assert.IsNotNull(World);
            Assert.IsTrue(World.IsCreated);
            Assert.IsNotNull(Manager);
        }
    }
}