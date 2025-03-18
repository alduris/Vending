namespace VendingMod
{
    internal static class PebbsiHooks
    {
        public static void Apply()
        {
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize;
            On.Player.SwallowObject += Player_SwallowObject;
            On.Player.Grabability += Player_Grabability;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;
            On.ScavengerAI.CollectScore_PhysicalObject_bool += ScavengerAI_CollectScore_PhysicalObject_bool;
            On.SLOracleBehaviorHasMark.TypeOfMiscItem += SLOracleBehaviorHasMark_TypeOfMiscItem;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
        }

        private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            orig(self);
            if (self.id == Conversation.ID.Moon_Misc_Item)
            {
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("It's a can of Five Pebbsi. It is a drink that was popular with our citizens."), 0));
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("I'm not sure if I would drink it if I were you, <PlayerName>. It's not good for you."), 0));
            }
        }

        private static SLOracleBehaviorHasMark.MiscItemType SLOracleBehaviorHasMark_TypeOfMiscItem(On.SLOracleBehaviorHasMark.orig_TypeOfMiscItem orig, SLOracleBehaviorHasMark self, PhysicalObject testItem)
        {
            if (testItem is FivePebbsi) return Enums.PebbsiDialogue;
            return orig(self, testItem);
        }

        private static int ScavengerAI_CollectScore_PhysicalObject_bool(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
        {
            var res = orig(self, obj, weaponFiltered);

            if (obj is FivePebbsi)
            {
                if (self.scavenger.room is Room rm)
                {
                    var ownedItemOnGround = rm.socialEventRecognizer.ItemOwnership(obj);
                    if (ownedItemOnGround is not null && ownedItemOnGround.offeredTo is not null && ownedItemOnGround.offeredTo != self.scavenger)
                        return 0;
                }
                if (!weaponFiltered || !self.NeedAWeapon)
                    return 2;
            }

            return res;
        }

        private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            return testObj is FivePebbsi || orig(self, testObj);
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            return obj is FivePebbsi ? Player.ObjectGrabability.OneHand : orig(self, obj);
        }

        private static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
        {
            orig(self, grasp);
            if (self.objectInStomach.type == Enums.FivePebbsi)
            {
                self.mushroomCounter += 500;
                self.Stun(30);
                self.AddFood(1);
                self.objectInStomach.Destroy();
                self.objectInStomach = null;
            }
        }

        private static void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            orig(self);
            if (self.realizedObject == null && self.type == Enums.FivePebbsi)
            {
                self.realizedObject = new FivePebbsi(self);
            }
        }
    }
}
