/***

	This script was made by Jonathan Kings for use within the Unity Asset "Haze Triangulator".
	You are free to modify this file for your own use or to redistribute it, but please do not remove this header.
	Thanks for using Haze assets in your projects :)

***/

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Haze {

	public class ExamplePath : MonoBehaviour {

#pragma warning disable CS0649
		[Tooltip("Whether to display the handles allowing to modify the path")] public bool viewHandles = true;
		[Tooltip("Whether to view the triangulated results")] public bool viewTriangles = true;
		[HideInInspector] public List<Vector2> points = new List<Vector2> { new Vector2(-0.7f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(-0.5f, -0.5f) };
		[HideInInspector] public List<Triangulator.Triangle> triangles = new List<Triangulator.Triangle>();
#pragma warning restore CS0649

#if UNITY_EDITOR

		/** Resets vertices to their initial disposition. */
		public void __Reset() {
			points = new List<Vector2> { new Vector2(-0.7f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(-0.5f, -0.5f) };
			triangles = new List<Triangulator.Triangle>();
		}

		/** Add one point to the path, in between the first and last vertices. */
		public void __AddPoint() {
			if(points.Count < 2)
				points.Add(new Vector2(1, 1));
			else {
				Vector2 p1 = points[points.Count - 1];
				Vector2 p2 = points[0];
				points.Add((p1 + p2) * 0.5f);
			}
		}

		/** Triangulate the path. */
		public void __Triangulate() {
			triangles = Triangulator.Triangulate(points);
			if(triangles == null) triangles = new List<Triangulator.Triangle>();
		}




		private void line(Vector2 one, Vector2 two) {
			Gizmos.DrawLine(transform.TransformPoint(new Vector3(one.x, one.y, 0)), transform.TransformPoint(new Vector3(two.x, two.y, 0)));
		}

		private void OnDrawGizmos() {
			if(points.Count < 3) return;

			//Draw path
			Gizmos.color = Color.blue;
			for(int i = 0; i < points.Count; ++i) {
				Vector2 p1 = points[i];
				Vector2 p2 = points[(i + 1) % points.Count];
				line(p1, p2);
			}

			//Draw triangles
			if(viewTriangles) {
				Gizmos.color = Color.red;
				foreach(Triangulator.Triangle tri in triangles) {
					line(tri.a, tri.b);
					line(tri.b, tri.c);
					line(tri.c, tri.a);
				}
			}
		}
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(ExamplePath))]
	public class ExamplePathEditor : Editor {

		private void repaintScene() {
			EditorWindow view = EditorWindow.GetWindow<SceneView>();
			view.Repaint();
		}

		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			ExamplePath ep = target as ExamplePath;
			if(!ep) return;

			if(GUILayout.Button("Add vertex")) {
				Undo.RecordObject(ep, "Add vertex to path");
				ep.__AddPoint();
				repaintScene();
			}
			if(GUILayout.Button("Reset vertices")) {
				Undo.RecordObject(ep, "Reset path vertices");
				ep.__Reset();
				repaintScene();
			}
			if(GUILayout.Button("Triangulate")) {
				Undo.RecordObject(ep, "Triangulate path");
				ep.__Triangulate();
				repaintScene();
			}
			GUILayout.Label("Vertex count: " + ep.points.Count);
			GUILayout.Label("Triangle count: " + ep.triangles.Count);
		}

		protected virtual void OnSceneGUI() {
			ExamplePath ep = target as ExamplePath;
			if(!ep) return;

			if(!ep.viewHandles) return;

			EditorGUI.BeginChangeCheck();
			for(int i = 0; i < ep.points.Count; ++i) {
				Vector3 epAsV3 = new Vector3(ep.points[i].x, ep.points[i].y, 0);
				epAsV3 = ep.transform.InverseTransformPoint(Handles.PositionHandle(ep.transform.TransformPoint(epAsV3), Quaternion.identity));
				ep.points[i] = new Vector2(epAsV3.x, epAsV3.y);
			}
			if(EditorGUI.EndChangeCheck()) {

			}
		}

	}
#endif

}