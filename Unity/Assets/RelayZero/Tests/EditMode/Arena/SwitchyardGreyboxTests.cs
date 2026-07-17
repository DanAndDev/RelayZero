using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RelayZero.Arena.Authoring;
using RelayZero.Editor.Arena;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RelayZero.Tests.EditMode.Arena
{
    public sealed class SwitchyardGreyboxTests
    {
        private static readonly Regex StableIdPattern = new Regex(
            "^[a-z0-9]+(?:[.-][a-z0-9]+)*$",
            RegexOptions.CultureInvariant);

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ArenaBakeProcessor.SwitchyardScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void AuthoredSceneContainsEveryRequiredAuthoringCategory()
        {
            Assert.That(Find<ArenaBoundsAuthoring>(), Has.Length.EqualTo(1));
            Assert.That(Find<ArenaWallAuthoring>(), Has.Length.EqualTo(8));
            Assert.That(Find<ConvexPylonAuthoring>(), Has.Length.EqualTo(4));
            Assert.That(Find<RelayZoneAuthoring>(), Has.Length.EqualTo(2));
            Assert.That(Find<TerminalAuthoring>(), Has.Length.EqualTo(2));
            Assert.That(Find<ShockGateAuthoring>(), Has.Length.EqualTo(2));
            Assert.That(Find<BoostPadAuthoring>(), Has.Length.EqualTo(4));
            Assert.That(Find<SpawnPointAuthoring>(), Has.Length.EqualTo(2));
            Assert.That(Find<CoreResetPointAuthoring>(), Has.Length.EqualTo(1));
            Assert.That(Find<BarrierForbiddenVolumeAuthoring>(), Has.Length.EqualTo(7));
            Assert.That(Find<NavigationHintAuthoring>(), Has.Length.EqualTo(17));
            Assert.That(Find<CameraBoundsAuthoring>(), Has.Length.EqualTo(1));
            Assert.That(Find<SwitchyardSceneLegendAuthoring>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void AuthoredSceneUsesReferenceCoordinatesAndRotationalSymmetry()
        {
            ArenaBoundsAuthoring bounds = Find<ArenaBoundsAuthoring>().Single();
            Assert.That(bounds.Size, Is.EqualTo(new Vector2(28f, 20f)));

            AssertPairIsRotationallySymmetric(Find<SpawnPointAuthoring>().Cast<ArenaElementAuthoring>(), "spawn.north", "spawn.south");
            AssertPairIsRotationallySymmetric(Find<RelayZoneAuthoring>().Cast<ArenaElementAuthoring>(), "relay.alpha", "relay.beta");
            AssertPairIsRotationallySymmetric(Find<TerminalAuthoring>().Cast<ArenaElementAuthoring>(), "terminal.north", "terminal.south");
            AssertPairIsRotationallySymmetric(Find<ShockGateAuthoring>().Cast<ArenaElementAuthoring>(), "gate.alpha", "gate.beta");

            AssertPosition("core.reset.central", Vector3.zero);
            AssertPosition("spawn.north", new Vector3(0f, 0f, 7.5f));
            AssertPosition("relay.alpha", new Vector3(-10.2f, 0f, 0f));
            AssertPosition("terminal.north", new Vector3(0f, 0f, 5.5f));
            AssertPosition("gate.alpha", new Vector3(-6.8f, 0f, 0f));

            Vector3[] pylonPositions = Find<ConvexPylonAuthoring>()
                .Select(component => component.transform.position)
                .OrderBy(position => position.x)
                .ThenBy(position => position.z)
                .ToArray();
            Assert.That(pylonPositions, Does.Contain(new Vector3(-3.4f, 0f, -2.8f)));
            Assert.That(pylonPositions, Does.Contain(new Vector3(-3.4f, 0f, 2.8f)));
            Assert.That(pylonPositions, Does.Contain(new Vector3(3.4f, 0f, -2.8f)));
            Assert.That(pylonPositions, Does.Contain(new Vector3(3.4f, 0f, 2.8f)));
        }

        [Test]
        public void StableIdsAreExplicitUniqueAndHierarchyIndependent()
        {
            ArenaElementAuthoring[] elements = Find<ArenaElementAuthoring>();
            string[] ids = elements.Select(element => element.StableId).ToArray();

            Assert.That(elements, Has.Length.EqualTo(52));
            Assert.That(ids, Has.All.Matches<string>(id => StableIdPattern.IsMatch(id)));
            Assert.That(ids.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(ids.Length));
            Assert.That(ids, Does.Not.Contain(string.Empty));
        }

        [Test]
        public void SceneContainsSingleArenaAuthoringRoot()
        {
            Assert.That(
                EditorSceneManager.GetActiveScene().GetRootGameObjects()
                    .Count(root => root.GetComponent<SwitchyardArenaAuthoring>() != null),
                Is.EqualTo(1));
        }

        private static T[] Find<T>()
            where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include);
        }

        private static void AssertPairIsRotationallySymmetric(
            IEnumerable<ArenaElementAuthoring> elements,
            string firstId,
            string secondId)
        {
            Dictionary<string, Vector3> positions = elements.ToDictionary(
                element => element.StableId,
                element => element.transform.position,
                StringComparer.Ordinal);
            Assert.That(positions[secondId], Is.EqualTo(-positions[firstId]));
        }

        private static void AssertPosition(string stableId, Vector3 expected)
        {
            ArenaElementAuthoring element = Find<ArenaElementAuthoring>().Single(item => item.StableId == stableId);
            Assert.That(element.transform.position, Is.EqualTo(expected));
        }

    }
}
