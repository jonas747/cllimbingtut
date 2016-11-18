#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;


namespace Climbing {

	[ExecuteInEditMode]
	public class PointConnectionsGenerator : MonoBehaviour {
		public float maxDistance = 2.5f;
		public float directTreshold = 1;

		public bool updateConnections;
		public bool resetConnections;

		List<Point> _points = new List<Point>();
		Vector3[] _availableDirections = {
			new Vector3(1, 0, 0),
			new Vector3(-1, 0, 0),
			new Vector3(0, 1, 0),
			new Vector3(0, -1, 0),
			new Vector3(-1, -1, 0),
			new Vector3(1, 1, 0),
			new Vector3(1, -1, 0),
			new Vector3(-1, -1, 0),
		};

		void CreateDirections(){
			_availableDirections[0] = new Vector3(1, 0, 0);
			_availableDirections[1] = new Vector3(-1, 0, 0);
			_availableDirections[2] = new Vector3(0, 1, 0);
			_availableDirections[3] = new Vector3(0, -1, 0);
			_availableDirections[4] = new Vector3(-1, -1, 0);
			_availableDirections[5] = new Vector3(1, 1, 0);
			_availableDirections[6] = new Vector3(1, -1, 0);
			_availableDirections[7] = new Vector3(-1, -1, 0);
		}

		void Update(){
			if(resetConnections){
				ResetConnections();
			}
			
			if(updateConnections){
				UpdateConnections();
			}

			updateConnections = false;
			resetConnections = false;
		}

		void SetPoints(){
			_points.Clear();
			_points.AddRange(GetComponentsInChildren<Point>());
		}

		void UpdateConnections(){
			SetPoints();
			CreateConnections();
			RefreshAll();
		}

		void CreateConnections(){
			foreach(var point in _points){
				CreateConnectionsForPoint(point);
			}
		}

		void CreateConnectionsForPoint(Point point){

			foreach(var direction in _availableDirections){
				var candidates = CandidatePointsOnDirection(direction, point);
				Point closest = FindClosest(point.transform.position, candidates);

				if(closest == null)
					continue;

				var distance = Vector3.Distance(point.transform.position, closest.transform.position);
				if(distance > maxDistance)
					continue;
				
				if(Mathf.Abs(direction.x) > 0 && Mathf.Abs(direction.y) > 0 && distance > directTreshold){
					continue;
				}

				var isDirect = distance < directTreshold;

				AddNeighbour(point, closest, direction, isDirect);

			}

		}

		List<Point> CandidatePointsOnDirection(Vector3 targetDirection, Point from){
			var candidates = new List<Point>();

			foreach(var p in _points){
				if(p == from) continue;

				var worldDirection = p.transform.position - from.transform.position;
				var localDirectoin = from.transform.InverseTransformDirection(worldDirection);

				if(IsDirectionValid(targetDirection, localDirectoin)){
					candidates.Add(p);
				}
			}

			return candidates;
		}


		bool IsDirectionValid(Vector3 targetDirection, Vector3 direction){
			
			float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.y) * Mathf.Rad2Deg;
			float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

			if(angle < targetAngle+22.5f && angle > targetAngle - 22.5f)
				return true;

			return false; 
		}

		Point FindClosest(Vector3 pos, List<Point> points){
			Point closest = null;

			foreach(var p in points){
				if(closest == null || Vector3.Distance(pos, p.transform.position) < Vector3.Distance(pos, closest.transform.position)){
					closest = p;
				}
			}

			return closest;
		}

		void AddNeighbour(Point from, Point target, Vector3 targetDir, bool direct){
			var neighbour = new Neighbour();

			neighbour.direction = targetDir;
			neighbour.target = target;
			neighbour.cType = direct ? ConnectionType.direct : ConnectionType.inBetween;
			from.neighbours.Add(neighbour);
			UnityEditor.EditorUtility.SetDirty(from);
		}


		// TODO
		void FindDismountCandidates(){
			GameObject dismountPrefab =  Resources.Load("Dismount") as GameObject;
			if(dismountPrefab == null){
				Debug.LogError("No dismount prefab");
				return;
			}

			var generators = GetComponentsInChildren<PointGenerator>();
			List<Point> candidates = new List<Point>();

			foreach(var generator in generators){
				if(generator.dismountPoint)
					candidates.AddRange(generator.pointsInOrder);
			}

			if(candidates.Count < 1)
				return;

			GameObject parentObj = new GameObject();
			parentObj.name = "Dismount points";
			parentObj.transform.SetParent(transform, false);
			parentObj.transform.position = candidates[0].transform.localPosition;

			foreach(Point p in candidates){	}
		}

		// TODO
		void RefreshAll(){
		}

		void ResetConnections(){
			SetPoints();
			foreach(var point in _points){
				point.neighbours.Clear();
			}
			RefreshAll();
		}

		public List<Connection> GetAllConnections(){
			List<Connection> connections = new List<Connection>();

			foreach(var point in _points){
				foreach(var neighbour in point.neighbours){
					Connection con = new Connection(point, neighbour.target, neighbour.cType);
					if(!ContainsConnection(connections, con)){
						connections.Add(con);
					}
				}
			}

			return connections;
		}

		bool ContainsConnection(List<Connection> connections, Connection c){
			return connections.Exists(x => (x.p1 == c.p1 && x.p2 == c.p2) || (x.p2 == c.p1 && x.p1 == c.p2));
		}

	}

	public class Connection {
		public Point p1;
		public Point p2;
		public ConnectionType cType;

		public Connection(Point p1, Point p2, ConnectionType cType){
			this.p1 = p1;
			this.p2 = p2;
			this.cType = cType;
		}
	}
}

#endif