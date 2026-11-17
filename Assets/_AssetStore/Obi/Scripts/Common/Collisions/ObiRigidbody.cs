using UnityEngine;
using System;
using System.Collections;

namespace Obi{

	/**
	 * Small helper class that lets you specify Obi-only properties for rigidbodies.
	 */

	[ExecuteInEditMode]
	[RequireComponent(typeof(Rigidbody))]
	public class ObiRigidbody : ObiRigidbodyBase
	{
		private Rigidbody unityRigidbody;
        private Quaternion prevRotation;
        private Vector3 prevPosition;

        public override void OnEnable()
        {
			unityRigidbody = GetComponent<Rigidbody>();
            prevPosition = transform.position;
            prevRotation = transform.rotation;
            base.OnEnable();
		}

        private void UpdateKinematicVelocities(float stepTime)
        {
            // differentiate positions/orientations to get our own velocites for kinematic objects.
            // when calling Physics.Simulate, MovePosition/Rotation do not work correctly. Also useful for animations.
            //
            // 【修改说明】Unity 2020.1+ 起，对 Kinematic 刚体写 velocity/angularVelocity 会被忽略，并每帧打印
            // "Setting linear velocity of a kinematic body is not supported." 警告（WebGL 下会刷屏数千条，
            // 既污染日志也浪费性能）。既然写入本就被引擎忽略（Obi 从 FromRigidbody 读到的速度同样恒为 0），
            // 这里直接跳过该写入，行为保持一致。
#if !UNITY_2020_1_OR_NEWER
            if (unityRigidbody.isKinematic)
            {
                // differentiate positions to obtain linear velocity:
                unityRigidbody.velocity = (transform.position - prevPosition) / stepTime;

                // differentiate rotations to obtain angular velocity:
                Quaternion delta = transform.rotation * Quaternion.Inverse(prevRotation);
                unityRigidbody.angularVelocity = new Vector3(delta.x, delta.y, delta.z) * 2.0f / stepTime;
            }
#endif

            prevPosition = transform.position;
            prevRotation = transform.rotation;
        }

		public override void UpdateIfNeeded(float stepTime)
        {
            UpdateKinematicVelocities(stepTime);

            var rb = ObiColliderWorld.GetInstance().rigidbodies[handle.index];
            rb.FromRigidbody(unityRigidbody, kinematicForParticles);
            ObiColliderWorld.GetInstance().rigidbodies[handle.index] = rb;

        }

		/**
		 * Reads velocities back from the solver.
		 */
		public override void UpdateVelocities(Vector3 linearDelta, Vector3 angularDelta)
        {
			// kinematic rigidbodies are passed to Obi with zero velocity, so we must ignore the new velocities calculated by the solver:
			if (Application.isPlaying && !(unityRigidbody.isKinematic || kinematicForParticles))
            {
                unityRigidbody.velocity += linearDelta;
                unityRigidbody.angularVelocity += angularDelta;
            }
        }
	}
}

