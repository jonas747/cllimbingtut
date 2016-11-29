using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Climbing {
	public class ClimbBehaviour : MonoBehaviour {
		public bool climbing;
		bool _initClimb;
		bool _waitToStartClimb;

		Animator _anim;
		ClimbIK _ik;

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
		float _rmMax = 0.1f;
		float _rmT;

		public List<LerpIK> _lerpingIKs = new List<LerpIK>();

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
			_ik = GetComponent<ClimbIK>();
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

				if(Input.GetKey(KeyCode.Space)){
					Debug.Log("Space");
					LookForClimbSpot();
				}
			}

		}

		void HandleClimbing(){
			if(!_lockInput){
				float h = Input.GetAxis("Horizontal");
				float v = Input.GetAxis("Vertical");

				if(_climbState == ClimbState.onPoint){
					_ik.UpdateAllTargetPositions(_curPoint);
					//_ik.ImmediatePlaceHelpers();
				}

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
			InitIK(directionToPoint, !twoStep);
		}



		AvatarIKGoal _ikLanding;
		AvatarIKGoal _ikFollowing;
		float _ikT;
		float _fikT;

		Vector3[] _ikStartPos = new Vector3[4];
		Vector3[] _ikTargetPos = new Vector3[4];

		void InitIK(Vector3 directionToPoint, bool opposite){
			Vector3 relativeDirection = transform.InverseTransformDirection(directionToPoint);

			if(Mathf.Abs(relativeDirection.y) > 0.5f){
				float targetAnim = 0;

				if(_targetState == ClimbState.onPoint){
					_ikLanding = ClimbIK.GetOppositeIK(_ikLanding);
				}else{
					if(Mathf.Abs(relativeDirection.x) > 0){
						if(relativeDirection.x < 0)
							_ikLanding = AvatarIKGoal.LeftHand;
						else
							_ikLanding = AvatarIKGoal.RightHand;
					}

					targetAnim = _ikLanding == AvatarIKGoal.RightHand ? 1 : 0;
					if(relativeDirection.y < 0)
						targetAnim = _ikLanding == AvatarIKGoal.RightHand ? 0 : 1;
					
					_anim.SetFloat("Movement", targetAnim);
				}
			}else{
				_ikLanding = relativeDirection.x < 0 ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
				if(opposite){
					_ikLanding = ClimbIK.GetOppositeIK(_ikLanding);
				}
			}

			_ikT = 0;
			UpdateIKTarget(0, _ikLanding, _targetPoint);

			_ikFollowing = ClimbIK.GetOppositeLimb(_ikLanding);
			_fikT = 0;
			UpdateIKTarget(1, _ikFollowing, _targetPoint);
		}

		void UpdateIKTarget(int posIndex, AvatarIKGoal ikGoal, Point point){
			_ikStartPos[posIndex] = _ik.GetCurrentPointPosition(ikGoal);
			_ikTargetPos[posIndex] = point.GetIK(ikGoal).target.transform.position;
			_ik.UpdatePoint(ikGoal, point);
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
			float speed = speedLinear * Time.deltaTime;
			float lerpSpeed = speed / _interpolDistance;

			_ikT += lerpSpeed * 2;
			if(_ikT > 1){
				_ikT = 1;
				_ikLandSideReached = true;
			}

			Vector3 ikPosition = Vector3.Lerp(_ikStartPos[0], _ikTargetPos[0], _ikT);
			_ik.UpdateTargetPosition(_ikLanding, ikPosition);

			_fikT += lerpSpeed * 2;
			if(_fikT > 1){
				_fikT = 1;
				_ikFollowSideReached = true;
			}

			Vector3 followSide = Vector3.Lerp(_ikStartPos[1], _ikTargetPos[1], _fikT);
			_ik.UpdateTargetPosition(_ikFollowing, followSide);
		}

		void UpdateDirectVariables(Vector3 direction){
			if(_initTransit) return;
			
			_initTransit = true;

			enableRootMovement = false;
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

			InitIKDirect(direction);
		}

		void InitIKDirect(Vector3 directionToPoint){

			var delayedSide = AvatarIKGoal.LeftHand;
			if(directionToPoint.x < 0){
				delayedSide = AvatarIKGoal.RightHand;
			}
			var instantSide = ClimbIK.GetOppositeIK(delayedSide);

			_ik.UpdateAllPointsOnOne(_targetPoint);


			_ik.UpdateTargetPosition(instantSide, _targetPoint.GetIK(instantSide).target.transform.position);
			_ik.UpdateTargetPosition(ClimbIK.GetOppositeLimb(instantSide), _targetPoint.GetIK(ClimbIK.GetOppositeLimb(instantSide)).target.transform.position);
		}

		void InitIKOpposite(){
			//UpdateIKTarget(2, ClimbIK.GetOppositeIK(_ikLanding), _curPoint);
			UpdateIKTarget(3, ClimbIK.GetOppositeIK(_ikFollowing), _targetPoint);
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

		void DirectHandleIK(){
		}

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

			_ikT = 0;
			_fikT = 0;
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

		void HandleDismountIK(){
			if(enableRootMovement)
				_ikT += Time.deltaTime * 3;

			_fikT += Time.deltaTime * 2;

			HandleIKWeightDismount(_ikT, _fikT, 1, 0);
		}

		void HandleIKWeightDismount(float ht, float ft, float from, float to){
			
			float t1 = ht * 3;
			if(t1 > 1){
				t1 = 1;
				_ikLandSideReached = true;
			}

			float handsWeight = Mathf.Lerp(from, to, t1);
			_ik.InfluenceWeight(AvatarIKGoal.LeftHand, handsWeight);
			_ik.InfluenceWeight(AvatarIKGoal.RightHand, handsWeight);
			
			float t2 = ft;
			if(t2 > 1){
				t2 = 1;
				_ikFollowSideReached = true;
			}

			float feetWeight = Mathf.Lerp(from, to, t2);
			_ik.InfluenceWeight(AvatarIKGoal.LeftFoot, feetWeight);
			_ik.InfluenceWeight(AvatarIKGoal.RightFoot, feetWeight);

		}

		bool _waitForWrapUp;

		void WrapUp(bool direct = false){
			if(!_rootReached || _anim.GetBool("Jump") || _waitForWrapUp){
				return;
			}

			if(_targetState == ClimbState.onPoint){
				_ik.UpdateAllTargetPositions(_targetPoint);
			}

			StartCoroutine(WrapUpTransition(0.15f));
			_waitForWrapUp = true;
		}

		IEnumerator WrapUpTransition(float t){
			yield return new WaitForSeconds(t);

			_climbState = _targetState;
			if(_climbState == ClimbState.onPoint){
				_curPoint = _targetPoint;
				_anim.SetBool("Move", false);
			}

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
					Debug.Log(transitDir.ToString());
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
					Debug.Log("Moving to pervpoint?");
				}else{
					Debug.Log("Doing nothing");
				}
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
			_ik.AddWeightInfluenceAll(0);
			GetComponent<Controller.StateManager>().EnableController();
			_anim.SetBool("onAir", true);
		}

		void InitClimbing(){
			if(_initClimb)
				return;

			_initClimb = true;

			if(_ik != null){
				_ik.UpdateAllPointsOnOne(_targetPoint);
				_ik.UpdateAllTargetPositions(_targetPoint);
				_ik.ImmediatePlaceHelpers();
			}
			
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
				_interpolT += Time.deltaTime*2;

			if(_interpolT >= 1){
				_interpolT = 1;
				_waitToStartClimb = false;
				_lockInput = false;
				_initTransit = false;
				_ikLandSideReached = false;
				_climbState = _targetState; 
			}

			var targetPos = _curCurve.GetPointAt(_interpolT);
			Debug.DrawLine(transform.position, targetPos, Color.gray);
			Debug.DrawLine(transform.position, _interpolTarget, Color.black);
			transform.position = targetPos;

			HandleWeightAll(_interpolT, mountCurve);
			HandleRotation();
		}

		void HandleWeightAll(float t, AnimationCurve curve){
			float inf = curve.Evaluate(t);
			Debug.Log("Inf"+inf.ToString() + ", t:" +t.ToString());
			_ik.AddWeightInfluenceAll(inf);
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
				Debug.Log("No hit");
				return;
			}

			Debug.Log(hit.transform.gameObject.name);

			var manager = hit.transform.GetComponent<GridManager>();
			if(manager == null){
				Debug.Log("No manager");
				return;
			}

			var closestPoint = manager.GetClosestPoint(transform.position);
			float distance = Vector3.Distance(transform.position, closestPoint.transform.parent.position);

			if(distance > 5){
				Debug.Log("Too great distance");
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
			Debug.Log("All o kay");
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

	public class LerpIK {
		public AvatarIKGoal ik;
		public ClimbIK ikController;

		public Point targetPoint;

		Vector3 _startPos;
		
		float _t;
		float _waitTime;

		public bool Finished{
			get{return _t >= 1;}
		}

		public LerpIK(ClimbIK ikController, AvatarIKGoal ik, Point targetPoint, float waitTime){
			this.ik = ik;
			this.targetPoint = targetPoint;
			this._waitTime = waitTime;
			this.ikController = ikController;

			_startPos = ikController.GetCurrentPointPosition(ik);
			
		}

		public void Update(){
			if(_waitTime > 0){
				_waitTime -= Time.deltaTime;
				if(_waitTime > 0){
					return;
				}
			}

			_t += Time.deltaTime;
			Vector3 targetPos = targetPoint.GetIK(ik).target.transform.position;
			Vector3 pos = Vector3.Lerp(_startPos, targetPos, _t);
			ikController.UpdateTargetPosition(ik, pos);

			Debug.DrawLine(ikController.transform.position, targetPos, Color.red);
			Debug.DrawLine(ikController.transform.position, pos, Color.blue);
		}
	}
}