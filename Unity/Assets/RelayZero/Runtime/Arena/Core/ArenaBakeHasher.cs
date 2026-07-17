using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.Mathematics;

namespace RelayZero.Arena
{
    public static class ArenaBakeHasher
    {
        private static readonly byte[] Header = Encoding.ASCII.GetBytes("RZ-ARENA-BAKE");

        public static string Compute(ArenaBakeData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Header.Length);
                writer.Write(Header);
                writer.Write(data.BakeVersion);
                WriteString(writer, data.SourceScene);

                WriteElements(writer, data.Elements.OrderBy(value => value.Id.Value));
                WriteBounds(writer, data.Bounds);
                WriteWalls(writer, data.Walls.OrderBy(value => value.Id.Value));
                WriteObstacles(writer, data.Obstacles.OrderBy(value => value.Id.Value));
                WriteCircles(writer, data.Circles.OrderBy(value => value.Id.Value));
                WriteGates(writer, data.Gates.OrderBy(value => value.Id.Value));
                WriteBoosts(writer, data.Boosts.OrderBy(value => value.Id.Value));
                WriteSpawns(writer, data.Spawns.OrderBy(value => value.Id.Value));
                WriteCoreReset(writer, data.CoreReset);
                WriteForbidden(writer, data.BarrierForbiddenVolumes.OrderBy(value => value.Id.Value));
                WriteNodes(writer, data.NavigationNodes.OrderBy(value => value.Id.Value));
                WriteEdges(
                    writer,
                    data.NavigationEdges.OrderBy(value => value.NodeA.Value).ThenBy(value => value.NodeB.Value));
                WriteCamera(writer, data.CameraBounds);
                writer.Flush();

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(stream.ToArray());
                    StringBuilder result = new StringBuilder(hash.Length * 2);
                    for (int i = 0; i < hash.Length; i++)
                    {
                        result.Append(hash[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                    }

                    return result.ToString();
                }
            }
        }

        private static void WriteElements(BinaryWriter writer, IEnumerable<ArenaElementDescriptor> values)
        {
            ArenaElementDescriptor[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (ArenaElementDescriptor value in array)
            {
                WriteId(writer, value.Id);
                writer.Write((byte)value.Kind);
                WriteString(writer, value.StableId);
            }
        }

        private static void WriteBounds(BinaryWriter writer, BakedArenaBounds value)
        {
            WriteId(writer, value.Id);
            WriteFloat2(writer, value.Center);
            WriteFloat2(writer, value.HalfExtents);
        }

        private static void WriteWalls(BinaryWriter writer, IEnumerable<BakedWall> values)
        {
            BakedWall[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (BakedWall value in array)
            {
                WriteId(writer, value.Id);
                WriteFloat2(writer, value.Start);
                WriteFloat2(writer, value.End);
                WriteFloat2(writer, value.Normal);
                WriteFloat(writer, value.Thickness);
                WriteAabb(writer, value.Bounds);
                WriteAabb(writer, value.PlayerExpandedBounds);
            }
        }

        private static void WriteObstacles(BinaryWriter writer, IEnumerable<BakedConvexObstacle> values)
        {
            BakedConvexObstacle[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (BakedConvexObstacle value in array)
            {
                WriteId(writer, value.Id);
                WriteFloat2List(writer, value.Vertices);
                WriteFloat2List(writer, value.Normals);
                WriteAabb(writer, value.Bounds);
                WriteAabb(writer, value.PlayerExpandedBounds);
            }
        }

        private static void WriteCircles(BinaryWriter writer, IEnumerable<BakedCircle> values)
        {
            BakedCircle[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (BakedCircle value in array)
            {
                WriteId(writer, value.Id);
                writer.Write((byte)value.Kind);
                WriteFloat2(writer, value.Center);
                WriteFloat(writer, value.Radius);
                WriteFloat(writer, value.RadiusSquared);
            }
        }

        private static void WriteGates(BinaryWriter writer, IEnumerable<BakedShockGate> values)
        {
            BakedShockGate[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (BakedShockGate value in array)
            {
                WriteId(writer, value.Id);
                writer.Write((byte)value.SafeWhenPoweredSide);
                WriteFloat2(writer, value.Center);
                WriteFloat2(writer, value.HalfExtents);
                WriteFloat2(writer, value.SafeSideDirection);
                WriteAabb(writer, value.Bounds);
            }
        }

        private static void WriteBoosts(BinaryWriter writer, IEnumerable<BakedBoostPad> values)
        {
            BakedBoostPad[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (BakedBoostPad value in array)
            {
                WriteId(writer, value.Id);
                writer.Write((byte)value.Side);
                WriteFloat2List(writer, value.Vertices);
                WriteFloat2List(writer, value.Normals);
                WriteAabb(writer, value.Bounds);
            }
        }

        private static void WriteSpawns(BinaryWriter writer, IEnumerable<BakedSpawn> values)
        {
            BakedSpawn[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (BakedSpawn value in array)
            {
                WriteId(writer, value.Id);
                writer.Write(value.PlayerSlot);
                WriteFloat2(writer, value.Position);
                WriteFloat2(writer, value.FacingDirection);
            }
        }

        private static void WriteCoreReset(BinaryWriter writer, BakedCoreReset value)
        {
            WriteId(writer, value.Id);
            WriteFloat2(writer, value.Position);
            WriteFloat(writer, value.PedestalRadius);
            WriteFloat(writer, value.PedestalRadiusSquared);
        }

        private static void WriteForbidden(BinaryWriter writer, IEnumerable<BakedBarrierForbiddenVolume> values)
        {
            BakedBarrierForbiddenVolume[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (BakedBarrierForbiddenVolume value in array)
            {
                WriteId(writer, value.Id);
                writer.Write((byte)value.Shape);
                WriteFloat2(writer, value.Center);
                WriteFloat(writer, value.Radius);
                WriteFloat(writer, value.RadiusSquared);
                WriteFloat2(writer, value.HalfExtents);
                WriteString(writer, value.Reason);
            }
        }

        private static void WriteNodes(BinaryWriter writer, IEnumerable<BakedNavigationNode> values)
        {
            BakedNavigationNode[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (BakedNavigationNode value in array)
            {
                WriteId(writer, value.Id);
                writer.Write((byte)value.Kind);
                WriteFloat2(writer, value.Position);
                WriteFloat(writer, value.ConnectionRadius);
                ArenaElementId[] neighbors = value.NeighborIds.OrderBy(id => id.Value).ToArray();
                writer.Write(neighbors.Length);
                foreach (ArenaElementId neighbor in neighbors)
                {
                    WriteId(writer, neighbor);
                }
            }
        }

        private static void WriteEdges(BinaryWriter writer, IEnumerable<BakedNavigationEdge> values)
        {
            BakedNavigationEdge[] array = values.ToArray();
            writer.Write(array.Length);
            foreach (BakedNavigationEdge value in array)
            {
                WriteId(writer, value.NodeA);
                WriteId(writer, value.NodeB);
                WriteFloat(writer, value.LengthSquared);
                WriteId(writer, value.CrossedGate);
            }
        }

        private static void WriteCamera(BinaryWriter writer, BakedCameraBounds value)
        {
            WriteId(writer, value.Id);
            WriteFloat2(writer, value.Center);
            WriteFloat2(writer, value.HalfExtents);
            WriteFloat(writer, value.OrthographicSize);
        }

        private static void WriteId(BinaryWriter writer, ArenaElementId value)
        {
            writer.Write(value.Value);
        }

        private static void WriteAabb(BinaryWriter writer, ArenaAabb value)
        {
            WriteFloat2(writer, value.Minimum);
            WriteFloat2(writer, value.Maximum);
        }

        private static void WriteFloat2List(BinaryWriter writer, IReadOnlyList<float2> values)
        {
            writer.Write(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                WriteFloat2(writer, values[i]);
            }
        }

        private static void WriteFloat2(BinaryWriter writer, float2 value)
        {
            WriteFloat(writer, value.x);
            WriteFloat(writer, value.y);
        }

        private static void WriteFloat(BinaryWriter writer, float value)
        {
            writer.Write(ArenaGeometry.Canonicalize(value));
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
    }
}
