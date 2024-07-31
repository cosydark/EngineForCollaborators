namespace Editor.MathGeoLib
{
#if UNITY || UNITY_EDITOR
#else

// ReSharper disable once CheckNamespace
namespace Editor.MathGeoLib
{
    [PublicAPI]
    [StructLayout(LayoutKind.Sequential)]
    public struct Plane
    {
        public readonly Vector3 Normal;

        public readonly float Distance;

        public Plane(Vector3 normal, float distance)
        {
            Normal = normal;
            Distance = distance;
        }

        public override string ToString()
        {
            return $"{nameof(Normal)}: {Normal}, {nameof(Distance)}: {Distance}";
        }
    }
}

#endif // !UNITY
}