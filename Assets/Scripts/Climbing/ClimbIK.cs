using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Climbing {
	public class ClimbIK : MonoBehaviour{
		public class Limb {
			public Point point;
			public float weight;
			public Transform helper;
			public Vector3 targetPosition;
			public AvatarIKGoal ik;
		}

		Animator _anim;

		// Right/left hand, right/left foot. 4 limbs
		public List<Limb> limbs = new List<Limb> {
			new Limb(),
			new Limb(),
			new Limb(),
			new Limb(),
		};
		
		public float helperSpeed;

		Transform _hips;
		public bool forceFeetHeight;

		void Start(){

			_anim = GetComponent<Animator>();
			_hips = _anim.GetBoneTransform(HumanBodyBones.Hips);

			// Initialize the limbs
			for(int i = 0; i < 4; i++){
				limbs[i].helper = new GameObject().transform;
				limbs[i].ik = (AvatarIKGoal)i;
				limbs[i].helper.name = ((AvatarIKGoal)i).ToString() + " IK Helper";
				Debug.Log(limbs[i].ik);
			}
		}

		public void UpdateAllPointsOnOne(Point targetPoint){
			limbs.ForEach(x => x.point = targetPoint);
		}

		public void UpdatePoint(AvatarIKGoal ik, Point targetPoint){
			limbs[(int)ik].point = targetPoint;
		}

		public void UpdateAllTargetPositions(Point point){
			foreach(var limb in limbs){
				var ikPositions = point.GetIK(limb.ik);
				if(ikPositions != null && ikPositions.target != null)
					limb.targetPosition = ikPositions.target.position;
			}
		}

		public void UpdateTargetPosition(AvatarIKGoal ik, Vector3 targetPosition){
			limbs[(int)ik].targetPosition = targetPosition;
		}

		public Vector3 GetCurrentPointPosition(AvatarIKGoal ik){
			return limbs[(int)ik].point.GetIK(ik).target.transform.position;
		}

		public Point GetPointForIK(AvatarIKGoal ik){
			return limbs[(int)ik].point;
		}

		public static AvatarIKGoal GetOppositeIK(AvatarIKGoal ik){
			switch(ik){
				case AvatarIKGoal.LeftFoot:
					return AvatarIKGoal.RightFoot;
				case AvatarIKGoal.RightFoot:
					return AvatarIKGoal.LeftFoot;
				case AvatarIKGoal.RightHand:
					return AvatarIKGoal.LeftHand;
				case AvatarIKGoal.LeftHand:
					return AvatarIKGoal.RightHand;
			}

			// Should never get to this point
			Debug.LogError("Passed invalid IKGoal !!!!");
			return AvatarIKGoal.LeftFoot;
		}

		public static AvatarIKGoal GetOppositeLimb(AvatarIKGoal ik){
			switch(ik){
				case AvatarIKGoal.LeftFoot:
					return AvatarIKGoal.LeftHand;
				case AvatarIKGoal.RightFoot:
					return AvatarIKGoal.RightHand;
				case AvatarIKGoal.RightHand:
					return AvatarIKGoal.RightFoot;
				case AvatarIKGoal.LeftHand:
					return AvatarIKGoal.LeftFoot;
			}

			// Should never get to this point
			Debug.LogError("Passed invalid IKGoal !!!!");
			return AvatarIKGoal.LeftFoot;
		}

		public void AddWeightInfluenceAll(float w){
			limbs.ForEach(x => x.weight = w);
		}

		public void InfluenceWeight(AvatarIKGoal ik, float w){
			limbs[(int)ik].weight = w;
		}

		public void ImmediatePlaceHelpers(){
			foreach(var limb in limbs){
				if(limb.point != null){
					limb.helper.position = limb.targetPosition;
				}
			}
		}

		void OnAnimatorIK(){
			if(_hips == null)
				_hips = _anim.GetBoneTransform(HumanBodyBones.Hips);

			foreach(var limb in limbs){
				if(!limb.point){
					continue;
				}

				var ikPos = limb.point.GetIK(limb.ik);

				if(ikPos != null && ikPos.target != null){
					var targetPos = limb.targetPosition;

					
					if(limb.ik == AvatarIKGoal.RightFoot || limb.ik == AvatarIKGoal.LeftFoot){
						// Feet has them placed at a lower height
						if(forceFeetHeight && targetPos.y > _hips.transform.position.y){
							targetPos.y -= 0.2f;
						}
					}
					
					// TODO PROPER LERPING
					//limb.helper.position = Vector3.Lerp(limb.helper.transform.position, limb.targetPosition, Time.deltaTime * helperSpeed);
					//limb.helper.position = limb.targetPosition;
					limb.helper.position = Vector3.MoveTowards(limb.helper.position, limb.targetPosition, Time.deltaTime * helperSpeed);
				}

				UpdateIK(limb, ikPos);
			}
		}

		void UpdateIK(Limb limb, IKPosition ikPos){
			if(ikPos == null) return;

			_anim.SetIKPositionWeight(limb.ik, limb.weight); 
			_anim.SetIKRotationWeight(limb.ik, limb.weight);
			_anim.SetIKPosition(limb.ik, limb.helper.position);
			_anim.SetIKRotation(limb.ik, limb.helper.rotation);

			Debug.DrawLine(transform.position, limb.helper.position, Color.green);
			//Debug.Log(limb.weight);

			if(limb.ik == AvatarIKGoal.LeftHand || limb.ik == AvatarIKGoal.RightHand){
				var bone = limb.ik == AvatarIKGoal.LeftHand ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder;
				Transform shoulder = _anim.GetBoneTransform(bone);
				
				Vector3 targetRotationDir = shoulder.transform.position - limb.helper.transform.position;
				limb.helper.rotation = Quaternion.LookRotation(-targetRotationDir);
			}else{
				limb.helper.rotation = ikPos.target.transform.rotation;
			}

			if(ikPos.hint != null){
				// Find the proper ik hint
				AvatarIKHint hint;
				if(limb.ik == AvatarIKGoal.LeftFoot) hint = AvatarIKHint.LeftKnee; 
				else if(limb.ik == AvatarIKGoal.RightFoot) hint = AvatarIKHint.RightKnee;
				else if(limb.ik == AvatarIKGoal.RightHand) hint = AvatarIKHint.RightElbow;
				else hint = AvatarIKHint.LeftElbow;

				_anim.SetIKHintPositionWeight(hint, limb.weight);
				_anim.SetIKHintPosition(hint, ikPos.hint.position);
			}
		}

	}
}