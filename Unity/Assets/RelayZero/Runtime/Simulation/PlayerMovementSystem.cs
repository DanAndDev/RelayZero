using RelayZero.Foundation;
using Unity.Mathematics;

namespace RelayZero.Simulation
{
    internal static class PlayerMovementSystem
    {
        public static float2 Step(
            ref PlayerState player,
            in InputCommand command,
            in PlayerMovementConfig config)
        {
            float maximumSpeed = player.IsCarrying
                ? config.CarrierMaximumSpeedMetersPerSecond
                : config.NormalMaximumSpeedMetersPerSecond;
            float2 desiredVelocity = command.Move * maximumSpeed;
            float rate = math.lengthsq(command.Move) > 0f
                ? config.AccelerationMetersPerSecondSquared
                : config.DecelerationMetersPerSecondSquared;
            float maximumDelta = rate / SimulationTime.TicksPerSecond;

            player.Velocity = MoveTowards(player.Velocity, desiredVelocity, maximumDelta);
            float speedSquared = math.lengthsq(player.Velocity);
            if (speedSquared > maximumSpeed * maximumSpeed)
            {
                player.Velocity *= maximumSpeed * math.rsqrt(speedSquared);
            }

            UpdateFacing(ref player, in command, in config);
            return player.Velocity / SimulationTime.TicksPerSecond;
        }

        private static void UpdateFacing(
            ref PlayerState player,
            in InputCommand command,
            in PlayerMovementConfig config)
        {
            float2 target = math.lengthsq(command.Aim) > 0.00000001f
                ? command.Aim
                : command.Move;
            float targetLengthSquared = math.lengthsq(target);
            if (targetLengthSquared <= 0.00000001f)
            {
                return;
            }

            target *= math.rsqrt(targetLengthSquared);
            float2 current = player.FacingDirection;
            float signedAngle = math.atan2(
                current.x * target.y - current.y * target.x,
                math.clamp(math.dot(current, target), -1f, 1f));
            float maximumTurn = config.TurnSpeedRadiansPerSecond / SimulationTime.TicksPerSecond;
            float turn = math.clamp(signedAngle, -maximumTurn, maximumTurn);
            float sine = math.sin(turn);
            float cosine = math.cos(turn);
            player.FacingDirection = math.normalizesafe(
                new float2(
                    current.x * cosine - current.y * sine,
                    current.x * sine + current.y * cosine),
                current);
        }

        private static float2 MoveTowards(float2 current, float2 target, float maximumDelta)
        {
            float2 difference = target - current;
            float distanceSquared = math.lengthsq(difference);
            if (distanceSquared == 0f || distanceSquared <= maximumDelta * maximumDelta)
            {
                return target;
            }

            return current + difference * (maximumDelta * math.rsqrt(distanceSquared));
        }
    }
}
