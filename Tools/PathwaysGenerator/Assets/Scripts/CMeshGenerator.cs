using System.Collections.Generic;
using UnityEngine;

public class CMeshGenerator
{
	public enum EPolylinePointType
	{
		Common,
		Arc,
		End,
	}

	public class CPolylinePoint
	{
		public Vector3            Position;
		public Vector3            Up;
		public float              Radius;
		public EPolylinePointType PointType = EPolylinePointType.Common;
	}

	public static void ExtrudePolylineRotated(
		List<CPolylinePoint> polyline,
		List<Vector3>       vertices,
		List<int>           indices
	)
	{
		if (polyline.Count < 2)
		{
			return;
		}

		int start_vertex = vertices.Count;
			
		// first profile
		Vector3 position  = polyline[0].Position;
		Vector3 direction = (polyline[1].Position - polyline[0].Position).normalized;
		Vector3 up        = polyline[0].Up;
		Vector3 right     = Vector3.Cross(up, direction).normalized;
		float   radius    = polyline[0].Radius;
		up = Vector3.Cross(direction, right).normalized; // up vector might not be perpendicular to direction
		
		vertices.Add(position - up    * radius);
		vertices.Add(position - right * radius);
		vertices.Add(position + up    * radius);
		vertices.Add(position + right * radius);

		// next profiles
		for (int polyline_index = 1; polyline_index < polyline.Count; ++polyline_index)
		{
			Vector3 prev_direction = direction;

			// next direction vector
			if (polyline_index < polyline.Count - 1)
			{
				direction = (polyline[polyline_index + 1].Position - polyline[polyline_index].Position).normalized;
			}

			// next vectors
			Vector3 avg_direction = ((prev_direction + direction) * 0.5f).normalized;

			position = polyline[polyline_index].Position;
			up       = polyline[polyline_index].Up;
			right    = Vector3.Cross(up,            avg_direction).normalized;
			up       = Vector3.Cross(avg_direction, right).normalized;
			radius   = polyline[polyline_index].Radius;

			// next profile
			vertices.Add(position - up    * radius);
			vertices.Add(position - right * radius);
			vertices.Add(position + up    * radius);
			vertices.Add(position + right * radius);
		}
			
		// build indices
		for (int i = 0; i < (vertices.Count - start_vertex) / 4 - 1; ++i)
		{
			int b = start_vertex + i * 4; // base index

			indices.Add(b + 0);
			indices.Add(b + 5);
			indices.Add(b + 1);

			indices.Add(b + 0);
			indices.Add(b + 4);
			indices.Add(b + 5);

				
			indices.Add(b + 1);
			indices.Add(b + 6);
			indices.Add(b + 2);

			indices.Add(b + 1);
			indices.Add(b + 5);
			indices.Add(b + 6);


			indices.Add(b + 2);
			indices.Add(b + 7);
			indices.Add(b + 3);

			indices.Add(b + 2);
			indices.Add(b + 6);
			indices.Add(b + 7);


			indices.Add(b + 3);
			indices.Add(b + 4);
			indices.Add(b + 0);

			indices.Add(b + 3);
			indices.Add(b + 7);
			indices.Add(b + 4);
		}
		
		// close caps
		indices.Add(start_vertex + 0);
		indices.Add(start_vertex + 1);
		indices.Add(start_vertex + 2);

		indices.Add(start_vertex + 0);
		indices.Add(start_vertex + 2);
		indices.Add(start_vertex + 3);

		{
			int b = vertices.Count - 4;

			indices.Add(b + 0);
			indices.Add(b + 2);
			indices.Add(b + 1);

			indices.Add(b + 0);
			indices.Add(b + 3);
			indices.Add(b + 2);
		}
	}
}