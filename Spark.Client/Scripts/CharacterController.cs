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

    public class CharacterController : Component, IUpdate
    {
        public float Radius = .35f;
        public float Height = 2.2f;
        public Vector3 Center = new Vector3(0, 1, 0);
        public bool ShowCapsule = false;

        public float RunSpeed = 5.2f;
        public float WalkSpeed = 2.5f;
        public float BackwardSpeed = 1.5f;

        private Animator animator;
        private CapsuleShape shape;
        private RigidBody body;
        private CharacterControllerConstraint controller;

        private Vector2 smoothInputVector;

        protected override void Awake()
        {
            animator = Entity.GetComponent<Animator>();
            //animator.OnBoneTransform += HeadRotate;

            shape = new CapsuleShape(Height, Radius);

            body = new RigidBody(shape);
            body.Tag = Entity;
            body.Position = Transform.Position;
            body.SetMassProperties(JMatrix.Zero, 1.0f, true);
            body.Material.Restitution = 0.0f;

            controller = new CharacterControllerConstraint(Physics.World, body);
            controller.OnJump += delegate () { animator.SetParam("jump", 1); animator.SetParam("landing", 0); };
            controller.OnLand += delegate () { animator.SetParam("landing", 1); animator.SetParam("falling", 0); };
            controller.OnFall += delegate () { animator.SetParam("falling", 1); };

            Physics.World.AddBody(body);
            Physics.World.AddConstraint(controller);
        }

        void HeadRotate(Bone bone, ref Matrix matrix)
        {
            if (bone.Name.Contains("Head"))
            {
                // bring bone into world space
                // rotate towards target (with limits)
                // bring back into object space

                float dir = Vector3.Dot(Transform.Forward, Camera.Main.Forward);
                float sign = Math.Sign(dir);

                float angleV = Vector3.Dot(Transform.Forward, Camera.Main.Right);
                matrix *= Matrix.RotationAxis(Vector3.Up, -angleV * sign);

                float angleH = Vector3.Dot(Transform.Forward, Camera.Main.Up);
                matrix *= Matrix.RotationAxis(matrix.Right, -angleH);
            }
        }

        public void Update()
        {
            body.EnableDebugDraw = ShowCapsule;
            shape.Length = Height;
            shape.Radius = Radius;

            Vector3 targetVelocity = JVector.Zero;

            body.IsActive = true;

            bool forward = false;
            bool right = false;
            bool left = false;
            bool backward = false;
            bool run = Input.Shift;

           // if (Input.IsKeyDown(Keys.S))
           //     run = false;

            if (Input.IsKeyDown(Keys.W)) targetVelocity += Transform.Forward;
            else if (Input.IsKeyDown(Keys.S))targetVelocity -= Transform.Forward;
            if (Input.IsKeyDown(Keys.A)) targetVelocity -= Transform.Right;
            else if (Input.IsKeyDown(Keys.D)) targetVelocity += Transform.Right;

            if (animator != null)
            {
                if (Input.IsKeyDown(Keys.W))
                    forward = true;
                else if (Input.IsKeyDown(Keys.S))
                    backward = true;

                if (Input.IsKeyDown(Keys.A))
                    left = true;
                else if (Input.IsKeyDown(Keys.D))
                    right = true;
                
                if (!Input.IsKeyDown(Keys.Space))
                    animator.SetParam("jump", 0);

                Vector2 axis = Vector2.Zero;

                if (forward)
                    axis.Y = run ? 1f : .5f;
                else if(backward)
                    axis.Y = run ? -1f : -.5f;

                if (left)
                    axis.X = run ? -1 : -.5f;
                else if (right)
                    axis.X = run ? 1 : .5f;

                smoothInputVector = Vector2.Lerp(smoothInputVector, axis, Math.Min(1, Time.SmoothDelta * 6));

                animator.SetParam("axisX", smoothInputVector.X);
                animator.SetParam("axisY", smoothInputVector.Y);

                var coll = animator.GetParam("ColliderY");
                var grav = animator.GetParam("Gravity");

                //shape.Length = Height * (coll > 0 ? coll : 1);
                //Center.Y = 1 + (1 - (coll > 0 ? coll : 1));
                //body.GravityWeight = grav;// > 0 ? grav : 1;
            }

            if (!targetVelocity.IsZero)
            {
                Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(WowCamera.angleY, 0, 0);
                //Entity.Transform.Rotate(Vector3.UnitY, Input.MouseDelta.X * Time.SmoothDelta * 50);
                body.Orientation = Matrix.RotationQuaternion(Transform.Rotation);
                //Entity.Transform.RotateLocal(Vector3.UnitX, Input.MouseDelta.Y * Time.ElapsedSeconds * 50);
            }

            float speed = 0;

            if(left || right)
                speed = run ? RunSpeed : WalkSpeed;

            if (forward)
                speed = run ? RunSpeed : WalkSpeed;

            if (backward)
                speed = run ? BackwardSpeed * 3 : BackwardSpeed;

            if (targetVelocity.LengthSquared() > 0.0f) targetVelocity.Normalize();
            targetVelocity *= speed;// (run ? 4f : 1.5f);

            controller.TryJump = Input.IsKeyDown(Keys.Space);
            controller.TargetVelocity = targetVelocity;

            Transform.Position = body.Position;
            Transform.Position -= Center;// new Vector3(0, Height / 2 + Radius, 0);
        }
    }

    public class CharacterControllerConstraint : Constraint
    {
        private const float JumpVelocity = 2.25f;

        private float feetPosition;
        private JVector deltaVelocity = JVector.Zero;
        private bool shouldIJump = false;
        private bool canIJump = false;

        public World World { private set; get; }
        public JVector TargetVelocity { get; set; }
        public bool TryJump { get; set; }
        public RigidBody BodyWalkingOn { get; set; }

        public event Action OnJump;
        public event Action OnLand;
        public event Action OnFall;

        public float LandingDistance = .5f;
        private float lastGroundDistance;

        public CharacterControllerConstraint(World world, RigidBody body)
            : base(body, null)
        {
            this.World = world;

            // determine the position of the feets of the character
            // this can be done by supportmapping in the down direction.
            // (furthest point away in the down direction)
            JVector vec = JVector.Down;
            JVector result = JVector.Zero;

            // Note: the following works just for normal shapes, for multishapes (compound for example)
            // you have to loop through all sub-shapes -> easy.
            body.Shape.SupportMapping(ref vec, out result);

            // feet position is now the distance between body.Position and the feets
            // Note: the following '*' is the dot product.
            feetPosition = result * JVector.Down;
        }

        bool inAir;
        bool shouldILand;

        public override void PrepareForIteration(float timestep)
        {
            // send a ray from our feet position down.
            // if we collide with something which is 0.05f units below our feets remember this!

            RigidBody resultingBody = null;
            JVector normal; float frac;

            bool result = World.CollisionSystem.Raycast(Body1.Position + JVector.Down * (feetPosition - 0.1f), JVector.Down, RaycastCallback,
                out resultingBody, out normal, out frac);

            BodyWalkingOn = (result && frac <= 0.2f) ? resultingBody : null;
            canIJump = (result && frac <= 0.2f && Body1.LinearVelocity.Y < JumpVelocity);
            shouldIJump = (result && frac <= 0.2f && Body1.LinearVelocity.Y < JumpVelocity && TryJump);

            if (frac > LandingDistance)
            {
                inAir = true;
                OnFall?.Invoke();
            }

            shouldILand = inAir && (result && frac < LandingDistance);// && lastGroundDistance > LandingDistance);

            lastGroundDistance = frac;

            if (shouldILand || canIJump)
            {
                inAir = false;
                OnLand?.Invoke();
            }

            if (shouldIJump)
            {
                Body1.IsActive = true;
                Body1.ApplyImpulse(JumpVelocity * JVector.Up * Body1.Mass);
                if (OnJump != null) OnJump();

                // apply the negative impulse to the other body
                if (!BodyWalkingOn.IsStatic)
                {
                    BodyWalkingOn.IsActive = true;
                    BodyWalkingOn.ApplyImpulse(-1.0f * JumpVelocity * JVector.Up * Body1.Mass);
                }

            }
        }

        private bool RaycastCallback(RigidBody body, JVector normal, float fraction)
        {
            // prevent the ray to collide with ourself!
            return (body != this.Body1);
        }

        public override void Iterate()
        {
            deltaVelocity = TargetVelocity - Body1.LinearVelocity;
            deltaVelocity.Y = 0.0f;

            // determine how 'stiff' the character follows the target velocity
            deltaVelocity *= 0.02f;

            if (deltaVelocity.LengthSquared() != 0.0f)
            {
                // activate it, in case it fall asleep :)
                Body1.IsActive = true;
                Body1.ApplyImpulse(deltaVelocity * Body1.Mass);
            }
        }
    }
}
