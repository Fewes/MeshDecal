// Copyright (c) 2020 Felix Westin
// This code is licensed under MIT license (see LICENSE for details)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshDecal : MonoBehaviour
{
	#region Structs
	internal struct Vertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Vector4 tangent;
		public Vector2 uv;

		public Vertex (Vector3 position, Vector3 normal, Vector4 tangent, Vector2 uv)
		{
			this.position = position;
			this.normal = normal;
			this.tangent = tangent;
			this.uv = uv;
		}

		public static Vertex Lerp (Vertex A, Vertex B, float d)
		{
			return new Vertex() {
				position = Vector3.Lerp(A.position, B.position, d),
				normal = Vector3.Lerp(A.normal, B.normal, d),
				tangent = Vector3.Lerp(A.tangent, B.tangent, d),
				uv = Vector2.Lerp(A.uv, B.uv, d)
			};
		}
	}

	internal struct Triangle
	{
		public Vertex A, B, C;

		public Triangle (Vertex A, Vertex B, Vertex C)
		{
			this.A = A;
			this.B = B;
			this.C = C;
		}

		/// <summary>
		/// Create a new triangle between A, AB, AC
		/// </summary>
		void NewTriangle (
			Vertex A, Vertex B, Vertex C,
			float fA, float fB, float fC,
			ref Queue<Triangle> triangleList)
		{
			float dAB = (1 - fA) / (fB - fA);
			float dAC = (1 - fA) / (fC - fA);
			var AB = Vertex.Lerp(A, B, dAB);
			var AC = Vertex.Lerp(A, C, dAC);

			triangleList.Enqueue(new Triangle() { A = A, B = AB, C = AC });
		}

		/// <summary>
		/// Create two new triangles between B, C, AB, AC
		/// </summary>
		void NewQuad (
			Vertex A, Vertex B, Vertex C,
			float fA, float fB, float fC,
			ref Queue<Triangle> triangleList)
		{
			float dB = (1 - fA) / (fB - fA);
			float dC = (1 - fA) / (fC - fA);
			var AB = Vertex.Lerp(A, B, dB);
			var AC = Vertex.Lerp(A, C, dC);

			triangleList.Enqueue(new Triangle() { A = B, B = C, C = AC });
			triangleList.Enqueue(new Triangle() { A = B, B = AC,  C = AB });
		}

		/// <summary>
		/// Attempt to slice the triangle along the plane defined by the axis-aligned normal
		/// </summary>
		public bool Slice (Vector3 normal, ref Queue<Triangle> triangleList)
		{
			float fA = Vector3.Dot(A.position, normal);
			float fB = Vector3.Dot(B.position, normal);
			float fC = Vector3.Dot(C.position, normal);

			if (fA > 1 && fB > 1 && fC > 1) // Triangle is outside of the projection volume
				return true;

			// Triangles
			if (fA < 1 && fB > 1 && fC > 1)
			{
				NewTriangle(A, B, C, fA, fB, fC, ref triangleList);
				return true;
			}
			else if (fA > 1 && fB < 1 && fC > 1)
			{
				NewTriangle(B, C, A, fB, fC, fA, ref triangleList);
				return true;
			}
			else if (fA > 1 && fB > 1 && fC < 1)
			{
				NewTriangle(C, A, B, fC, fA, fB, ref triangleList);
				return true;
			}
			// Quads
			else if (fA > 1 && fB < 1 && fC < 1)
			{
				NewQuad(A, B, C, fA, fB, fC, ref triangleList);
				return true;
			}
			else if (fA < 1 && fB > 1 && fC < 1)
			{
				NewQuad(B, C, A, fB, fC, fA, ref triangleList);
				return true;
			}
			else if (fA < 1 && fB < 1 && fC > 1)
			{
				NewQuad(C, A, B, fC, fA, fB, ref triangleList);
				return true;
			}

			return false;
		}
	}
	#endregion

	public Transform sourceTransform => targetMesh ? targetMesh : transform.parent;
	public Mesh sourceMesh
	{
		get
		{
			if (!sourceTransform)
				return null;

			var meshFilter = sourceTransform.GetComponent<MeshFilter>();
			if (meshFilter)
				return meshFilter.sharedMesh;

			var skinnedMeshRenderer = sourceTransform.GetComponent<SkinnedMeshRenderer>();
			if (skinnedMeshRenderer)
				return skinnedMeshRenderer.sharedMesh;

			return null;
		}
	}

	MeshFilter _meshFilter;
	public MeshFilter meshFilter
	{
		get
		{
			if (!_meshFilter)
				_meshFilter = GetComponent<MeshFilter>();
			if (!_meshFilter)
				_meshFilter = gameObject.AddComponent<MeshFilter>();
			return _meshFilter;
		}
	}

	public Mesh mesh { get => meshFilter.sharedMesh; set => meshFilter.sharedMesh = value; }

	MeshRenderer _meshRenderer;
	public MeshRenderer meshRenderer
	{
		get
		{
			if (!_meshRenderer)
				_meshRenderer = GetComponent<MeshRenderer>();
			if (!_meshRenderer)
				_meshRenderer = gameObject.AddComponent<MeshRenderer>();
			return _meshRenderer;
		}
	}

	/// <summary>
	/// The material used to render the decal
	/// </summary>
	public Material material
	{
		get => m_Material;
		set
		{
			m_Material = value;
			meshRenderer.sharedMaterial = m_Material;
		}
	}

	[Tooltip("The target object to project the decal onto. Must contain a MeshFilter or SkinnedMeshRenderer component (the resulting decal is not skinned, however). If no target is specified, the parent transform is used")]
	/// <summary>
	/// The target object to project the decal onto. Must contain a MeshFilter or SkinnedMeshRenderer component (the resulting decal is not skinned, however). If no target is specified, the parent transform is used
	/// </summary>
	public Transform targetMesh			= null;

	[Tooltip("The material used to render the decal")]
	[SerializeField]
	Material		m_Material			= null;
	[Tooltip("Offset vertices by their normals. Useful to prevent z-fighting, but note that it is pretty much always better to use a shader with a depth offset if possible")]
	[Range(0, 0.1f)]
	/// <summary>
	/// Offset vertices by their normals. Useful to prevent z-fighting, but note that it is pretty much always better to use a shader with a depth offset if possible
	/// </summary>
	public float	offset				= 0.01f;
	[Tooltip("Remove backfaces. Use this if you want to reduce the amount of geometry needed to render the decal and if it is not seen from behind")]
	/// <summary>
	/// Remove backfaces. Use this if you want to reduce the amount of geometry needed to render the decal and if it is not seen from behind
	/// </summary>
	public bool		removeBackfaces		= true;
	[Tooltip("Serialize the mesh data in the component. If this is not checked, the decal will be recreated during runtime")]
	/// <summary>
	/// Serialize the mesh data in the component. If this is not checked, the decal will be recreated during runtime
	/// </summary>
	public bool		serialized			= true;
	[Tooltip("Hide the components used to render the decal in the inspector")]
	/// <summary>
	/// Hide the components used to render the decal in the inspector
	/// </summary>
	public bool		hideComponents		= true;

	// We can't serialize a mesh, so instead we serialize the components and rebuild it
	[SerializeField]
	List<Vector3> vertices				= new List<Vector3>();
	[SerializeField]
	List<Vector3> normals				= new List<Vector3>();
	[SerializeField]
	List<Vector4> tangents				= new List<Vector4>();
	[SerializeField]
	List<Vector2> uvs					= new List<Vector2>();
	[SerializeField]
	List<Vector2> originalUVs			= new List<Vector2>();
	[SerializeField]
	List<int> triangles					= new List<int>();
	[System.NonSerialized]
	bool meshIsRefreshed				= false;

	readonly List<int> tempTriangles	= new List<int>();

#if UNITY_EDITOR
	[SerializeField, HideInInspector]
	Vector3 m_PrevLocalPos;
	[SerializeField, HideInInspector]
	Vector3 m_PrevLocalEuler;
	[SerializeField, HideInInspector]
	Vector3 m_PrevLocalScale;
#endif

	void OnEnable ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
			UnityEditor.EditorApplication.update +=	EditorUpdate;
#endif

		if (serialized)
		{
			if (!meshIsRefreshed)
				RefreshMesh();
		}
		else
		{
			Recalculate();
		}
	}

	void OnDisable ()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.update -=	EditorUpdate;
#endif
	}

#if UNITY_EDITOR
	void EditorUpdate ()
	{
		if (transform.localPosition != m_PrevLocalPos ||
			transform.localEulerAngles != m_PrevLocalEuler ||
			transform.localScale != m_PrevLocalScale)
		{
			Recalculate();

			m_PrevLocalPos = transform.localPosition;
			m_PrevLocalEuler = transform.localEulerAngles;
			m_PrevLocalScale = transform.localScale;
		}
	}
#endif

	void OnValidate ()
	{
		Recalculate();
	}

	bool IsInsideUnitCube (Vector3 p)
	{
		return Mathf.Abs(p.x) <= 1 && Mathf.Abs(p.y) <= 1 && Mathf.Abs(p.z) <= 1;
	}

	/// <summary>
	/// Recalculate the decal mesh
	/// </summary>
	public void Recalculate ()
	{
		if (!sourceMesh)
			return;

		sourceMesh.GetVertices(vertices);
		sourceMesh.GetNormals(normals);
		sourceMesh.GetTangents(tangents);
		sourceMesh.GetUVs(0, originalUVs);

		if (sourceMesh.subMeshCount == 1)
		{
			sourceMesh.GetTriangles(triangles, 0);
		}
		else
		{
			triangles.Clear();
			for (int i = 0; i < sourceMesh.subMeshCount; i++)
			{
				sourceMesh.GetTriangles(tempTriangles, i);
				triangles.AddRange(tempTriangles);
			}
		}

		var newTriangles = new List<Triangle>();
		var inputTriangles = new Queue<Triangle>();
		var outputTriangles = new Queue<Triangle>();

		var axii = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down, Vector3.back, Vector3.forward };

		for (int t = 0; t < triangles.Count; t += 3)
		{
			var vA = vertices[triangles[t+0]];
			var vB = vertices[triangles[t+1]];
			var vC = vertices[triangles[t+2]];

			var pA = transform.InverseTransformPoint(sourceTransform.TransformPoint(vA));
			var pB = transform.InverseTransformPoint(sourceTransform.TransformPoint(vB));
			var pC = transform.InverseTransformPoint(sourceTransform.TransformPoint(vC));

			// Skip triangles that are entirely outside of the projector volume
			bool removed = false;
			for (int i = 0; i < axii.Length; i++)
			{
				var axis = axii[i];
				float fA = Vector3.Dot(pA, axis);
				float fB = Vector3.Dot(pB, axis);
				float fC = Vector3.Dot(pC, axis);

				if (fA > 1 && fB > 1 && fC > 1)
				{
					removed = true;
					break;
				}
			}
			if (removed)
			{
				continue;
			}

			var nA = transform.InverseTransformDirection(sourceTransform.TransformDirection(normals[triangles[t+0]]));
			var nB = transform.InverseTransformDirection(sourceTransform.TransformDirection(normals[triangles[t+1]]));
			var nC = transform.InverseTransformDirection(sourceTransform.TransformDirection(normals[triangles[t+2]]));

			var tA = transform.InverseTransformDirection(sourceTransform.TransformDirection(tangents[triangles[t+0]]));
			var tB = transform.InverseTransformDirection(sourceTransform.TransformDirection(tangents[triangles[t+1]]));
			var tC = transform.InverseTransformDirection(sourceTransform.TransformDirection(tangents[triangles[t+2]]));

			// Triangle normal check
			if (removeBackfaces)
			{
				var normal = nA + nB + nC;
				if (normal.z > 0)
				{
					continue;
				}
			}

			var A = new Vertex(pA, nA, tA, originalUVs[triangles[t+0]]);
			var B = new Vertex(pB, nB, tB, originalUVs[triangles[t+1]]);
			var C = new Vertex(pC, nC, tC, originalUVs[triangles[t+2]]);

			// Keep triangles that are encapsulated entirely by the projector volume
			if (IsInsideUnitCube(A.position) && IsInsideUnitCube(B.position) && IsInsideUnitCube(C.position))
			{
				newTriangles.Add(new Triangle(A, B, C));
				continue;
			}

			inputTriangles.Clear();
			outputTriangles.Clear();
			inputTriangles.Enqueue(new Triangle(A, B, C));

			for (int i = 0; i < axii.Length; i++)
			{
				var axis = axii[i];

				while (inputTriangles.Count > 0)
				{
					var triangle = inputTriangles.Dequeue();
					if (!triangle.Slice(axis, ref outputTriangles))
						outputTriangles.Enqueue(triangle);
				}

				if (i != axii.Length-1)
				{
					var tmp = inputTriangles;
					inputTriangles = outputTriangles;
					outputTriangles = tmp;
				}
			}

			while (outputTriangles.Count > 0)
				newTriangles.Add(outputTriangles.Dequeue());
		}

		vertices.Clear();
		normals.Clear();
		tangents.Clear();
		originalUVs.Clear();
		triangles.Clear();
		int index = 0;
		foreach (var triangle in newTriangles)
		{
			vertices.Add(triangle.A.position);
			vertices.Add(triangle.B.position);
			vertices.Add(triangle.C.position);
			normals.Add(triangle.A.normal);
			normals.Add(triangle.B.normal);
			normals.Add(triangle.C.normal);
			tangents.Add(triangle.A.tangent);
			tangents.Add(triangle.B.tangent);
			tangents.Add(triangle.C.tangent);
			originalUVs.Add(triangle.A.uv);
			originalUVs.Add(triangle.B.uv);
			originalUVs.Add(triangle.C.uv);
			triangles.Add(index++);
			triangles.Add(index++);
			triangles.Add(index++);
		}

		uvs.Clear();
		for (int i = 0; i < vertices.Count; i++)
		{
			uvs.Add(new Vector2(vertices[i].x*0.5f+0.5f, vertices[i].y*0.5f+0.5f));
			vertices[i] += normals[i] * offset;
		}

		RefreshMesh();
	}

	void RefreshMesh ()
	{
		if (vertices.Count < 1 || triangles.Count < 1)
		{
			// No geometry
			meshRenderer.enabled = false;
			return;
		}
		else
		{
			meshRenderer.enabled = true;
		}

		var meshName = sourceMesh.name + "_" + name;
		if (!mesh || mesh.name != meshName)
			mesh = new Mesh() { name = meshName };

		mesh.Clear();
		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetTangents(tangents);
		mesh.SetTriangles(triangles, 0);
		mesh.SetUVs(0, uvs);
		mesh.SetUVs(1, originalUVs);

		mesh.Optimize();

		meshFilter.hideFlags = hideComponents ? HideFlags.HideInInspector : HideFlags.None;
		meshRenderer.hideFlags = hideComponents ? HideFlags.HideInInspector : HideFlags.None;
		meshRenderer.sharedMaterial = m_Material;

		if (!serialized)
		{
			vertices.Clear();
			normals.Clear();
			tangents.Clear();
			originalUVs.Clear();
			triangles.Clear();
			meshIsRefreshed = false;
		}
		else
		{
			meshIsRefreshed = true;
		}
	}

	void OnDrawGizmosSelected ()
	{
		var p000 = transform.TransformPoint(new Vector3(-1, -1, -1));
		var p100 = transform.TransformPoint(new Vector3( 1, -1, -1));
		var p010 = transform.TransformPoint(new Vector3(-1,  1, -1));
		var p110 = transform.TransformPoint(new Vector3( 1,  1, -1));
		var p001 = transform.TransformPoint(new Vector3(-1, -1,  1));
		var p101 = transform.TransformPoint(new Vector3( 1, -1,  1));
		var p011 = transform.TransformPoint(new Vector3(-1,  1,  1));
		var p111 = transform.TransformPoint(new Vector3( 1,  1,  1));

		Gizmos.color = Color.yellow;

		Gizmos.DrawLine(p000, p100);
		Gizmos.DrawLine(p100, p110);
		Gizmos.DrawLine(p110, p010);
		Gizmos.DrawLine(p010, p000);

		Gizmos.DrawLine(p000, p001);
		Gizmos.DrawLine(p100, p101);
		Gizmos.DrawLine(p110, p111);
		Gizmos.DrawLine(p010, p011);

		Gizmos.color = Color.black;

		Gizmos.DrawLine(p001, p101);
		Gizmos.DrawLine(p101, p111);
		Gizmos.DrawLine(p111, p011);
		Gizmos.DrawLine(p011, p001);
	}
}
