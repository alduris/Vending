using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;

namespace VendingMod
{
    public static class Enums
    {
        public static AbstractObjectType FivePebbsi = null;

        internal static void Register()
        {
            FivePebbsi ??= new(nameof(FivePebbsi), true);
        }

        internal static void Unregister()
        {
            FivePebbsi?.Unregister();
            FivePebbsi = null;
        }
    }
}
