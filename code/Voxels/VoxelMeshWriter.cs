using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Voxels
{
	public interface IVoxelMeshWriter
	{
		void Write( Voxel[] data, Vector3i size, Vector3i min, Vector3i max );

		public void Write( Voxel[] data, Vector3i size )
		{
			Write( data, size, 0, size );
		}
	}

	public partial class MarchingCubesMeshWriter : IVoxelMeshWriter
	{
		private static readonly List<MarchingCubesMeshWriter> _sPool = new List<MarchingCubesMeshWriter>();

		public static MarchingCubesMeshWriter Rent()
		{
			if ( _sPool.Count > 0 )
			{
				var writer = _sPool[_sPool.Count - 1];
				_sPool.RemoveAt( _sPool.Count - 1 );

				writer._isInPool = false;
				writer.Clear();

				return writer;
			}

			return new MarchingCubesMeshWriter();
		}

		public void Return()
		{
			if ( _isInPool )
			{
				throw new InvalidOperationException( "Already returned." );
			}

			Clear();

			_isInPool = true;
			_sPool.Add( this );
		}

		private bool _isInPool;

		public List<VoxelVertex> Vertices { get; } = new List<VoxelVertex>();

		public Vector3 Offset { get; set; } = 0f;
		public Vector3 Scale { get; set; } = 1f;

		public void Clear()
		{
			Vertices.Clear();

			Offset = 0f;
			Scale = 1f;
		}

		public void Write( Voxel[] data, Vector3i size, Vector3i min, Vector3i max )
		{
			const int xStride = 1;
			var yStride = xStride * size.x;
			var zStride = yStride * size.y;

			max -= 1;

			var scale = new Vector3( 1f / (max.x - min.x), 1f / (max.y - min.y), 1f / (max.z - min.z) );

			for ( var z = min.z; z < max.z; ++z )
			{
				for ( var y = min.y; y < max.y; ++y )
				{
					var i0 = (y + 0) * yStride + (z + 0) * zStride;
					var i1 = (y + 1) * yStride + (z + 0) * zStride;
					var i2 = (y + 0) * yStride + (z + 1) * zStride;
					var i3 = (y + 1) * yStride + (z + 1) * zStride;

					var x0y0z0 = data[i0];
					var x0y1z0 = data[i1];
					var x0y0z1 = data[i2];
					var x0y1z1 = data[i3];

					var x0hash
						= ((x0y0z0.RawValue & 0x80) >> 7)
						| ((x0y1z0.RawValue & 0x80) >> 6)
						| ((x0y0z1.RawValue & 0x80) >> 5)
						| ((x0y1z1.RawValue & 0x80) >> 4);

					for ( var x = min.x; x < max.x; ++x )
					{
						var x1y0z0 = data[++i0];
						var x1y1z0 = data[++i1];
						var x1y0z1 = data[++i2];
						var x1y1z1 = data[++i3];

						var x1hash
							= ((x1y0z0.RawValue & 0x80) >> 7)
							| ((x1y1z0.RawValue & 0x80) >> 6)
							| ((x1y0z1.RawValue & 0x80) >> 5)
							| ((x1y1z1.RawValue & 0x80) >> 4);

						var hash = (x1hash << 4) | x0hash;

						if ( hash != 0b0000_0000 && hash != 0b1111_1111 )
						{
							Write( new Vector3i( x, y, z ), scale,
								x0y0z0, x1y0z0, x0y1z0, x1y1z0,
								x0y0z1, x1y0z1, x0y1z1, x1y1z1);
						}

						x0y0z0 = x1y0z0;
						x0y1z0 = x1y1z0;
						x0y0z1 = x1y0z1;
						x0y1z1 = x1y1z1;

						x0hash = x1hash;
					}
				}
			}
		}

		private EdgeIntersection GetIntersection( int index, Vector3 pos0, Vector3 pos1, Voxel val0, Voxel val1 )
		{
			if ( (val0.RawValue & 0x80) == (val1.RawValue & 0x80) )
			{
				return default;
			}

			var t = (val0.RawValue - 127.5f) / (val0.RawValue - val1.RawValue);

			return new EdgeIntersection( index, val0, val1, pos0 + (pos1 - pos0) * t );
		}

		private int ProcessFace( in Span<DualEdge> dualEdgeMap,
			EdgeIntersection edge0, EdgeIntersection edge1,
			EdgeIntersection edge2, EdgeIntersection edge3 )
		{
			var hash
				= (edge0.Exists ? 0b0001 : 0)
				| (edge1.Exists ? 0b0010 : 0)
				| (edge2.Exists ? 0b0100 : 0)
				| (edge3.Exists ? 0b1000 : 0);

			switch ( hash )
			{
				case 0b0000:
					return 0;

				case 0b0011:
				{
					var dualEdge = new DualEdge( edge0, edge1 );
					dualEdgeMap[dualEdge.Index0] = dualEdge;
					return 1;
				}

				case 0b0101:
				{
					var dualEdge = new DualEdge( edge0, edge2 );
					dualEdgeMap[dualEdge.Index0] = dualEdge;
					return 1;
				}

				case 0b1001:
				{
					var dualEdge = new DualEdge( edge0, edge3 );
					dualEdgeMap[dualEdge.Index0] = dualEdge;
					return 1;
				}

				case 0b0110:
				{
					var dualEdge = new DualEdge( edge1, edge2 );
					dualEdgeMap[dualEdge.Index0] = dualEdge;
					return 1;
				}

				case 0b1010:
				{
					var dualEdge = new DualEdge( edge1, edge3 );
					dualEdgeMap[dualEdge.Index0] = dualEdge;
					return 1;
				}

				case 0b1100:
				{
					var dualEdge = new DualEdge( edge2, edge3 );
					dualEdgeMap[dualEdge.Index0] = dualEdge;
					return 1;
				}

				case 0b1111:
				{
					// Special case: two possible pairs of edges,
					// so find the shortest total length

					var len0 = (edge0.Pos - edge1.Pos).Length + (edge2.Pos - edge3.Pos).Length;
					var len1 = (edge0.Pos - edge3.Pos).Length + (edge1.Pos - edge2.Pos).Length;

					DualEdge dualEdge0;
					DualEdge dualEdge1;

					if ( len0 <= len1 )
					{
						dualEdge0 = new DualEdge( edge0, edge1 );
						dualEdge1 = new DualEdge( edge2, edge3 );
					}
					else
					{
						dualEdge0 = new DualEdge( edge0, edge3 );
						dualEdge1 = new DualEdge( edge1, edge2 );
					}

					dualEdgeMap[dualEdge0.Index0] = dualEdge0;
					dualEdgeMap[dualEdge1.Index0] = dualEdge1;
					return 2;
				}

				default:
					throw new Exception( "Invalid mesh generated" );
			}
		}

		private static readonly Vector3 X0Y0Z0 = new Vector3( 0f, 0f, 0f );
		private static readonly Vector3 X1Y0Z0 = new Vector3( 1f, 0f, 0f );
		private static readonly Vector3 X0Y1Z0 = new Vector3( 0f, 1f, 0f );
		private static readonly Vector3 X1Y1Z0 = new Vector3( 1f, 1f, 0f );
		private static readonly Vector3 X0Y0Z1 = new Vector3( 0f, 0f, 1f );
		private static readonly Vector3 X1Y0Z1 = new Vector3( 1f, 0f, 1f );
		private static readonly Vector3 X0Y1Z1 = new Vector3( 0f, 1f, 1f );
		private static readonly Vector3 X1Y1Z1 = new Vector3( 1f, 1f, 1f );

		public void Write( Vector3i index3, Vector3 scale,
			Voxel x0y0z0, Voxel x1y0z0,
			Voxel x0y1z0, Voxel x1y1z0,
			Voxel x0y0z1, Voxel x1y0z1,
			Voxel x0y1z1, Voxel x1y1z1 )
		{
			// Find out if / where the surface intersects each edge of the cube.

			var edgeXMinYMin = GetIntersection(  0, X0Y0Z0, X0Y0Z1, x0y0z0, x0y0z1 );
			var edgeXMinZMin = GetIntersection(  1, X0Y0Z0, X0Y1Z0, x0y0z0, x0y1z0 );
			var edgeXMaxYMin = GetIntersection(  2, X1Y0Z0, X1Y0Z1, x1y0z0, x1y0z1 );
			var edgeXMaxZMin = GetIntersection(  3, X1Y0Z0, X1Y1Z0, x1y0z0, x1y1z0 );
			var edgeXMinYMax = GetIntersection(  4, X0Y1Z0, X0Y1Z1, x0y1z0, x0y1z1 );
			var edgeXMinZMax = GetIntersection(  5, X0Y0Z1, X0Y1Z1, x0y0z1, x0y1z1 );
			var edgeXMaxYMax = GetIntersection(  6, X1Y1Z0, X1Y1Z1, x1y1z0, x1y1z1 );
			var edgeXMaxZMax = GetIntersection(  7, X1Y0Z1, X1Y1Z1, x1y0z1, x1y1z1 );
			var edgeYMinZMin = GetIntersection(  8, X0Y0Z0, X1Y0Z0, x0y0z0, x1y0z0 );
			var edgeYMaxZMin = GetIntersection(  9, X0Y1Z0, X1Y1Z0, x0y1z0, x1y1z0 );
			var edgeYMinZMax = GetIntersection( 10, X0Y0Z1, X1Y0Z1, x0y0z1, x1y0z1 );
			var edgeYMaxZMax = GetIntersection( 11, X0Y1Z1, X1Y1Z1, x0y1z1, x1y1z1 );

			// Each face of the cube will have either 0, 2 or 4 edges with intersections.
			// We will turn each pair of edge intersections into an edge in the final mesh.
			// Each of these "dual edges" will be stored in a table, indexed by the first
			// intersection edge.

			Span<DualEdge> dualEdgeMap = stackalloc DualEdge[12];

			dualEdgeMap.Clear();

			var dualEdgeCount = 0;

			dualEdgeCount += ProcessFace( dualEdgeMap, +edgeXMinZMin, +edgeXMinYMax, -edgeXMinZMax, -edgeXMinYMin );
			dualEdgeCount += ProcessFace( dualEdgeMap, +edgeXMaxZMax, -edgeXMaxYMax, -edgeXMaxZMin, +edgeXMaxYMin );
			dualEdgeCount += ProcessFace( dualEdgeMap, +edgeXMinYMin, +edgeYMinZMax, -edgeXMaxYMin, -edgeYMinZMin );
			dualEdgeCount += ProcessFace( dualEdgeMap, +edgeXMaxYMax, -edgeYMaxZMax, -edgeXMinYMax, +edgeYMaxZMin );
			dualEdgeCount += ProcessFace( dualEdgeMap, +edgeXMaxZMin, -edgeYMaxZMin, -edgeXMinZMin, +edgeYMinZMin );
			dualEdgeCount += ProcessFace( dualEdgeMap, +edgeXMinZMax, +edgeYMaxZMax, -edgeXMaxZMax, -edgeYMinZMax );

			if ( dualEdgeCount == 0 )
			{
				return;
			}

			if ( dualEdgeCount < 3 )
			{
				throw new Exception( "Invalid mesh generated" );
			}

			scale *= Scale;

			var offset = Offset + index3 * scale;

			// Follow edges in a loop to triangulate.

			for (var i = 0; i < 12 && dualEdgeCount > 0; ++i)
            {
				var first = dualEdgeMap[i];
				if (!first.Exists) continue;

				// Start of an edge loop.

				var prev = first;
				var next = first;

				// Remove first edge in edge loop.
				dualEdgeMap[i] = default;
				--dualEdgeCount;

				var a = offset + first.Pos0 * scale;
				var b = offset + first.Pos1 * scale;

				// We skip the first edge, and break on the last edge, so
				// we output a triangle every N-2 edges in the loop.

				while ((next = dualEdgeMap[prev.Index1]).Exists)
				{
					// Remove edge from map.
					dualEdgeMap[prev.Index1] = default;
					--dualEdgeCount;

					if ( next.Index1 == i ) break;

					var c = offset + next.Pos1 * scale;

					var cross = Vector3.Cross( b - a, c - a );
					var normal = cross.Normal;
					var tangent = (b - a).Normal;

					Vertices.Add( new VoxelVertex( a, normal, tangent ) );
					Vertices.Add( new VoxelVertex( b, normal, tangent ) );
					Vertices.Add( new VoxelVertex( c, normal, tangent ) );

					b = c;
					prev = next;
				}
			}

			if ( dualEdgeCount != 0 )
			{
				throw new Exception( "Invalid mesh generated" );
			}
		}
	}
}
