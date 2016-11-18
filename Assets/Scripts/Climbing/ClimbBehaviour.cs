using UnityEngine;

namespace Climbing {
	public class ClimbBehaviour : MonoBehaviour {
		public bool climbing;
		bool _initClimb;
		bool _waitToStartClimb;

		Animator _anim;
		//ClimbIK _ik;

		GridManager _curManager;
		Point _targetPoint;
		Point _curPoint;
		Point _prevPoint;
		Neighbour neighbour;
		ConnectionType curConnection;

		ClimbState _climbState;
		ClimbState _targetState;

		public enum ClimbState {
			onPoint,
			betweenPoints,
			inTransit,
		}

		// Curve movement
		CurvesHolder _curvesHolder;
		BezierCurve _directCurveHorizontal;
		BezierCurve _directCurveVertical;
		BezierCurve _dismountCurve;
		BezierCurve _mountCurve;
		BezierCurve _curCurve;
		
		// Interpolation
		Vector3 _interpolStart;
		Vector3 _interpolTarget;
		float _interpolDistance;
		float _interpolT;
		bool _initTransit;
		bool _rootReached;
		bool _ikFollowSideReached;
		bool _ikLandSideReached;

		// Input
		bool _lockInput;
		Vector3 _inputDirection;
		Vector3 _targetPosition;

		public Vector3 rootOffset = new Vector3(0, -0.86f, 0);
		public float speedLinear = 3;
		public float speedDirect = 2;

		public AnimationCurve jumpingCurve;
		public AnimationCurve mountCurve;
		public bool enableRootMovement;
		float _rmMax = 0.25f;
		float _rmT;



		void SetCurveReference(){
			GameObject chPrefab = Resources.Load("CurvesHolder") as GameObject;
			GameObject chGO = Instantiate(chPrefab) as GameObject;

			_curvesHolder = chGO.GetComponent<CurvesHolder>();

			_directCurveHorizontal = _curvesHolder.ReturnCurve(CurveType.horizontal);
			_directCurveVertical = _curvesHolder.ReturnCurve(CurveType.vertical);
			_dismountCurve = _curvesHolder.ReturnCurve(CurveType.dismount);
			_mountCurve = _curvesHolder.ReturnCurve(CurveType.mount);
		}

		void Start(){
			_anim = GetComponent<Animator>();
			//_ik = GetComponent<ClimbIK>();
			SetCurveReference();

			transform.parent = null;
		}

		void FixedUpdate(){
			if(climbing){
				if(!_waitToStartClimb){
					HandleClimbing();
					InitiateFallOff();
				}else{
					InitClimbing();
					HandleMount();
				}
			}else{
				if(_initClimb){
					transform.parent = null;
					_initClimb = false;
				}

				if(Input.GetKey(KeyCode.Space))
					LookForClimbSpot();
			}

		}

		void HandleClimbing(){
			if(!_lockInput){
				float h = Input.GetAxis("Horizontal");
				float v = Input.GetAxis("Vertical");

				_inputDirection = ConvertToInputDirection(h, v);
				if(_inputDirection != Vector3.zero){
					switch(_climbState){
						case ClimbState.onPoint:
							OnPoint(_inputDirection);
							break;
						case ClimbState.betweenPoints:
							BetweenPoints(_inputDirection);
							break;
					}
				}
			}
		}

		Vector3 ConvertToInputDirection(float h, float v){
			Vector3 dir = Vector3.zero;

			if(h != 0){
				dir.x = h < 0 ? -1 : 1;
			}

			if(v != 0){
				dir.y = v < 0 ? -1 : 1;
			}

			return dir;
		}

		void OnPoint(Vector3 direction){

		}

		void BetweenPoints(Vector3 direction){

		}

		void InitiateFallOff(){
			if(_climbState != ClimbState.onPoint || !Input.GetKey(KeyCode.X))
				return;

			climbing = false;
			_initTransit = false;
			//ik.AddWeightInfluenceAll(0);
			GetComponent<Controller.StateManager>().EnableController();
			_anim.SetBool("onAir", true);
		}

		void InitClimbing(){
			if(_initClimb)
				return;

			_initClimb = true;

			// if(ik != null){
			// 	ik.UpdateAllPointsOnOne(_targetPoint);
			// 	ik.UpdateAllTargetPositions(_targetPoint);
			// 	ik.ImmediatePlaceHelpers();
			// }
			
			curConnection = ConnectionType.direct;
			_targetState = ClimbState.onPoint;
		}

		void HandleMount(){
			if(!_initTransit){
				_initTransit = true;

				_ikFollowSideReached = false;
				_ikLandSideReached = false;
				_interpolT = 0;
				_interpolStart = transform.position;
				_interpolTarget = _targetPosition + rootOffset;

				_curCurve = _mountCurve;
				_curCurve.transform.rotation = _targetPoint.transform.rotation;
				var points = _curCurve.GetAnchorPoints();
				points[0].transform.position = _interpolStart;
				points[points.Length-1].transform.position = _interpolTarget;
			}

			if(enableRootMovement)
				_interpolT += Time.deltaTime;

			if(_interpolT >= 1){
				_interpolT = 1;
				_waitToStartClimb = false;
				_lockInput = false;
				_initTransit = false;
				_ikLandSideReached = false;
				_climbState = _targetState; 
			}

			var targetPos = _curCurve.GetPointAt(_interpolT);
			transform.position = targetPos;

			HandleWeightAll(_interpolT, mountCurve);
			HandleRotation();
		}

		void HandleWeightAll(float t, AnimationCurve curve){

		}

		void HandleRotation(){
			Vector3 targetDir = _targetPoint.transform.forward;

			if(targetDir == Vector3.zero)
				targetDir = transform.forward;

			Quaternion targetRot = Quaternion.LookRotation(targetDir);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5);
		}

		void LookForClimbSpot(){
			Transform camTrans = Camera.main.transform;

			var ray = new Ray(camTrans.position, camTrans.forward);
			RaycastHit hit;
			LayerMask mask = ~((1 << gameObject.layer) | (1 << 3));

			float maxDistance = 20;
			if(!Physics.Raycast(ray, out hit, maxDistance, mask)){
				return;
			}

			var manager = hit.transform.GetComponent<GridManager>();
			if(manager == null)
				return;

			var closestPoint = manager.GetClosestPoint(transform.position);
			float distance = Vector3.Distance(transform.position, closestPoint.transform.parent.position);

			if(distance > 5){
				return;
			}

			_curManager = manager;
			_targetPoint = closestPoint;
			_targetPosition = closestPoint.transform.position;
			_curPoint = closestPoint;
			climbing = true;
			_lockInput = true;
			_targetState = ClimbState.onPoint;

			_anim.CrossFade("To_Climb", 0.4f);
			GetComponent<Controller.StateManager>().DisableController();

			_waitToStartClimb = true;
		}
	}
}