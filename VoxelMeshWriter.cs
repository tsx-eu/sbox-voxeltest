using Sandbox;
using System;
using System.Collections.Generic;

namespace Voxels
{
	public class VoxelMeshWriter
	{
		private static readonly List<VoxelMeshWriter> _sPool = new List<VoxelMeshWriter>();

		private ref struct SpanList<T>
			where T : struct
		{
			private Span<T> _span;

			public int Count { get; private set; }

			public ref T this[int index]
			{
				get
				{
					if ( index >= Count ) throw new IndexOutOfRangeException();

					return ref _span[index];
				}
			}

			public SpanList( ref Span<T> span )
			{
				_span = span;

				Count = 0;
			}

			public void Add( T value )
			{
				if ( Count >= _span.Length ) throw new Exception( "Capacity exceeded." );

				_span[Count++] = value;
			}

			public void RemoveAt( int index )
			{
				if ( Count <= 0 )
				{
					if ( Count >= _span.Length ) throw new Exception( "Attempting to remove from an empty list." );
				}

				--Count;

				for ( var i = index; i < Count; ++i )
				{
					_span[i] = _span[i + 1];
				}
			}

			public void Clear()
			{
				Count = 0;
			}
		}

		public static VoxelMeshWriter Rent()
		{
			if ( _sPool.Count > 0 )
			{
				var writer = _sPool[_sPool.Count - 1];
				_sPool.RemoveAt( _sPool.Count - 1 );

				writer._isInPool = false;
				writer.Clear();

				return writer;
			}

			return new VoxelMeshWriter();
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

		private void ProcessEdge( ref SpanList<Intersection> intersections, CubeVertex v0, CubeVertex v1, Voxel val0, Voxel val1 )
		{
			if ( val0.RawValue >= 128 == val1.RawValue >= 128 ) return;

			var t = val0.Value / (val0.Value - val1.Value);

			intersections.Add( new Intersection( v0, v1, t ) );
		}

		private void ProcessFace( ref SpanList<(Intersection a, Intersection b)> dualEdges, ref Span<Voxel> corners, CubeFace face )
		{
			CubeVertex.GetFace( face, out var v00, out var v01, out var v10, out var v11 );

			var value00 = corners[v00.Index];
			var value01 = corners[v01.Index];
			var value10 = corners[v10.Index];
			var value11 = corners[v11.Index];

			Span<Intersection> faceIntersectionSpan = stackalloc Intersection[4];
			var faceIntersections = new SpanList<Intersection>( ref faceIntersectionSpan );

			ProcessEdge( ref faceIntersections, v00, v01, value00, value01 );
			ProcessEdge( ref faceIntersections, v01, v11, value01, value11 );
			ProcessEdge( ref faceIntersections, v11, v10, value11, value10 );
			ProcessEdge( ref faceIntersections, v10, v00, value10, value00 );

			if ( faceIntersections.Count == 0 ) return;

			var readOffset = 0;

			if ( faceIntersections.Count == 4 )
			{
				var length0 = (faceIntersections[1].Pos - faceIntersections[0].Pos).Length +
							  (faceIntersections[3].Pos - faceIntersections[2].Pos).Length;

				var length1 = (faceIntersections[2].Pos - faceIntersections[1].Pos).Length +
							  (faceIntersections[0].Pos - faceIntersections[3].Pos).Length;

				if ( length1 < length0 )
				{
					readOffset = 1;
				}
			}

			for ( var i = 0; i < faceIntersections.Count; i += 2 )
			{
				var i0 = faceIntersections[(readOffset + i) % faceIntersections.Count];
				var i1 = faceIntersections[(readOffset + i + 1) % faceIntersections.Count];

				if ( corners[i0.Vert1.Index].RawValue >= 128 )
				{
					dualEdges.Add( (i1, i0) );
				}
				else
				{
					dualEdges.Add( (i0, i1) );
				}
			}
		}

		private int IndexOfNextDualEdge( ref SpanList<(Intersection a, Intersection b)> dualEdges, in Intersection prev )
		{
			for ( var i = 0; i < dualEdges.Count; ++i )
			{
				var dualEdge = dualEdges[i];
				if ( dualEdge.a.Equals( prev ) || dualEdge.b.Equals( prev ) )
				{
					return i;
				}
			}

			return -1;
		}

		public void Write( Vector3i index3, Vector3 size,
			Voxel x0y0z0, Voxel x1y0z0,
			Voxel x0y1z0, Voxel x1y1z0,
			Voxel x0y0z1, Voxel x1y0z1,
			Voxel x0y1z1, Voxel x1y1z1 )
		{
			Span<Voxel> corners = stackalloc Voxel[8];

			corners[0] = x0y0z0;
			corners[1] = x1y0z0;
			corners[2] = x0y1z0;
			corners[3] = x1y1z0;
			corners[4] = x0y0z1;
			corners[5] = x1y0z1;
			corners[6] = x0y1z1;
			corners[7] = x1y1z1;

			Span<(Intersection, Intersection)> dualEdgesSpan = stackalloc (Intersection, Intersection)[12];
			var dualEdges = new SpanList<(Intersection a, Intersection b)>( ref dualEdgesSpan );

			ProcessFace( ref dualEdges, ref corners, CubeFace.XMin );
			ProcessFace( ref dualEdges, ref corners, CubeFace.XMax );
			ProcessFace( ref dualEdges, ref corners, CubeFace.YMin );
			ProcessFace( ref dualEdges, ref corners, CubeFace.YMax );
			ProcessFace( ref dualEdges, ref corners, CubeFace.ZMin );
			ProcessFace( ref dualEdges, ref corners, CubeFace.ZMax );

			if ( dualEdges.Count == 0 )
			{
				return;
			}

			var offset = Offset + index3 * Scale * size;
			var scale = Scale * size;

			while ( dualEdges.Count > 0 )
			{
				var nextIndex = dualEdges.Count - 1;
				var first = dualEdges[nextIndex];
				(Intersection a, Intersection b) prev = default;

				var a = offset + first.a.Pos * scale;
				var b = a;

				var edgeLoopVertIndex = 0;

				do
				{
					var next = dualEdges[nextIndex];
					dualEdges.RemoveAt( nextIndex );

					if ( next.b.Equals( prev.b ) )
					{
						next = (next.b, next.a);
					}

					var c = offset + next.b.Pos * scale;

					if ( edgeLoopVertIndex > 0 )
					{
						var cross = Vector3.Cross( b - a, c - a );
						var normal = cross.Normal;
						var tangent = (b - a).Normal;

						Vertices.Add( new VoxelVertex( a, normal, tangent ) );
						Vertices.Add( new VoxelVertex( b, normal, tangent ) );
						Vertices.Add( new VoxelVertex( c, normal, tangent ) );
					}

					b = c;
					prev = next;

					++edgeLoopVertIndex;
				} while ( (nextIndex = IndexOfNextDualEdge( ref dualEdges, prev.b )) != -1 );
			}
		}
	}
}
