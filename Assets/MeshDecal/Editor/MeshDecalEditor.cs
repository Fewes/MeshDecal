// Copyright (c) 2020 Felix Westin
// This code is licensed under MIT license (see LICENSE for details)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshDecal)), CanEditMultipleObjects]
public class MeshDecalEditor : Editor
{
	SerializedProperty targetMesh, material, offset, removeBackfaces, serialized, hideComponents;

	void OnEnable()
	{
		targetMesh = serializedObject.FindProperty("targetMesh");
		material = serializedObject.FindProperty("m_Material");
		offset = serializedObject.FindProperty("offset");
		removeBackfaces = serializedObject.FindProperty("removeBackfaces");
		serialized = serializedObject.FindProperty("serialized");
		hideComponents = serializedObject.FindProperty("hideComponents");
	}

	public override void OnInspectorGUI ()
	{
		//DrawDefaultInspector();

		EditorGUILayout.PropertyField(targetMesh);
		EditorGUILayout.PropertyField(material);
		EditorGUILayout.PropertyField(offset);
		EditorGUILayout.PropertyField(removeBackfaces);
		EditorGUILayout.PropertyField(serialized);
		EditorGUILayout.PropertyField(hideComponents);

		serializedObject.ApplyModifiedProperties();
	}
}
