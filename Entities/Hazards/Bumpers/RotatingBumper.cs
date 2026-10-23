using System;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.MaxHelpingHand.Runtime.Hazards.Bumpers;
using Celeste.Physics.Collision;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.MaxHelpingHand.Entities.Hazards.Bumpers
{
    [CustomEntity("MaxHelpingHand/RotatingBumper")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.RotatingBumper")]
    public class RotatingBumper : BumperNotCoreMode
    {
        private Vector2 center;
        private readonly float radius;
        private readonly float speed;
        private readonly bool wobble;
        private float currentAngle;

        public RotatingBumper(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(data.Position + offset, null, data.Bool("notCoreMode", false), false, engine)
        {
            Vector2 position = Position;
            center = data.NodesOffset(offset)[0];
            radius = ((ISourceVectorEnvironment)engine).VectorArithmetic.Length(center - position);
            speed = (float)((double)data.Float("speed", 360f) * Math.PI / 180.0);
            wobble = data.Bool("wobble", false);
            currentAngle = ScalarMath.WrapAngle(Vector2Geometry.Angle(center, position));
            if (data.Bool("attachToCenter", false))
                Add(new SourceStaticMoverState
                {
                    SolidChecker = solid => SourceEntityCollision.CheckAt(this, solid, center),
                    JumpThruChecker = jumpThru => SourceEntityCollision.CheckAt(this, jumpThru, center),
                    OnMove = amount => center += amount
                });
        }

        public override void Update()
        {
            base.Update();
            currentAngle += engine.Frame.DeltaTime * speed;
            currentAngle = ScalarMath.WrapAngle(currentAngle);
            Position = center + Vector2Geometry.AngleToVector(currentAngle, radius);
            if (wobble)
            {
                var sine = BumperFields.GetSine(this);
                Position += new Vector2(sine.Value * 3f, sine.ValueOverTwo * 2f);
            }
        }
    }
}
