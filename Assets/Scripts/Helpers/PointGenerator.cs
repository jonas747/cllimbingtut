using UnityEngine;
using System.Collections.Generic;

namespace Climbing {

	[ExecuteInEditMode]
	public class PointGenerator : MonoBehaviour {
		[HeaderAttribute("Helper properties")]
		public bool dismountPoint;
		public bool fallPoint;
		public bool hangingPoints;
		public bool singlePoint;

		[HeaderAttribute("Yup")]
		public bool updatePoints;

		[HeaderAttribute("Helpers")]
		public bool deleteAll;
		public bool createIndicators;

		public GameObject pointPrefab;
		float posInterval = 0.5f;

		public Point left;
		public Point right;

		public List<Point> pointsInOrder;
	 

		void LoadPrefab(){
			pointPrefab = Resources.Load("Point") as GameObject;
			if(pointPrefab == null){
				Debug.LogError("No point found");
			}
		}

		void Update(){
			if(updatePoints){
				LoadPrefab();
				UpdatePoints();
				updatePoints = false;
			}

			if(createIndicators){
				LoadPrefab();
				if(singlePoint){
					CreateIndicatorSingle();
				}else{	
					CreateIndicators();
				}
				createIndicators = false;
			}

			if(deleteAll){
				var points = GetComponentsInChildren<Point>();
				foreach(var point in points){
					if(point != left && point != right) DestroyImmediate(point.transform.parent.gameObject);
				}
				pointsInOrder = new List<Point>(){left, right};
			}
		}

		void UpdatePoints(){
			LoadPrefab();
			Point[] points = GetComponentsInChildren<Point>();
			
			if(singlePoint){
				pointsInOrder = new List<Point>();
				
				foreach(var p in points){
					pointsInOrder.Add(p);
				}

				return;
			}

			RemoveOldPoints(points);
			Creatpoints();
		}

		void RemoveOldPoints(Point[] points){
			foreach(var point in points){
				if(point != left && point != right) DestroyImmediate(point.transform.parent.gameObject);				
			}
		}

		void Creatpoints(){
			float width = Vector3.Distance(left.transform.parent.position, right.transform.parent.position);
			int numPoints = Mathf.FloorToInt(width/posInterval);

			var deltaPos = right.transform.parent.position-left.transform.parent.position;
				
			pointsInOrder = new List<Point>();
			pointsInOrder.Add(left);
			for(int i = 1; i < numPoints-1; i++){
				var curPos = (deltaPos/numPoints)*i;

				curPos -= deltaPos/2;

				GameObject point = Instantiate(pointPrefab, curPos, Quaternion.identity) as GameObject;
				point.transform.SetParent(transform, false);
				pointsInOrder.Add(point.GetComponentInChildren<Point>());
			}
			pointsInOrder.Add(right);
		}

		void CreateIndicators(){
			if(left != null) DestroyImmediate(left.transform.parent.gameObject);
			if(right != null) DestroyImmediate(right.transform.parent.gameObject);
			
			left = createIndicator(Vector3.left/2).GetComponentInChildren<Point>();
			left.transform.parent.gameObject.name = "Left Point";
			right = createIndicator(Vector3.right/2).GetComponentInChildren<Point>();
			right.transform.parent.gameObject.name = "Right point";
		}

		void CreateIndicatorSingle(){
			if(left != null) DestroyImmediate(left.transform.parent.gameObject);

			left = createIndicator().GetComponentInChildren<Point>();
		}

		GameObject createIndicator(Vector3 pos = default(Vector3), Quaternion rot = default(Quaternion)){
			GameObject p = Instantiate(pointPrefab, pos, rot) as GameObject;
			p.transform.SetParent(transform, false);
			return p;
		}

	}
}