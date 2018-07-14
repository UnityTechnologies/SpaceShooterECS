using Unity.Mathematics;

namespace UnityEngine.ECS.MathUtils
{
    class matrix_math_util
    {
        const float epsilon = 0.000001F;

        public static float3x3 identity3
        {
            get { return new float3x3(new float3(1, 0, 0), new float3(0, 1, 0), new float3(0, 0, 1)); }
        }
        public static float4x4 identity4
        {
            get { return new float4x4(new float4(1, 0, 0, 0), new float4(0, 1, 0, 0), new float4(0, 0, 1, 0), new float4(0, 0, 0, 1)); }
        }

        public static float4x4 LookRotationToMatrix(float3 position, float3 forward, float3 up)
        {
            float3x3 rot = LookRotationToMatrix(forward, up);

            float4x4 matrix = new float4x4
            {
                c0 = new float4(rot.c0, 0.0F),
                c1 = new float4(rot.c1, 0.0F),
                c2 = new float4(rot.c2, 0.0F),
                c3 = new float4(position, 1.0F),
            };

            return matrix;
        }

        public static float3x3 LookRotationToMatrix(float3 forward, float3 up)
        {
            float3 z = forward;
            // compute u0
            float mag = math.length(z);
            if (mag < epsilon)
                return identity3;
            z /= mag;

            float3 x = math.cross(up, z);
            mag = math.length(x);
            if (mag < epsilon)
                return identity3;
            x /= mag;

            float3 y = math.cross(z, x);
            float yLength = math.length(y);
            if (yLength < 0.9F || yLength > 1.1F)
                return identity3;

            return new float3x3(x, y, z);
        }
    }
}