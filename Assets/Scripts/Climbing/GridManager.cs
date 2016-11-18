using UnityEngine;
using System.Collections.Generic;

namespace Climbing {

	public class GridManager : MonoBehaviour{
		public List<Point> points = new List<Point>();

		void Start(){
			points.Clear();
			points.AddRange(GetComponentsInChildren<Point>());
		}

		public Point GetClosestPoint(Vector3 pos){
			Point currentClosest = null;
			float closestDist = Mathf.Infinity;

			foreach(var point in points){
				var dist = Vector3.Distance(pos, point.transform.position);
				if(dist < closestDist){
					closestDist = dist;
					currentClosest = point;
				}
			}

			return currentClosest;
		}
	}
}