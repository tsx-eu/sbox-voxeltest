using Sandbox;
using System;
using System.Runtime.InteropServices;

namespace Voxels
{
	public readonly struct Voxel
	{
		private static readonly float[] _sValueLookup = new float[256];

		public static Voxel operator +( Voxel a, Voxel b )
		{
			return new Voxel( (byte)Math.Min( a.RawValue + b.RawValue, 255 ), b.MaterialIndex );
		}

		public static Voxel operator -( Voxel a, Voxel b )
		{
			return new Voxel( (byte)Math.Max( a.RawValue - b.RawValue, 0 ), b.MaterialIndex );
		}

		static Voxel()
		{
			for ( var i = 1; i < 255; ++i )
			{
				_sValueLookup[i] = (i - 127.5f) / 127.5f;
			}

			_sValueLookup[0] = -1f;
			_sValueLookup[255] = 1f;
		}

		public readonly byte RawValue;
		public readonly byte MaterialIndex;

		public float Value => _sValueLookup[RawValue];

		public Voxel( float value, byte materialIndex )
		{
			RawValue = (byte)Math.Clamp( (int)MathF.Round( value * 127.5f + 127.5f ), 0, 255 );
			MaterialIndex = materialIndex;
		}

		public Voxel( byte rawValue, byte materialIndex )
		{
			RawValue = rawValue;
			MaterialIndex = materialIndex;
		}
	}

	[StructLayout( LayoutKind.Sequential )]
	public readonly struct VoxelVertex
	{
		public static VertexAttribute[] Layout { get; } =
		{
			new VertexAttribute(VertexAttributeType.Position, VertexAttributeFormat.Float32),
			new VertexAttribute(VertexAttributeType.Normal, VertexAttributeFormat.Float32),
			new VertexAttribute(VertexAttributeType.Tangent, VertexAttributeFormat.Float32)
		};

		public readonly Vector3 Position;
		public readonly Vector3 Normal;
		public readonly Vector3 Tangent;

		public VoxelVertex(Vector3 position, Vector3 normal, Vector3 tangent)
		{
			Position = position;
			Normal = normal;
			Tangent = tangent;
		}
	}

	[StructLayout( LayoutKind.Sequential )]
	public readonly struct CompressedVoxelVertex
	{
		private const int MaxPositionValue = 1 << 9;
		private const int MaxNormalValue = 127;

		public static VertexAttribute[] Layout { get; } =
		{
			new VertexAttribute( VertexAttributeType.TexCoord, VertexAttributeFormat.UInt32, 1, 10 ),
			new VertexAttribute( VertexAttributeType.TexCoord, VertexAttributeFormat.UInt32, 1, 11 ),
			// new VertexAttribute( VertexAttributeType.TexCoord, VertexAttributeFormat.UInt8, 1, 12 )
		};

		public readonly uint PositionData;
		public readonly uint NormalData;

		public CompressedVoxelVertex( Vector3 position, Vector3 normal, byte matIndex )
		{
			var intPos = Vector3i.Clamp( Vector3i.Round( position * MaxPositionValue ), 0, MaxPositionValue );

			PositionData = (uint)((intPos.x & 0x3ff) | ((intPos.y & 0x3ff) << 10) | ((intPos.z & 0x3ff) << 20));

			var intNormal = Vector3i.Clamp( Vector3i.Round( normal * MaxNormalValue ) + 128, 1, 255 );

			NormalData = (uint)((intNormal.x & 0xff) | ((intNormal.y & 0xff) << 8) | ((intNormal.z & 0xff) << 16));

			// MaterialIndex = matIndex;
		}
	}
}
