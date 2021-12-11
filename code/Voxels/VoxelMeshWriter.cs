using Sandbox;
using System;
using System.Collections.Generic;

namespace Voxels
{
	public class VoxelMeshWriter
	{
		private static readonly List<VoxelMeshWriter> _sPool = new List<VoxelMeshWriter>();

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

		private readonly Voxel[] _corners = new Voxel[8];

		public List<SimpleVertex> Vertices { get; } = new List<SimpleVertex>();

		public Vector3 Offset { get; set; } = 0f;
		public Vector3 Scale { get; set; } = 1f;

		public void Clear()
		{
			Vertices.Clear();

			Offset = 0f;
			Scale = 1f;
		}

		private readonly List<Intersection> _faceIntersections = new List<Intersection>( 4 );
		private readonly List<(Intersection a, Intersection b)> _dualEdges = new List<(Intersection a, Intersection b)>( 12 );

		private void ProcessEdge( CubeVertex v0, CubeVertex v1, Voxel val0, Voxel val1 )
		{
			if ( val0.RawValue >= 128 == val1.RawValue >= 128 ) return;

			var t = val0.Value / (val0.Value - val1.Value);

			_faceIntersections.Add( new Intersection( v0, v1, t ) );
		}

		private void ProcessFace( CubeFace face )
		{
			CubeVertex.GetFace( face, out var v00, out var v01, out var v10, out var v11 );

			var value00 = _corners[v00.Index];
			var value01 = _corners[v01.Index];
			var value10 = _corners[v10.Index];
			var value11 = _corners[v11.Index];

			_faceIntersections.Clear();

			ProcessEdge( v00, v01, value00, value01 );
			ProcessEdge( v01, v11, value01, value11 );
			ProcessEdge( v11, v10, value11, value10 );
			ProcessEdge( v10, v00, value10, value00 );

			if ( _faceIntersections.Count == 0 ) return;
			if ( _faceIntersections.Count == 4 )
			{
				var length0 = (_faceIntersections[1].Pos - _faceIntersections[0].Pos).Length +
							 (_faceIntersections[3].Pos - _faceIntersections[2].Pos).Length;

				var length1 = (_faceIntersections[2].Pos - _faceIntersections[1].Pos).Length +
							  (_faceIntersections[0].Pos - _faceIntersections[3].Pos).Length;

				if ( length1 < length0 )
				{
					_faceIntersections.Insert( 0, _faceIntersections[3] );
					_faceIntersections.RemoveAt( 4 );
				}
			}

			for ( var i = 0; i < _faceIntersections.Count; i += 2 )
			{
				var i0 = _faceIntersections[i];
				var i1 = _faceIntersections[(i + 1) % _faceIntersections.Count];

				if ( _corners[i0.Vert1.Index].RawValue >= 128 )
				{
					_dualEdges.Add( (i1, i0) );
				}
				else
				{
					_dualEdges.Add( (i0, i1) );
				}
			}
		}

		private int IndexOfNextDualEdge( in Intersection prev )
		{
			for ( var i = 0; i < _dualEdges.Count; ++i )
			{
				var dualEdge = _dualEdges[i];
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
			_corners[0] = x0y0z0;
			_corners[1] = x1y0z0;
			_corners[2] = x0y1z0;
			_corners[3] = x1y1z0;
			_corners[4] = x0y0z1;
			_corners[5] = x1y0z1;
			_corners[6] = x0y1z1;
			_corners[7] = x1y1z1;

			_dualEdges.Clear();

			ProcessFace( CubeFace.XMin );
			ProcessFace( CubeFace.XMax );
			ProcessFace( CubeFace.YMin );
			ProcessFace( CubeFace.YMax );
			ProcessFace( CubeFace.ZMin );
			ProcessFace( CubeFace.ZMax );

			var offset = Offset + index3 * Scale * size;
			var scale = Scale * size;

			while ( _dualEdges.Count > 0 )
			{
				var nextIndex = _dualEdges.Count - 1;
				var first = _dualEdges[nextIndex];
				(Intersection a, Intersection b) prev = default;

				var a = offset + first.a.Pos * scale;
				var b = a;

				var edgeLoopVertIndex = 0;

				do
				{
					var next = _dualEdges[nextIndex];
					_dualEdges.RemoveAt( nextIndex );

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

						Vertices.Add( new SimpleVertex( a, normal, tangent, new Vector2( a.x, a.y ) ) );
						Vertices.Add( new SimpleVertex( b, normal, tangent, new Vector2( a.x, a.y ) ) );
						Vertices.Add( new SimpleVertex( c, normal, tangent, new Vector2( a.x, a.y ) ) );
					}

					b = c;
					prev = next;

					++edgeLoopVertIndex;
				} while ( (nextIndex = IndexOfNextDualEdge( prev.b )) != -1 );
			}
		}
	}
}
