using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;

namespace VendingMod
{
    public static class Enums
    {
        public static AbstractObjectType FivePebbsi = null;

        public static SLOracleBehaviorHasMark.MiscItemType PebbsiDialogue = null;

        internal static void Register()
        {
            FivePebbsi ??= new(nameof(FivePebbsi), true);

            PebbsiDialogue ??= new(nameof(PebbsiDialogue), true);
        }

        internal static void Unregister()
        {
            FivePebbsi?.Unregister();
            FivePebbsi = null;
        }
    }
}
