using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
public class RotorPathwaysMeshBehaviour : MonoBehaviour
{
	[Serializable]
	public class CConnection
	{
		public int From;
		public int To;
		public int Layer;

		public CConnection(int from, int to)
		{
			From  = from;
			To    = to;
			Layer = -1;
		}
	}

	private class CLayer
	{
		public List<bool> Slots;

		public CLayer(int pins_count)
		{
			Slots = new List<bool>(pins_count);
			for (int i = 0; i < pins_count; ++i)
			{
				Slots.Add(false);
			}
		}

		public bool IsEmpty(int from, int to, int pins_count)
		{
			SortFromTo(ref from, ref to, pins_count);

			bool is_empty = true;
			for (int i = from; i != to; i=(i +1) %pins_count)
			{
				if (Slots[i])
				{
					is_empty = false;
				}
			}

			return !Slots[from] && is_empty;
		}

		public void Write(int from, int to, int pins_count)
		{
			SortFromTo(ref from, ref to, pins_count);

			for (int i = from; i != to; i = (i + 1) % pins_count)
			{
				Slots[i] = true;
			}
		}

		private void SortFromTo(ref int from, ref int to, int pins_count)
		{
			if (from > to)
			{
				(from, to) = (to, from);
			}

			int distance          = to                - from;
			int opposite_distance = from + pins_count - to;

			if (opposite_distance < distance)
			{
				(from, to) = (to, from);
			}
		}


	}

	[Header("Cipher permutation")]
	[SerializeField] private int m_PinsCount = 26;
	[SerializeField] private List<CConnection> m_Connections;

	[Header("Mesh attributes")]
	[SerializeField] private RotorDefinition m_Definition;

	[Header("Output")]
	[SerializeField] private string m_ExportPath;

	[Header("Debug")]
	[SerializeField] private bool m_DrawGizmos; 
	private List<Vector3> m_GizmoLines = new ();

	void OnDrawGizmos()
	{
		if (!m_DrawGizmos)
		{
			return;
		}

		for (int i = 0; i < m_GizmoLines.Count -1; ++i)
		{
			float r = (i % 7) / 6f; //UnityEngine.Random.Range(0f, 1f);
			float g = (i % 3) / 2f; //UnityEngine.Random.Range(0f, 1f);
			float b = Mathf.Max(0, 1f - r - g);
			Gizmos.color = new Color(r, g, b);
			Gizmos.DrawLine(transform.TransformPoint(m_GizmoLines[i]), transform.TransformPoint(m_GizmoLines[i +1]));
		}
	}

	[ContextMenu("Create connections")]
	private void CreateConnections()
	{
		m_Connections = new List<CConnection>(m_PinsCount);
		List<int> pool = new List<int>(m_PinsCount);
		for (int i = 0; i < m_PinsCount; ++i)
		{
			pool.Add(i);
			m_Connections.Add(new CConnection(i, -1));
		}

		// bi-directional connections
		while (pool.Count > 0)
		{
			int from_index = Random.Range(0, pool.Count);
			int from       = pool[from_index];
			pool.RemoveAt(from_index);

			int to_index = Random.Range(0, pool.Count);
			int to       = pool[to_index];
			pool.RemoveAt(to_index);

			m_Connections[from].To = to;
			m_Connections[to].To   = from;
		}

		// fix - connections with distance 1 created without collision, so remove them
		for (int i = 0; i < m_Connections.Count; ++i)
		{
			if (
				m_Connections[i].From == (m_Connections[i].To + 1) % m_PinsCount
				||
				m_Connections[i].To == (m_Connections[i].From + 1) % m_PinsCount
			)
			{
				// find another connection that can be modified
				for (int j = 0; j < m_Connections.Count; ++j)
				{
					if (i == j)
					{
						continue;
					}

					if (
						m_Connections[j].To != (m_Connections[i].To + 1) % m_PinsCount
						||
						m_Connections[i].To != (m_Connections[j].To + 1) % m_PinsCount
					)
					{
						// another pair found -> swap To
						int tmp_i_to = m_Connections[i].To;
						int tmp_j_to = m_Connections[j].To;

						m_Connections[i].To        = tmp_j_to;
						m_Connections[j].To        = tmp_i_to;
						m_Connections[tmp_i_to].To = j;
						m_Connections[tmp_j_to].To = i;
						break;
					}
				}
			}
		}
	}

	[ContextMenu("Distribute to layers")]
	private void DistributeToLayers()
	{
		List<CLayer> layers = new List<CLayer>(m_Definition.LayersCount);
		for (int i = 0; i < m_Definition.LayersCount; ++i)
		{
			layers.Add(new CLayer(m_PinsCount));
		}

		for (int i = 0; i < m_Connections.Count; ++i)
		{
			if (m_Connections[i].To < i)
			{
				continue;
			}

			m_Connections[i].Layer = -1;

			for (int layer = 0; layer < layers.Count; ++layer)
			{
				if (layers[layer].IsEmpty(m_Connections[i].From, m_Connections[i].To, m_PinsCount))
				{
					layers[layer].Write(m_Connections[i].From, m_Connections[i].To, m_PinsCount);
					m_Connections[i].Layer                   = layer;
					m_Connections[m_Connections[i].To].Layer = layer;
					break;
				}
			}

			if (m_Connections[i].Layer == -1)
			{
				Debug.LogError($"Cannot find empty layer for connection: {m_Connections[i].From} -> {m_Connections[i].To}");
			}
		}
	}

	[ContextMenu("Rebuild mesh")]
	private void RebuildMesh()
	{
		m_GizmoLines.Clear();
		
		// mesh
		List<Vector3> vertices = new List<Vector3>();
		List<int>     indices  = new List<int>();

		for (int i = 0; i < m_Connections.Count; ++i)
		{
			bool is_second_offset = m_Connections[i].To > m_Connections[i].From;
			if (Mathf.Abs(m_Connections[i].To - m_Connections[i].From) > m_Connections.Count / 2)
			{
				// important for collision-free meshes
				is_second_offset = !is_second_offset; // reverse, connection crosses 0
			}

			AddExtrudedPath(
				m_Connections[i].From,
				m_Connections[i].To,
				m_Definition.GetLayerTopY(m_Connections[i].Layer),
				m_Definition.GetLayerBottomY(m_Connections[i].Layer),
				is_second_offset ? m_Definition.InnerOffset2 : m_Definition.InnerOffset1,
				vertices,
				indices
			);
		}

		// setup unity object
		Mesh mesh = new Mesh();
		mesh.indexFormat = IndexFormat.UInt32;
		mesh.SetVertices(vertices);
		mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0, true);

		MeshFilter mesh_filter = GetComponent<MeshFilter>();
		mesh_filter.mesh = mesh;
	}

	[ContextMenu("Export mesh to OBJ")]
	private void ExportMesh()
	{
		MeshFilter mesh_filter = GetComponent<MeshFilter>();

		List<Vector3> vertices = new();
		List<int>     indices  = new();
		
		mesh_filter.sharedMesh.GetVertices(vertices);
		mesh_filter.sharedMesh.GetIndices(indices, 0);

		List<string> lines = new();

		lines.Add("o holes");

		foreach (Vector3 vertex in vertices)
		{
			lines.Add($"v {vertex.x} {vertex.y} {vertex.z}");
		}

		for (int i = 0; i < indices.Count; i += 3)
		{
			lines.Add($"f {(indices[i] + 1)} {(indices[i +1] + 1)} {(indices[i +2] + 1)}");
		}

		string path = Path.Join(Application.dataPath, "\\..", m_ExportPath);
		File.WriteAllLines(path, lines);
		Debug.Log($"{lines.Count} lines written to {path}");
	}

	private void AddExtrudedPath(int from_pin, int to_pin, float layer_from_y, float layer_to_y, float inner_offset, List<Vector3> vertices, List<int> indices)
	{
		const float ANGLE_STEP = 0.05f;
		
		// calculate vectors
		float from_angle = 2f * Mathf.PI / m_PinsCount * from_pin;
		float to_angle   = 2f * Mathf.PI / m_PinsCount * to_pin;

		if (Mathf.Abs(from_angle - to_angle) > Mathf.PI)
		{
			// shorter path around
			if (from_angle > to_angle)
			{
				to_angle += 2f * Mathf.PI;
			}
			else
			{
				from_angle += 2f * Mathf.PI;
			}
		}

		Vector3 from_position = new Vector3(
			m_Definition.Radius * Mathf.Cos(from_angle),
			m_Definition.Height,
			m_Definition.Radius * Mathf.Sin(from_angle)
		);

		Vector3 from_position_offset = new Vector3(from_position.x, from_position.y - m_Definition.TopBottomOffset, from_position.z);
		
		Vector3 from_normal = new Vector3(
			Mathf.Cos(from_angle),
			0,
			Mathf.Sin(from_angle)
		);

		Vector3 from_layer_position = new Vector3(from_position.x, layer_from_y, from_position.z);

		Vector3 from_center_position = from_layer_position - from_normal * inner_offset;
		
		
		Vector3 to_position = new Vector3(
			m_Definition.Radius * Mathf.Cos(to_angle),
			0,
			m_Definition.Radius * Mathf.Sin(to_angle)
		);

		Vector3 to_position_offset = new Vector3(to_position.x, to_position.y + m_Definition.TopBottomOffset, to_position.z);
		
		Vector3 to_normal = new Vector3(
			Mathf.Cos(to_angle),
			0,
			Mathf.Sin(to_angle)
		);

		Vector3 to_layer_position = new Vector3(to_position.x, layer_to_y, to_position.z);

		Vector3 to_center_position = to_layer_position - to_normal * inner_offset;
		
		// polyline
		List<CMeshGenerator.CPolylinePoint> polyline = new List<CMeshGenerator.CPolylinePoint>();
		
		// up - input
		polyline.Add(new CMeshGenerator.CPolylinePoint{Position = from_position, Up        = -from_normal, PointType = CMeshGenerator.EPolylinePointType.End});
		polyline.Add(new CMeshGenerator.CPolylinePoint{Position = from_position_offset, Up = -from_normal, PointType = CMeshGenerator.EPolylinePointType.End});
		polyline.Add(new CMeshGenerator.CPolylinePoint{Position = from_layer_position, Up  = new Vector3(0, 1, 0)});
		polyline.Add(new CMeshGenerator.CPolylinePoint{Position = from_center_position, Up = new Vector3(0, 1, 0)});

		float angle_delta       = to_angle - from_angle;
		int   angle_steps_count = Mathf.Max(Mathf.CeilToInt(Mathf.Abs(angle_delta) / ANGLE_STEP), 3); // at least 3 steps are needed
		float angle_step        = angle_delta / angle_steps_count;

		{
			for (int step = 1; step < angle_steps_count; ++step)
			{
				//float blend = (float)step / angle_steps_count;
				float blend = (float)(step - 1) / (angle_steps_count - 2); // first and last must be at correct height

				float angle = from_angle + step * angle_step;

				Vector3 position = new Vector3(
					(m_Definition.Radius - inner_offset) * Mathf.Cos(angle),
					Mathf.Lerp(layer_from_y, layer_to_y, blend),
					(m_Definition.Radius - inner_offset) * Mathf.Sin(angle)
				);

				polyline.Add(new CMeshGenerator.CPolylinePoint { Position = position, Up = new Vector3(0, 1, 0) });
			}
		}
		
		// down - output
		polyline.Add(new CMeshGenerator.CPolylinePoint{Position = to_center_position, Up = new Vector3(0, 1, 0)});
		polyline.Add(new CMeshGenerator.CPolylinePoint{Position = to_layer_position, Up  = to_normal});
		polyline.Add(new CMeshGenerator.CPolylinePoint{Position = to_position_offset, Up = to_normal, PointType = CMeshGenerator.EPolylinePointType.End});
		polyline.Add(new CMeshGenerator.CPolylinePoint{Position = to_position, Up        = to_normal, PointType = CMeshGenerator.EPolylinePointType.End});

		// normalize steepness
		if(polyline.Count > 2)
		{
			float[] distance_from_start = new float[polyline.Count]; 
			float   polyline_length     = 0;
			for (int i = 3; i < polyline.Count - 2; ++i)
			{
				polyline_length        += Vector3.Distance(polyline[i - 1].Position, polyline[i].Position);
				distance_from_start[i] =  polyline_length;
			}


			Vector3 from = polyline[2].Position;
			Vector3 to   = polyline[^3].Position; 
			for (int i = 2; i < polyline.Count - 2; ++i)
			{
				//float blend = (i - 1) / (float) (polyline.Count - 2);
				float blend = distance_from_start[i] / polyline_length;
				
				Vector3 normalized_position = new Vector3(
					polyline[i].Position.x,
					Mathf.Lerp(from.y, to.y, blend),
					polyline[i].Position.z
				);

				polyline[i] = new CMeshGenerator.CPolylinePoint {Position = normalized_position, Up = polyline[i].Up};
			}
		}

		// merge points that are too close
		{
			const float MERGE_DISTANCE = 1f;
			for (int i = 0; i < polyline.Count; ++i)
			{
				if (polyline[i].PointType != CMeshGenerator.EPolylinePointType.Common)
				{
					continue;
				}

				float prev_distance = Vector3.Distance(polyline[i].Position, polyline[i - 1].Position);
				float next_distance = Vector3.Distance(polyline[i].Position, polyline[i + 1].Position);
				if (prev_distance < MERGE_DISTANCE)
				{
					polyline[i - 1].Position = (polyline[i].Position + polyline[i - 1].Position) * 0.5f;
					polyline.RemoveAt(i);
					--i;
				}
			}
		}

		// mark arcs
		{
			const float ANGLE_COS_THRESHOLD = 0.9f;
			for (int i = 1; i < polyline.Count - 1; ++i)
			{
				if (
					Vector3.Dot(
						(polyline[i].Position - polyline[i - 1].Position).normalized,
						(polyline[i                        + 1].Position - polyline[i].Position).normalized
					) < ANGLE_COS_THRESHOLD
				)
				{
					polyline[i].PointType = CMeshGenerator.EPolylinePointType.Arc;
				}
			}
		}
		
		// each arc needs space before and after it to make nice smooth transition
		{
			const float ARC_SAFE_DISTANCE = 6f;
			for (int i = 1; i < polyline.Count - 1; ++i)
			{
				if (polyline[i].PointType != CMeshGenerator.EPolylinePointType.Arc)
				{
					continue;
				}

				for (int j = i - 1; j > 0; --j)
				{
					if (polyline[j].PointType != CMeshGenerator.EPolylinePointType.Common)
					{
						break; // cannot reduce more
					}

					if (Vector3.Distance(polyline[j].Position, polyline[i].Position) < ARC_SAFE_DISTANCE)
					{
						polyline.RemoveAt(j);
						--i;
					}
				}

				for (int j = i + 1; j < polyline.Count; ++j)
				{
					if (polyline[j].PointType != CMeshGenerator.EPolylinePointType.Common)
					{
						break; // cannot reduce more
					}

					if (Vector3.Distance(polyline[j].Position, polyline[i].Position) < ARC_SAFE_DISTANCE)
					{
						polyline.RemoveAt(j);
						--j;
					}
				}
			}
		}
		
		// if there are two arc points one after another - insert one point in the middle so the arcs will not overlap
		{
			for (int i = 1; i < polyline.Count - 1; ++i)
			{
				if (
					polyline[i].PointType == CMeshGenerator.EPolylinePointType.Arc
					&&
					polyline[i + 1].PointType == CMeshGenerator.EPolylinePointType.Arc
				)
				{
					polyline.Insert(i + 1, new CMeshGenerator.CPolylinePoint
					                       {
						                       Position = (polyline[i].Position + polyline[i + 1].Position) * 0.5f,
						                       Up       = polyline[i].Up
					                       }
					);
				}
			}
		}
		
		// generate arcs
		{
			const int ARC_SUBDIVISION = 16;
			
			for (int i = 1; i < polyline.Count - 1; ++i)
			{
				if (polyline[i].PointType != CMeshGenerator.EPolylinePointType.Arc)
				{
					continue;
				}

				Vector3 arc_start  = polyline[i - 1].Position;
				Vector3 arc_middle = polyline[i].Position;
				Vector3 arc_end    = polyline[i + 1].Position;
				Vector3 arc_up     = polyline[i].Up;

				// make arc symmetric (also fixes problem with bent long edges)
				float distance_to_start = Vector3.Distance(arc_start, arc_middle);
				float distance_to_end   = Vector3.Distance(arc_end,   arc_middle);
				if (distance_to_start < distance_to_end)
				{
					arc_end = arc_middle + (arc_end - arc_middle).normalized * distance_to_start;
				}
				else
				{
					arc_start = arc_middle + (arc_start - arc_middle).normalized * distance_to_end;
				}

				polyline.RemoveAt(i);

				// simple curve patch
				for (int j = 1; j < ARC_SUBDIVISION - 1; ++j)
				{
					float blend = (float) j / ARC_SUBDIVISION;
					polyline.Insert(i, new CMeshGenerator.CPolylinePoint
					                   {
						                   Position = Vector3.Lerp(
							                   Vector3.Lerp(arc_start,  arc_middle, blend),
							                   Vector3.Lerp(arc_middle, arc_end,    blend),
							                   blend
						                   ),
						                   Up = arc_up
					                   }
					);
					++i;
				}
			}
		}

		// setup radii
		{
			const float SIZE_CORRECTION  = 1.4142135623730950488f; // size correction if profile is rotated by 45 degrees
			float       middle_radius    = m_Definition.ProfileSize         / 2f * SIZE_CORRECTION;
			float       start_end_radius = m_Definition.ProfileStartEndSize / 2f * SIZE_CORRECTION;

			float total_length = 0;
			for (int i = 1; i < polyline.Count; ++i)
			{
				total_length += Vector3.Distance(polyline[i - 1].Position, polyline[i].Position);
			}

			float distance_from_start = 0;
			for (int i = 0; i < polyline.Count; ++i)
			{
				float radius;
				if (distance_from_start < m_Definition.ProfileSizeChangeDistance)
				{
					radius = Mathf.Lerp(start_end_radius, middle_radius, distance_from_start / m_Definition.ProfileSizeChangeDistance);
				}
				else if (total_length - distance_from_start < m_Definition.ProfileSizeChangeDistance)
				{
					radius = Mathf.Lerp(start_end_radius, middle_radius, (total_length - distance_from_start) / m_Definition.ProfileSizeChangeDistance);
				}
				else
				{
					radius = middle_radius;
				}

				polyline[i].Radius = radius;

				if (i < polyline.Count - 1)
				{
					distance_from_start += Vector3.Distance(polyline[i].Position, polyline[i + 1].Position);
				}
			}
		}

		// extrude path
		CMeshGenerator.ExtrudePolylineRotated(polyline, vertices, indices);
		
		// gizmo - debug
		foreach (CMeshGenerator.CPolylinePoint point in polyline)
		{
			m_GizmoLines.Add(point.Position);
		}
	}
}