using UnityEngine;
using System.Collections;

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

				transform.parent = _curPoint.transform.parent;

				if(_climbState == ClimbState.onPoint){
					// ik.UpdateAllTargetPositions(_curPoint);
					// ik.ImmediatePlaceHelpers();
				}
			}else{
				InTransit(_inputDirection);
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

		void InTransit(Vector3 direction){
			switch(curConnection){
				case ConnectionType.inBetween:
					UpdateLinearVariables();
					LinearRootMovement();
					LerpIKLandingSideLinear();
					WrapUp();
					break;
				case ConnectionType.direct:
					UpdateDirectVariables(direction);
					DirectRootMovement();
					DirectHandleIK();
					WrapUp(true);
					break;
				case ConnectionType.dismount:
					HandleDismountVariables();
					DismountRootMovement();
					HandleDismountIK();
					DismountWrapUp();
					break;
			}
		}

		void UpdateLinearVariables(){
			if(_initTransit)
				return;

			_initTransit = true;

			enableRootMovement = true;
			_rootReached = false;
			_ikFollowSideReached = false;
			_ikLandSideReached = false;
			_interpolT = 0;
			_interpolStart = transform.position;
			_interpolTarget = _targetPosition + rootOffset;
			
			var directionToPoint = (_interpolTarget - _interpolStart).normalized;
			bool twoStep = _targetState == ClimbState.betweenPoints;
			var back = -transform.forward * 0.05f;
			if(twoStep)
				_interpolTarget += back;

			_interpolDistance = Vector3.Distance(_interpolTarget, _interpolStart);
			//InitIK(directionToPoint, !twoStep);
		}

		void LinearRootMovement(){
			float speed = speedLinear * Time.deltaTime;
			float lerpSpeed = speed / _interpolDistance;
			_interpolT += lerpSpeed;

			if(_interpolT >= 1){
				_interpolT = 1;
				_rootReached = true;
			}

			transform.position = Vector3.Lerp(_interpolStart, _interpolTarget, _interpolT);
			HandleRotation();
		}

		void LerpIKLandingSideLinear(){

		}

		void UpdateDirectVariables(Vector3 direction){
			if(_initTransit) return;
			
			_initTransit = true;

			enableRootMovement = true;
			_rootReached = false;
			_ikFollowSideReached = false;
			_ikLandSideReached = false;
			_interpolT = 0;
			_rmT = 0;
			_interpolTarget = _targetPosition + rootOffset;
			_interpolStart = transform.position;

			bool vertical = Mathf.Abs(direction.y) > 0.1f;
			_curCurve = vertical ? _directCurveVertical : _directCurveHorizontal;
			_curCurve.transform.rotation = _curPoint.transform.rotation;

			if(vertical && direction.y <= 0){
				Vector3 eulers = _curCurve.transform.eulerAngles;
				eulers.x = 180;
				eulers.y = 180;
				_curCurve.transform.eulerAngles = eulers;
				
			}else if(!vertical && direction.x <= 0){
				Vector3 eulers = _curCurve.transform.eulerAngles;
				eulers.y = -180;
				_curCurve.transform.eulerAngles = eulers;
			}

			var points = _curCurve.GetAnchorPoints();
			points[0].transform.position = _interpolStart;
			points[points.Length - 1].transform.position = _interpolTarget;

			//InitIKDirect(direction);
		}

		void DirectRootMovement(){
			if(enableRootMovement){
				_interpolT += Time.deltaTime * speedDirect;
			}else{
				if(_rmT < _rmMax)
					_rmT += Time.deltaTime;
				else
					enableRootMovement = true;
			}

			if(_interpolT > 0.95f){
				_interpolT = 1;
				_rootReached = true;
			}

			HandleWeightAll(_interpolT, jumpingCurve);

			transform.position = _curCurve.GetPointAt(_interpolT);
			HandleRotation();		
		}

		void DirectHandleIK(){}

		void HandleDismountVariables(){
			if(_initTransit) return;
			_initTransit = true;

			enableRootMovement = false;
			_rootReached = false;
			_ikLandSideReached = false;
			_ikFollowSideReached = false;
			_interpolT = 0;
			_rmT = 0;
			_interpolStart = transform.position;
			_interpolTarget = _targetPosition;

			_curCurve = _dismountCurve;
			var points = _curCurve.GetAnchorPoints();
			_curCurve.transform.rotation = transform.rotation;
			points[0].transform.position = _interpolStart;
			points[points.Length - 1].transform.position = _interpolTarget;

			// _ikT = 0;
			// _fikT = 0;
		}

		void DismountRootMovement(){
			if(enableRootMovement)
				_interpolT += Time.deltaTime / 2;

			if(_interpolT >= 1){
				_interpolT = 1;
				_rootReached = true;
			}

			transform.position = _curCurve.GetPointAt(_interpolT);
		}

		void HandleDismountIK(){}

		bool _waitForWrapUp;

		void WrapUp(bool direct = false){
			if(!_rootReached || _anim.GetBool("Jump") || _waitForWrapUp) return;

			StartCoroutine(WrapUpTransition(0.05f));
			_waitForWrapUp = true;
		}

		IEnumerator WrapUpTransition(float t){
			yield return new WaitForSeconds(t);

			_climbState = _targetState;
			if(_climbState == ClimbState.onPoint)
				_curPoint = _targetPoint;

			_initTransit = false;
			_lockInput = false;
			_inputDirection = Vector3.zero;
			_waitForWrapUp = false;
		}

		void DismountWrapUp(){
			if(_rootReached){
				climbing = false;
				_initTransit = false;
				GetComponent<Controller.StateManager>().EnableController();
			}
		}

		void OnPoint(Vector3 direction){
			neighbour = _curPoint.GetNeighbour(direction);

			if(neighbour == null){
				return;
			}

			_targetPoint = neighbour.target;
			_prevPoint = _curPoint;
			_climbState = ClimbState.inTransit;

			UpdateconnectionTransitByType(neighbour, direction);

			_lockInput = true;
		}

		void UpdateconnectionTransitByType(Neighbour n, Vector3 inputDirection){
			var desiredPosition = Vector3.zero;
			curConnection = n.cType;

			Vector3 direction = _targetPoint.transform.position - _curPoint.transform.position;
			direction.Normalize();
		
			switch(n.cType){
				case ConnectionType.inBetween:
				
					var dist = Vector3.Distance(_curPoint.transform.position, _targetPoint.transform.position);
					desiredPosition = _curPoint.transform.position + (direction * (dist/2));
					_targetState = ClimbState.betweenPoints;
					TransitDir transitDir = TransitDirection(inputDirection, false);
					PlayAnim(transitDir);
					break;
				case ConnectionType.direct:

					desiredPosition = _targetPoint.transform.position;
					_targetState = ClimbState.onPoint;
					transitDir = TransitDirection(direction, false);
					PlayAnim(transitDir);
					break;
				case ConnectionType.dismount:

					desiredPosition = _targetPoint.transform.position;
					_anim.SetInteger("JumpType", 20);
					_anim.SetBool("Move", true);
					break;
			}

			_targetPosition = desiredPosition;
		}

		void BetweenPoints(Vector3 direction){
			var n = _targetPoint.GetNeighbour(_prevPoint);
			if(n != null){
				if(direction == n.direction){
					_targetPoint = _prevPoint;
				}
			}else{
				_targetPoint = _curPoint;
			}

			_targetPosition = _targetPoint.transform.position;
			_climbState = ClimbState.inTransit;
			_targetState = ClimbState.onPoint;
			_prevPoint = _curPoint;
			_lockInput = true;
			_anim.SetBool("Move", false);
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

		void PlayAnim(TransitDir dir, bool jump = false){
			int target = 0;

			switch(dir){
				case TransitDir.moveHorizontal:
					target = 5;
					break;
				case TransitDir.moveVertical:
					target = 6;
					break;
				case TransitDir.jumpUp:
					target = 0;
					break;
				case TransitDir.jumpDown:
					target = 1;
					break;
				case TransitDir.jumpLeft:
					target = 3;
					break;
				case TransitDir.jumpRight:
					target = 2;
					break;
			}

			_anim.SetInteger("JumpType", target);

			if(!jump)
				_anim.SetBool("Move", true);
			else
				_anim.SetBool("Jump", true);

		}

		public TransitDir TransitDirection(Vector3 direction, bool jump){

			float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

			if(!jump){
				if(Mathf.Abs(direction.y) > 0) 
					return TransitDir.moveVertical;

				return TransitDir.moveHorizontal;
			}

			if(Mathf.Abs(direction.y) > Mathf.Abs(direction.x)){
				if(direction.y < 0) 
					return TransitDir.jumpDown;

				return TransitDir.jumpUp;
			}

			if(targetAngle < 22.5f && targetAngle > -22.5f){
				return TransitDir.jumpUp;
			}else if(targetAngle < 180 + 22.5f && targetAngle > 180 - 22.5f){
				return TransitDir.jumpDown;
			}else if(targetAngle < 90 + 22.5f && targetAngle > 90 - 22.5f){
				return TransitDir.jumpRight;
			}else if(targetAngle < -90 + 22.5f && targetAngle > -90 - 22.5f){
				return TransitDir.jumpLeft;
			}

			return TransitDir.moveHorizontal;
		}
	}


	public enum TransitDir{
		moveHorizontal,
		moveVertical,
		jumpUp,
		jumpDown,
		jumpLeft,
		jumpRight,
	}
}