using System.Linq;
using NUnit.Framework;
using RelayZero.Arena;
using RelayZero.Arena.Baking;
using RelayZero.Editor.Arena;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RelayZero.Tests.EditMode.Arena
{
    public sealed class SwitchyardBakeFixtureTests
    {
        [Test]
        public void ApprovedFixtureLoadsWithoutSceneOrTransformDependencies()
        {
            ArenaBakeAsset asset = LoadBake();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ArenaBakeData data = asset.CreateRuntimeData();

            Assert.That(data.Elements, Has.Count.EqualTo(52));
            Assert.That(data.Walls, Has.Count.EqualTo(8));
            Assert.That(data.Obstacles, Has.Count.EqualTo(4));
            Assert.That(data.NavigationNodes, Has.Count.EqualTo(17));
            Assert.That(data.NavigationEdges, Has.Count.GreaterThan(0));
            Assert.That(asset.TransformReferenceCount, Is.Zero);
            Assert.That(
                typeof(ArenaBakeData).Assembly.GetReferencedAssemblies().Select(reference => reference.Name),
                Does.Not.Contain("UnityEngine.CoreModule"));
        }

        [Test]
        public void ApprovedFixtureHashRoundTripsAndRebakesDeterministically()
        {
            ArenaBakeAsset asset = LoadBake();
            string persistedHash = asset.ContentHash;

            Assert.That(ArenaBakeHasher.Compute(asset.CreateRuntimeData()), Is.EqualTo(persistedHash));
            ArenaValidationResult result = ArenaBakeProcessor.ValidateAndBakeSwitchyard();
            Assert.That(result.Report.IsValid, Is.True, result.Report.FormatForLog());
            Assert.That(result.ContentHash, Is.EqualTo(persistedHash));
            Assert.That(LoadBake().ContentHash, Is.EqualTo(persistedHash));
        }

        [Test]
        public void ApprovedFixtureContainsCanonicalPrecomputedGeometryAndSymmetricAdjacency()
        {
            ArenaBakeData data = LoadBake().CreateRuntimeData();
            foreach (BakedConvexObstacle obstacle in data.Obstacles)
            {
                Assert.That(obstacle.Normals, Has.Count.EqualTo(obstacle.Vertices.Count));
                Assert.That(obstacle.PlayerExpandedBounds.Minimum.x, Is.LessThan(obstacle.Bounds.Minimum.x));
                Assert.That(obstacle.PlayerExpandedBounds.Maximum.y, Is.GreaterThan(obstacle.Bounds.Maximum.y));
                Assert.That(ArenaGeometry.SignedArea(obstacle.Vertices), Is.GreaterThan(0f));
                Assert.That(obstacle.Vertices[0].x, Is.EqualTo(obstacle.Vertices.Min(vertex => vertex.x)));
            }

            foreach (BakedCircle circle in data.Circles)
            {
                Assert.That(circle.RadiusSquared, Is.EqualTo(circle.Radius * circle.Radius).Within(0.00001f));
            }

            foreach (BakedNavigationNode node in data.NavigationNodes)
            {
                foreach (ArenaElementId neighbor in node.NeighborIds)
                {
                    BakedNavigationNode counterpart = data.NavigationNodes.Single(candidate => candidate.Id == neighbor);
                    Assert.That(counterpart.NeighborIds, Does.Contain(node.Id));
                    Assert.That(data.NavigationEdges.Any(edge =>
                        (edge.NodeA == node.Id && edge.NodeB == neighbor) ||
                        (edge.NodeA == neighbor && edge.NodeB == node.Id)), Is.True);
                }
            }
        }

        [TestCase(16f / 9f)]
        [TestCase(16f / 10f)]
        [TestCase(21f / 9f)]
        public void ApprovedFixtureFitsTargetCameraRatio(float aspectRatio)
        {
            ArenaBakeData data = LoadBake().CreateRuntimeData();

            Assert.That(data.CameraBounds.OrthographicSize, Is.GreaterThanOrEqualTo(data.Bounds.HalfExtents.y));
            Assert.That(data.CameraBounds.OrthographicSize * aspectRatio, Is.GreaterThanOrEqualTo(data.Bounds.HalfExtents.x));
        }

        private static ArenaBakeAsset LoadBake()
        {
            ArenaBakeAsset asset = AssetDatabase.LoadAssetAtPath<ArenaBakeAsset>(ArenaBakeProcessor.SwitchyardBakeAssetPath);
            Assert.That(asset, Is.Not.Null, "Bake the Switchyard arena before executing the fixture suite.");
            Assert.That(asset.BakeVersion, Is.EqualTo(1));
            Assert.That(asset.ContentHash, Has.Length.EqualTo(64));
            return asset;
        }
    }
}
