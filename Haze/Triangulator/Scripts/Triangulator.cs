/***

	This script was made by Jonathan Kings for use within the Unity Asset "Haze Triangulator".
	You are free to modify this file for your own use or to redistribute it, but please do not remove this header.
	Thanks for using Haze assets in your projects :)

***/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Haze {

	/**
	 * Static class with utilities for triangulating paths made up of 2D vertices, adding the resulting triangles to a mesh, as well as a few other geometric utilities.
	 * Unless expressed otherwise, most methods in this class assume non-self-intersecting convex or concave polygons for which winding order does not matter.
	 */
	public static class Triangulator {

		/**
		 * Represents a single 2D triangle made up of vertices a, b and c. Simple container class.
		 * Winding order does not matter; the Triangulator class can wind triangles clockwise or counterclockwise when filling up the mesh data.
		 */
		[Serializable]
		public struct Triangle {
			public Vector2 a, b, c;
			
			/** Constructor with params for the three vertices of the triangle. */
			public Triangle(Vector2 _a, Vector2 _b, Vector2 _c) {
				a = _a;
				b = _b;
				c = _c;
			}
		}

		/**
		 * Represents a single 2D line segment made up of vertices a and b.
		 */
		[Serializable]
		public struct Segment {
			public Vector2 a, b;
			
			/** Constructor with params for the two vertices of the segment. */
			public Segment(Vector2 _a, Vector2 _b) {
				a = _a;
				b = _b;
			}

			/** Utility to get the center point of the line segment. */
			public Vector2 Center { get { return (a + b) * 0.5f; } }
		}



		/**
		 * Modifies the order of the vertices in a path by removing the first vertex and appending it to the back.
		 * Does not modify the implied geometry of the polygon as the final vertex is implied to connect to the initial one.
		 */
		public static void RotatePathClockwise(ref List<Vector2> path) {
			if(path == null) throw new ArgumentNullException("Path must not be null!");
			if(path.Count < 1) {
				Debug.LogWarning("Attempting to rotate a path with no vertices.");
				return;
			}
			path.Add(path[0]);
			path.RemoveAt(0);
		}

		/**
		 * Returns true if line segments [p1, p2] and [p3, p4] intersect, and fills in parameter intersection with the point of intersection.
		 * Param allowOnLine can be set to true to consider one line passing through the end point of the other as an intersection, or to false to consider strictly the lines themselves without their endpoints.
		 * Adapted from https://github.com/setchi/Unity-LineSegmentsIntersection by Kevin Masson
		 */
		public static bool LineSegmentsIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector3 p4, out Vector2 intersection, bool allowOnLine = true) {
			intersection = Vector2.zero;

			var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

			if(d == 0.0f) {
				return false;
			}

			var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
			var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

			if(u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f) {
				return false;
			}

			if(!allowOnLine && (u == 0 || u == 1 || v == 0 || v == 1))
				return false;

			intersection.x = p1.x + u * (p2.x - p1.x);
			intersection.y = p1.y + u * (p2.y - p1.y);

			return true;
		}

		/**
		 * Returns an axis-aligned bounding box for a 2d polygon; that is, the smallest possible box which contains all points of the polygon.
		 */
		public static Rect aabbFromPath(List<Vector2> path) {
			if(path.Count < 1) throw new ArgumentException("Path must have length 1 at least!");
			float minX, maxX, minY, maxY;
			minX = maxX = path[0].x;
			minY = maxY = path[0].y;
			foreach(Vector2 v in path) {
				if(v.x < minX) minX = v.x;
				if(v.x > maxX) maxX = v.x;
				if(v.y < minY) minY = v.y;
				if(v.y > maxY) maxY = v.y;
			}
			return Rect.MinMaxRect(minX, minY, maxX, maxY);
		}

		/**
		 * Returns whether a 2D point is contained inside a polygon. This uses the ray casting algorithm, as described at https://en.wikipedia.org/wiki/Point_in_polygon#Ray_casting_algorithm.
		 * Note that the polygon can be self-intersecting.
		 */
		public static bool IsPointInsidePolygon(List<Vector2> polygon, Vector2 point) {
			if(polygon == null) throw new ArgumentNullException("Polygon cannot be null");
			if(point == null) throw new ArgumentNullException("Point cannot be null");

			Rect aabb = aabbFromPath(polygon);
			//Cast a ray from point to positive x
			Segment ray = new Segment(point, new Vector2(aabb.max.x, point.y));
			//Count intersections with path
			int intersections = 0;
			for(int i = 0; i<polygon.Count; ++i) {
				Vector2 p1 = polygon[i];
				Vector2 p2 = polygon[(i + 1) % polygon.Count];
				Vector2 unused;
				if(LineSegmentsIntersection(p1, p2, ray.a, ray.b, out unused)) {
					++intersections;
				}
			}

			return intersections % 2 == 1;
		}

		/**
		 * Utility for determining whether two 3D points are essentially the same (correcting for floating-point errors)
		 */
		public static bool IsVector3ApproximatelyEqual(Vector3 a, Vector3 b) {
			return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
		}

		/**
		 * Returns whether the polygon is clockwise or counterclockwise.
		 * Note that the polygon can be self-intersecting; this will return whether it is *mostly* clockwise or *mostly* counterclockwise.
		 * If the polygon is self-intersecting and a perfect balance of clockwise and counterclockwise, the method will return true by default.
		 * This assumes a cartesian coordinate system; that is, positive values on the X axis are to the right, and positive values on the Y axis are upward.
		 */
		public static bool IsPolygonClockwise(List<Vector2> polygon) {
			if(polygon == null) throw new ArgumentNullException("Polygon must not be null!");
			if(polygon.Count < 3) throw new ArgumentException("Polygon cannot have less than 3 vertices!");
			float sum = 0;
			for(int i = 0; i<polygon.Count; ++i) {
				Vector2 p1 = polygon[i];
				Vector2 p2 = polygon[(i + 1) % polygon.Count];
				sum += (p2.x - p1.x) * (p2.y + p1.y);
			}
			return sum >= 0;
		}

		/**
		 * Returns whether the triangle is clockwise or counterclockwise.
		 * This assumes a cartesian coordinate system; that is, positive values on the X axis are to the right, and positive values on the Y axis are upward.
		 * The triangle's order is assumed a, b, c.
		 */
		public static bool IsTriangleClockwise(Triangle tri) {
			return IsPolygonClockwise(new List<Vector2> { tri.a, tri.b, tri.c });
		}

		/**
		 * Ensures a triangle is clockwise or counterclockwise, according to value of clockwise argument.
		 * The triangle's order should be considered a, b, c.
		 */
		public static Triangle CertifyWindingOrder(Triangle tri, bool clockwise) {
			if(IsTriangleClockwise(tri) != clockwise) {
				//invert winding order of triangle
				Vector2 cache = tri.b;
				tri.b = tri.c;
				tri.c = cache;
			}
			return tri;
		}

		/**
		 * Ensures a collection of triangles are all clockwise or counterclockwise, according to value of clockwise argument.
		 */
		public static void CertifyWindingOrder(ref List<Triangle> tris, bool clockwise) {
			if(tris == null) throw new ArgumentNullException("Tris is null; invalid operation.");
			for(int i = 0; i<tris.Count; ++i) {
				tris[i] = CertifyWindingOrder(tris[i], clockwise);
			}
		}


		/**
		 * Recursive algorithm for splitting a 2D non-self-intersecting convex or concave polygon into a small amount of triangles.
		 * Call by passing subsequentCalls as the value 0.
		 * This does not guarantee the minimal amount of triangles, but will attempt to keep the number as small as possible.
		 * The triangles returned by this method are not guaranteed to have a certain winding order, but can be passed through the CertifyWindingOrder methods to do so afterwards.
		 * Note: the algorithm might be too slow to use for modifying a mesh in realtime, therefore it is advised to use it strictly for baking data in editor.
		 */
		public static List<Triangle> Triangulate(List<Vector2> pathSource, int subsequentCalls = 0) {
			if(pathSource == null) throw new ArgumentNullException("PathSource must not be null!");

			if(pathSource.Count < 3) {
				Debug.LogWarning("Cannot triangulate path with less than 3 vertices. Path has " + pathSource.Count + " vertices.");
				return null;
			}

			//Copy path source over to path
			List<Vector2> path = new List<Vector2>();
			foreach(Vector2 vert in pathSource) path.Add(vert);

			Segment segment = new Segment(path[1], path[path.Count - 1]);

			bool segmentValid = true;

			//Does the center point of the segment lie outside the polygon?
			if(!IsPointInsidePolygon(path, segment.Center)) {
				segmentValid = false;
			}

			if(segmentValid) {
				//Does the segment [1, L-1] cut through the shape at any point?
				Vector2 center = segment.Center;
				for(int i = 0; i < path.Count; ++i) {
					Vector2 p1 = path[i];
					Vector2 p2 = path[(i + 1) % path.Count];
					Vector2 unused;
					if(LineSegmentsIntersection(p1, p2, segment.a, segment.b, out unused, false)) {
						segmentValid = false;
						break;
					}
				}
			}

			if(!segmentValid) {
				//Try again from the next vertex

				if(subsequentCalls > path.Count) {
					//Rotated fully the vertices and never found a break. Something bad happened :'(
					Debug.LogError("Cannot triangulate path. Rotated too many times subsequently with no improvements.");
					return null;
				}

				RotatePathClockwise(ref path);
				return Triangulate(path, subsequentCalls+1);
			}

			//Ok! the segment being valid, let's extract that triangle from the path and carry on.
			Triangle firstTri = new Triangle(path[0], path[1], path[path.Count-1]);
			path.RemoveAt(0);
			if(path.Count < 3) {
				return new List<Triangle> { firstTri };
			} else {
				//still have more than 4 vertices, keep going deeper
				List<Triangle> result = new List<Triangle> { firstTri };
				result.AddRange(Triangulate(path));
				return result;
			}
		}

		/**
		 * When building a 3D mesh from a list of vertices and indices (called triangles in Unity's APIs), use this utility to add a vertex to the mesh to ensure
		 * that no duplicate vertices exist, instead referencing the same vertex by its index.
		 * Note that typically for building a mesh, converting the list of vertices and indices to arrays would be required before passing to the Mesh APIs.
		 */
		public static void AddVertexToMesh(ref List<Vector3> vertices, ref List<int> indices, Vector3 vertex) {
			for(int i = 0; i<vertices.Count; ++i) {
				if(IsVector3ApproximatelyEqual(vertices[i], vertex)) {
					indices.Add(i);
					return;
				}
			}
			vertices.Add(vertex);
			indices.Add(vertices.Count - 1);
		}

		/**
		 * Allows to add a single triangle to a mesh's data; usually you'd use the AddTrianglesToMesh method instead to add all triangles at once.
		 * Parameter z will determine the depth of the vertices on the mesh in local space.
		 * Parameter clockwise will determine whether triangles will be added in clockwise or counterclockwise order.
		 * Unity uses a clockwise winding order for front-facing polygons (however you may want back-facing polygons instead).
		 * If you wish to change the way the triangles are added (for example, on a different plane than XY), you will have to write your own method for this purpose.
		 * Note that typically for building a mesh, converting the list of vertices and indices to arrays would be required before passing to the Mesh APIs.
		 */
		public static void AddTriangleToMesh(ref List<Vector3> vertices, ref List<int> indices, Triangle tri, float z, bool clockwise) {
			tri = CertifyWindingOrder(tri, clockwise);//make triangle clockwise (or counterclockwise)
			//Add the three vertices, in order.
			AddVertexToMesh(ref vertices, ref indices, new Vector3(tri.a.x, tri.a.y, z));
			AddVertexToMesh(ref vertices, ref indices, new Vector3(tri.b.x, tri.b.y, z));
			AddVertexToMesh(ref vertices, ref indices, new Vector3(tri.c.x, tri.c.y, z));
		}

		/**
		 * Allows to add triangles to a mesh's data.
		 * Parameter z will determine the depth of the vertices on the mesh in local space.
		 * Parameter clockwise will determine whether triangles will be added in clockwise or counterclockwise order.
		 * Unity uses a clockwise winding order for front-facing polygons (however you may want back-facing polygons instead).
		 * If you wish to change the way the triangles are added (for example, on a different plane than XY), you will have to write your own method for this purpose.
		 * Note that typically for building a mesh, converting the list of vertices and indices to arrays would be required before passing to the Mesh APIs.
		 */
		public static void AddTrianglesToMesh(ref List<Vector3> vertices, ref List<int> indices, List<Triangle> tris, float z, bool clockwise) {
			foreach(Triangle tri in tris)
				AddTriangleToMesh(ref vertices, ref indices, tri, z, clockwise);
		}

	}

}