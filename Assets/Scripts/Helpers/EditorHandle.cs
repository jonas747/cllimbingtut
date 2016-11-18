#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Climbing {
	[CustomEditor(typeof(DrawWireCube))]
	public class DrawWireCubeEditor : Editor{
		void OnSceneGUI(){
			DrawWireCube t = target as DrawWireCube;
			if(t.ikPos.Count == 0){
				t.ikPos = t.transform.GetComponent<Point>().iks;
			}

			foreach(var ik in t.ikPos){
				if(ik.target == null){
					continue;
				}

				var targetColor = Color.red;
				switch(ik.ik){
					case AvatarIKGoal.LeftFoot:
						targetColor = Color.blue;
						break;
					case AvatarIKGoal.RightFoot:
						targetColor = Color.green;
						break;
					case AvatarIKGoal.LeftHand:
						targetColor = Color.cyan;
						break;
					case AvatarIKGoal.RightHand:
						targetColor = Color.yellow;
						break;
				}

				Handles.color = targetColor;

				Handles.CubeCap(0, ik.target.position, ik.target.rotation, 0.05f);
				if(ik.hint != null){
					Handles.CubeCap(0, ik.hint.position, ik.hint.rotation, 0.05f);
				}
			}
		}
	}

	[CustomEditor(typeof(DrawLineIndividual))]
	public class DrawLineIndividualEditor : Editor{
		void OnSceneGUI(){
			DrawLineIndividual t = target as DrawLineIndividual;

			if(t.point == null){
				Debug.LogError("No point");
				return;
			}

			foreach(var connectedPoint in t.point.neighbours){
				if(connectedPoint.target == null){
					continue;
				}

				var pos1 = t.transform.position;
				var pos2 = connectedPoint.target.transform.position;

				DrawConnectionLine(pos1, pos2, connectedPoint.cType);
			}
		}

		public static void DrawConnectionLine(Vector3 p1, Vector3 p2, ConnectionType cType){
			switch(cType){
				case ConnectionType.direct:
					Handles.color = Color.red;
					break;
				case ConnectionType.dismount:
					Handles.color = Color.blue;
					break;
				case ConnectionType.fall:
					Handles.color = Color.cyan;
					break;
				case ConnectionType.inBetween:
					Handles.color = Color.green;
					break;
			}
			Handles.DrawLine(p1, p2);
		}
	}

	[CustomEditor(typeof(DrawLineGrid))]
	public class DrawLineGridEditor : Editor{
		void OnSceneGUI(){
			DrawLineGrid t = target as DrawLineGrid;
			if(t.grid == null){
				Debug.LogError("No grid");
				return;
			}

			var connections = t.grid.GetAllConnections();
			foreach(var connection in connections){
				DrawLineIndividualEditor.DrawConnectionLine(connection.p1.transform.position, connection.p2.transform.position, connection.cType);
			}
		}
	}
}
#endif