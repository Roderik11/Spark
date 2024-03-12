using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;

namespace Spark.Client
{
    using System.Windows.Forms;
    using Jitter;
    using Jitter.Collision.Shapes;
    using Jitter.Dynamics;
    using Jitter.Dynamics.Constraints;
    using Jitter.LinearMath;

    public class NPCController : Component, IUpdate
    {
        public float Radius = .35f;
        public float Height = 2f;
        public Vector3 Center = new Vector3(0, -1, 0);

        public float RunSpeed = 5.2f;
        public float WalkSpeed = 2.4f;
        public float BackwardSpeed = 1.5f;

        private Animator animator;
        private CapsuleShape shape;
        private RigidBody body;
        private CharacterControllerConstraint controller;

        static System.Random random = new Random();
        private Vector3 roamTarget;
        private Vector2 smoothInputVector;


        protected override void Awake()
        {
            animator = Entity.GetComponent<Animator>();

            shape = new CapsuleShape(Height, Radius);

            body = new RigidBody(shape);
            body.Tag = Entity;
            body.Position = Transform.Position;
            body.SetMassProperties(JMatrix.Zero, 1.0f, true);
            body.Material.Restitution = 0.0f;

            controller = new CharacterControllerConstraint(Physics.World, body);
            //controller.OnJump += delegate () { animator.SetParam("jump", 1); };

            Physics.World.AddBody(body);
            Physics.World.AddConstraint(controller);
        }

        float timer;
        float interval = 5;
        bool isWalking;
        bool run;

        public void Update()
        {
            shape.Length = Height;
            shape.Radius = Radius;
            body.IsActive = true;

            timer += Time.Delta;
            if(timer > interval)
            {
                isWalking = random.Next(0, 100) > 50;
                if(isWalking)
                    run = random.Next(0, 100) > 50;
                timer = 0;
                interval = random.Next(5, 10);
                roamTarget = Transform.Position + new Vector3(random.NextFloat(-4, 4), 0, random.NextFloat(-4, 4));
            }

            bool jump = false;

            Vector3 targetVelocity = JVector.Zero;


            if (isWalking)
            {
                targetVelocity = Transform.Forward * WalkSpeed;

                if (run)
                    targetVelocity = Transform.Forward * RunSpeed;
            }

            roamTarget.Y = Transform.Position.Y;

            var mat = Matrix.LookAtLH(Transform.Position, roamTarget, Vector3.Up);
            var look = Quaternion.RotationMatrix(mat);

            Entity.Transform.Rotation = Quaternion.Slerp(Entity.Transform.Rotation, look, Time.Delta * 5);
            body.Orientation = Matrix.RotationQuaternion(Transform.Rotation);

            if (animator != null)
            {
                var axis = new Vector2();
                if (isWalking)
                {
                    axis.Y = run ? 1f : .5f;
                }

                smoothInputVector = Vector2.Lerp(smoothInputVector, axis, Math.Min(1, Time.Delta * 10));

                animator.SetParam("axisX", smoothInputVector.X);
                animator.SetParam("axisY", smoothInputVector.Y);

                //animator.SetParam("speed", isWalking ?(run ? 2 : 1) : 0);
                //animator.SetParam("jump", jump ? 1 : 0);
            }

           // if (targetVelocity.LengthSquared() > 0.0f) targetVelocity.Normalize();
           //// targetVelocity *= 3.5f;
           // targetVelocity *= (run ? 3.5f : 1.40f);

            controller.TryJump = jump;
            controller.TargetVelocity = targetVelocity;

            Transform.Position = body.Position;
            Transform.Position -= new Vector3(0, Height / 2 + Radius, 0);
        }

    
    }

}
