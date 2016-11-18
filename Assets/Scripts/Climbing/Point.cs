using UnityEngine;
using System.Collections.Generic;

namespace Climbing {
	public class Point : MonoBehaviour{
		public List<Neighbour> neighbours = new List<Neighbour>();
		public List<IKPosition> iks = new List<IKPosition>();

		public IKPosition GetIK(AvatarIKGoal goal){
			return iks.Find(x => x.ik == goal);
		}

		public Neighbour GetNeighbour(Point target){
			return neighbours.Find(x => x.target == target);
		}

		public Neighbour GetNeighbour(Vector3 direction){
			return neighbours.Find(x => x.direction == direction);
		}

		public Point GetNeighbourPoint(Vector3 direction){
			var n = GetNeighbour(direction);
			if(n != null)
				return n.target;

			return null;
		}
	}

	[System.Serializable]
	public class IKPosition {
		public AvatarIKGoal ik;
		public Transform target;
		public Transform hint;
	}

	[System.Serializable]
	public class Neighbour {
		public Vector3 direction;
		public Point target;
		public ConnectionType cType;
	}

	public enum ConnectionType{
		inBetween,
		direct,
		dismount,
		fall,
	}
}