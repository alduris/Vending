using System;
using System.Collections.Generic;
using System.Linq;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using AbstractObjectType = AbstractPhysicalObject.AbstractObjectType;
using DataPearlType = DataPearl.AbstractDataPearl.DataPearlType;
using MSCObjectType = MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType;
using Triangle = TriangleMesh.Triangle;

namespace VendingMod
{
    public class Vending : UpdatableAndDeletable, IDrawable, IProvideWarmth
    {
        public Vector2 pos;

        private int tradeCooldown = 0;

        private int lastPlayerNear = 0;
        private int playerNear = 0;
        private bool foundOffer = false;

        private readonly Product[][] products;
        private readonly int[] rows = [4, 4, 6, 6, 6, 5];

        // warmth stuff
        public Room loadedRoom => room;
        public float warmth => RainWorldGame.DefaultHeatSourceWarmth * 0.5f;
        public float range => 80f;
        public Vector2 Position() => pos;


        public Vending(Room room, Vector2 pos)
        {
            this.room = room;
            this.pos = pos;

            products = new Product[rows.Length][];
            for (int i = 0; i < rows.Length; i++)
            {
                products[i] = new Product[rows[i]];
                for (int j = 0; j < rows[i]; j++)
                {
                    products[i][j] = new Product(i, j, Mathf.Lerp(Random.Range(0.8f, 1f), 1f, Mathf.Sqrt(Random.value)), Custom.HSL2RGB(Random.value, Mathf.Pow(Random.value, 0.2f), 0.5f));
                }
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            bool foundPlayer = false;
            lastPlayerNear = playerNear;
            foundOffer = false;
            if (tradeCooldown > 0) tradeCooldown--;

            if (tradeCooldown == 0)
            {
                foreach (var player in room.game.Players)
                {
                    if (player?.realizedCreature?.room == room)
                    {
                        var p = player.realizedCreature as Player;
                        if (!p.dead && !p.Stunned && !p.isNPC && p.bodyMode == Player.BodyModeIndex.Stand && Vector2.Distance(p.mainBodyChunk.pos, pos) < 50f)
                        {
                            foundPlayer = true;
                            bool makingOffer = p.grasps.Any(x => ValueForItem(x?.grabbed) > 0);
                            foundOffer |= makingOffer;
                            for (int i = 0; i < 10; i++)
                            {
                                if (p.input[i].y <= 0 && p.input[i].x == 0 && !p.input[i].jmp && !p.input[i].pckp && !p.input[i].mp && !p.input[i].thrw) makingOffer = false;
                            }
                            if (makingOffer)
                            {
                                MakeOffer(p);
                            }
                        }
                    }
                }
            }
            if (foundPlayer)
            {
                playerNear++;
            }
            else if (playerNear > 0)
            {
                playerNear--;
            }
        }

        private int ValueForItem(PhysicalObject item)
        {
            return item switch
            {
                ExplosiveSpear or ElectricSpear { abstractSpear.electricCharge: > 0 } => 6,
                Spear { bugSpear: true } => 6,

                Centipede { Small: true, dead: true } => 3,
                SmallNeedleWorm { dead: true } => 3,
                VultureGrub { dead: true } => 3,
                Hazer { dead: true } => 3,

                SlimeMold { JellyfishMode: true } => 0,
                GlowWeed => 1,
                Mushroom or Fly or Rock => 2,
                FlyLure or FlareBomb or FireEgg or GooieDuck or FirecrackerPlant or JellyFish or SporePlant or PuffBall or Lantern => 3,
                Spear or LillyPuck or ScavengerBomb => 4,
                OverseerCarcass => 5,
                NeedleEgg => 6,
                OracleSwarmer => 7,
                KarmaFlower => 8,
                DataPearl => 8,
                VultureMask vm => vm.King ? 9 : 7,
                SingularityBomb => 10,
                Player { isNPC: true, dead: false } => 10,

                Creature { dead: true } c => Custom.IntClamp(c.abstractCreature.creatureTemplate.meatPoints, 4, 12),
                Weapon => 4,
                IPlayerEdible food => Math.Max(1, food.FoodPoints),
                _ => 0
            };
        }

        private void MakeOffer(Player player)
        {
            if (tradeCooldown > 0) return;

            // Find grasp and worth
            int offerGrasp = -1;
            int offerWorth = -1;
            for (int i = 0; i < player.grasps.Length; i++)
            {
                offerWorth = ValueForItem(player.grasps[i]?.grabbed);
                if (offerWorth > 0)
                {
                    offerGrasp = i;
                    break;
                }
            }

            if (offerGrasp != -1)
            {
                Plugin.Logger.LogDebug("Vendor trading for: " + offerWorth);

                // Random bonus
                offerWorth += Math.Max(0, Random.Range(-1, 2) + Random.Range(-1, 2) + Random.Range(-1, 2));
                Plugin.Logger.LogDebug("After applied bonus: " + offerWorth);

                // Figure out some worthwhile goodies
                PhysicalObject offeredItem = player.grasps[offerGrasp].grabbed;
                WorldCoordinate pos = room.GetWorldCoordinate(this.pos);
                EntityID ID = room.game.GetNewID();
                while (offerWorth > 0)
                {
                    int value = Custom.IntClamp(Mathf.CeilToInt(Mathf.Lerp(offerWorth / 2f, offerWorth, Random.value)), 1, 12);
                    offerWorth -= value;

                    AbstractPhysicalObject obj = null;

                    if (value >= 12)
                    {
                        // Potential offers
                        List<AbstractObjectType> offers = [];
                        if (offeredItem is not VultureMask) offers.Add(AbstractObjectType.VultureMask);
                        if (offeredItem is not KarmaFlower) offers.Add(AbstractObjectType.KarmaFlower);
                        if (offeredItem is not DataPearl) offers.Add(AbstractObjectType.DataPearl);
                        if (ModManager.MSC)
                        {
                            if (offeredItem is not Player) offers.Add(AbstractObjectType.Creature);
                            if (offeredItem is not SingularityBomb) offers.Add(MSCObjectType.SingularityBomb);
                            if (Random.value < 0.01f) offers.Add(MSCObjectType.JokeRifle);
                        }

                        // Pick one
                        AbstractObjectType pick = offers[Random.Range(0, offers.Count)];
                        if (pick == AbstractObjectType.VultureMask)
                            obj = new VultureMask.AbstractVultureMask(room.world, null, pos, ID, ID.RandomSeed, true);
                        else if (pick == AbstractObjectType.DataPearl)
                            obj = new DataPearl.AbstractDataPearl(room.world, pick, null, pos, ID, -1, -1, null, new DataPearlType(DataPearlType.values.entries[DataPearlType.values.entries.Count], false));
                        else if (pick == AbstractObjectType.Creature)
                            obj = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, pos, ID);
                        else if (pick == AbstractObjectType.KarmaFlower)
                            obj = new AbstractConsumable(room.world, pick, null, pos, ID, -1, -1, null);
                        else if (ModManager.MSC && pick == MSCObjectType.JokeRifle)
                            obj = new JokeRifle.AbstractRifle(room.world, null, pos, ID, JokeRifle.AbstractRifle.AmmoType.Rock);
                        else
                            obj = new AbstractPhysicalObject(room.world, pick, null, pos, ID);
                    }

                    else if (value >= 8)
                    {
                        // Potential offers
                        List<AbstractObjectType> offers = [];
                        if (offeredItem is not VultureMask) offers.Add(AbstractObjectType.VultureMask);
                        if (offeredItem is not KarmaFlower && Random.value < 0.3f) offers.Add(AbstractObjectType.KarmaFlower);
                        if (offeredItem is not DataPearl) offers.Add(AbstractObjectType.DataPearl);
                        if (offeredItem is not OracleSwarmer) offers.Add(AbstractObjectType.SSOracleSwarmer);
                        if (ModManager.MSC)
                        {
                            if (offeredItem is not SingularityBomb && Random.value < 0.15f) offers.Add(MSCObjectType.SingularityBomb);
                            if (offeredItem is not ElectricSpear { abstractSpear.electricCharge: > 0 }) offers.Add(AbstractObjectType.Spear);
                        }

                        // Pick one
                        AbstractObjectType pick = offers[Random.Range(0, offers.Count)];
                        if (pick == AbstractObjectType.VultureMask)
                            obj = new VultureMask.AbstractVultureMask(room.world, null, pos, ID, ID.RandomSeed, Random.value < 0.2f);
                        else if (pick == AbstractObjectType.DataPearl)
                            obj = new DataPearl.AbstractDataPearl(room.world, pick, null, pos, ID, -1, -1, null, DataPearlType.Misc);
                        else if (pick == AbstractObjectType.KarmaFlower)
                            obj = new AbstractConsumable(room.world, pick, null, pos, ID, -1, -1, null);
                        else if (pick == AbstractObjectType.Spear)
                            obj = new AbstractSpear(room.world, null, pos, ID, false, true) { electricCharge = 10 };
                        else
                            obj = new AbstractPhysicalObject(room.world, pick, null, pos, ID);
                    }

                    else if (value >= 5)
                    {
                        // Potential offers
                        List<AbstractObjectType> offers = [];
                        if (offeredItem is not VultureMask && Random.value < 0.4f) offers.Add(AbstractObjectType.VultureMask);
                        if (offeredItem is not DataPearl) offers.Add(AbstractObjectType.DataPearl);
                        if (offeredItem is not Spear { abstractSpear.explosive: true } and not Spear { abstractSpear.electric: true }) offers.Add(AbstractObjectType.Spear);
                        if (offeredItem is not ScavengerBomb) offers.Add(AbstractObjectType.ScavengerBomb);
                        if (offeredItem is not SporePlant) offers.Add(AbstractObjectType.SporePlant);
                        if (offeredItem is not PuffBall) offers.Add(AbstractObjectType.PuffBall);
                        if (offeredItem is not FlareBomb) offers.Add(AbstractObjectType.FlareBomb);
                        if (offeredItem is not FlyLure && Random.value < 0.6f) offers.Add(AbstractObjectType.FlyLure);
                        if (offeredItem is not FirecrackerPlant) offers.Add(AbstractObjectType.FirecrackerPlant);
                        if (offeredItem is not NeedleEgg && Random.value < 0.4f) offers.Add(AbstractObjectType.NeedleEgg);
                        if (offeredItem is not Creature && Random.value < 0.8f) offers.Add(AbstractObjectType.Creature);

                        if (ModManager.MSC)
                        {
                            if (offeredItem is not FireEgg && Random.value < 0.2f) offers.Add(MSCObjectType.FireEgg);
                        }

                        // Pick one
                        AbstractObjectType pick = offers[Random.Range(0, offers.Count)];
                        if (pick == AbstractObjectType.Creature)
                        {
                            List<CreatureTemplate.Type> critters = [CreatureTemplate.Type.Snail, CreatureTemplate.Type.TubeWorm, CreatureTemplate.Type.TubeWorm];
                            if (ModManager.MSC) critters.Add(MoreSlugcatsEnums.CreatureTemplateType.Yeek);
                            CreatureTemplate.Type critter = RandomFrom(critters.ToArray());
                            obj = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(critter), null, pos, ID);
                        }
                        else if (pick == AbstractObjectType.VultureMask)
                            obj = new VultureMask.AbstractVultureMask(room.world, null, pos, ID, ID.RandomSeed, false);
                        else if (pick == AbstractObjectType.DataPearl)
                            obj = new DataPearl.AbstractDataPearl(room.world, pick, null, pos, ID, -1, -1, null, DataPearlType.Misc);
                        else if (pick == AbstractObjectType.Spear)
                            obj = new AbstractSpear(room.world, null, pos, ID, Random.value < 0.5f);
                        else if (AbstractConsumable.IsTypeConsumable(pick))
                            obj = new AbstractConsumable(room.world, pick, null, pos, ID, -1, -1, null);
                        else
                            obj = new AbstractPhysicalObject(room.world, pick, null, pos, ID);
                    }

                    else
                    {
                        // Potential offers
                        bool seed = ModManager.MSC && offeredItem.abstractPhysicalObject.type == MSCObjectType.Seed;
                        List<AbstractObjectType> offers = [];
                        if (offeredItem is not Spear) offers.Add(AbstractObjectType.Spear);
                        if (offeredItem is not ScavengerBomb) offers.Add(AbstractObjectType.ScavengerBomb);
                        if (offeredItem is not Rock) offers.Add(AbstractObjectType.Rock);
                        if (offeredItem is not DangleFruit) offers.Add(AbstractObjectType.DangleFruit);
                        if (offeredItem is not SlimeMold || seed) offers.Add(AbstractObjectType.SlimeMold);
                        if (offeredItem is not Mushroom) offers.Add(AbstractObjectType.Mushroom);
                        if (offeredItem is not WaterNut and not SwollenWaterNut) offers.Add(AbstractObjectType.WaterNut);
                        if (offeredItem is not FlyLure) offers.Add(AbstractObjectType.FlyLure);
                        if (offeredItem is not FlareBomb) offers.Add(AbstractObjectType.FlareBomb);
                        if (offeredItem is not FirecrackerPlant) offers.Add(AbstractObjectType.FirecrackerPlant);
                        if (offeredItem is not JellyFish) offers.Add(AbstractObjectType.JellyFish);
                        if (offeredItem is not Creature) offers.Add(AbstractObjectType.Creature);

                        if (ModManager.MSC)
                        {
                            if (offeredItem is not GooieDuck) offers.Add(MSCObjectType.GooieDuck);
                            if (offeredItem is not LillyPuck) offers.Add(MSCObjectType.LillyPuck);
                            if (offeredItem is not GlowWeed) offers.Add(MSCObjectType.GlowWeed);
                            if (offeredItem is not DandelionPeach) offers.Add(MSCObjectType.DandelionPeach);
                            if (offeredItem is not SlimeMold || !seed) offers.Add(MSCObjectType.Seed);
                        }

                        // Pick one
                        AbstractObjectType pick = offers[Random.Range(0, offers.Count)];
                        if (pick == AbstractObjectType.Creature)
                        {
                            CreatureTemplate.Type critter = RandomFrom(
                                CreatureTemplate.Type.Snail,
                                CreatureTemplate.Type.VultureGrub, CreatureTemplate.Type.Hazer,
                                CreatureTemplate.Type.TubeWorm, CreatureTemplate.Type.TubeWorm
                                );
                            obj = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(critter), null, pos, ID);
                        }
                        else if (pick == AbstractObjectType.Spear)
                            obj = new AbstractSpear(room.world, null, pos, ID, Random.value < 0.05f);
                        else if (ModManager.MSC && pick == MSCObjectType.LillyPuck)
                            obj = new LillyPuck.AbstractLillyPuck(room.world, null, pos, ID, 3, -1, -1, null);
                        else if (AbstractConsumable.IsTypeConsumable(pick))
                            obj = new AbstractConsumable(room.world, pick, null, pos, ID, -1, -1, null);
                        else
                            obj = new AbstractPhysicalObject(room.world, pick, null, pos, ID);
                    }

                    if (obj != null)
                    {
                        obj.Move(room.GetWorldCoordinate(this.pos));
                        obj.RealizeInRoom();
                    }
                }

                // Kill the grabbed item
                player.ReleaseGrasp(offerGrasp);
                offeredItem.Destroy();
                offeredItem.abstractPhysicalObject.Destroy();

                tradeCooldown = 40 * 5;
            }

            static T RandomFrom<T>(params T[] list) => list[Random.Range(0, list.Length)];
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            List<FSprite> sprites = [
                new FSprite("pixel") { scaleX = 40f, scaleY = 80f },
                new FSprite("pixel") { scaleX = 28f, scaleY = 58f },
                new FSprite("TinyGlyph0"),
                new FSprite("TinyGlyph1"),
                new FSprite("TinyGlyph2"),
                new FSprite("TinyGlyph3"),
                new FSprite("TinyGlyph4"),
                new FSprite("TinyGlyph5"),
            ];
            for (int i = 0; i < products.Length; i++)
            {
                for (int j = 0; j < products[i].Length; j++)
                {
                    sprites.Add(new FSprite("pixel") { scaleX = (28f - 2f * rows[i]) / rows[i], scaleY = (56f - 3f * (rows.Length - 1)) / rows.Length * products[i][j].height });
                }
            }
            sLeaser.sprites = [.. sprites];
            AddToContainer(sLeaser, rCam, null);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer("Background");
            sLeaser.RemoveAllSpritesFromContainer();
            foreach (var sprite in sLeaser.sprites)
            {
                newContatiner.AddChild(sprite);
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[0].SetPosition(pos - camPos);
            sLeaser.sprites[1].SetPosition(pos + new Vector2(-4f, 9f) - camPos);
            for (int i = 2; i < 8; i++)
            {
                sLeaser.sprites[i].SetPosition(pos + new Vector2(15f, 45f - 10f * i) - camPos);
            }

            float rowHeight = (sLeaser.sprites[1].scaleY - 2f * (rows.Length + 1)) / rows.Length + 2f;
            for (int i = 0, k = 0; i < products.Length; i++)
            {
                float colWidth = (sLeaser.sprites[1].scaleX - 2f * rows[i]) / rows[i] + 2f;
                for (int j = 0; j < products[i].Length; j++, k++)
                {
                    sLeaser.sprites[8 + k].SetPosition(pos + new Vector2(-18f, 38f) + new Vector2(colWidth * (j + 0.5f), -rowHeight * (1 + i - products[i][j].height / 2f)) - camPos);
                }
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            Random.State old = Random.state;
            Random.InitState((int)(pos.x + pos.y) + room.abstractRoom.index);
            sLeaser.sprites[0].color = Color.Lerp(new HSLColor(Random.Range(0.4f, 0.6f), Random.Range(0f, 0.1f), Random.Range(0.05f, 0.2f)).rgb, palette.blackColor, room.Darkness(pos) * 0.75f);
            sLeaser.sprites[1].color = new Color(0.01f, 0.01f, 0.01f);
            for (int i = 2; i < 8; i++)
            {
                sLeaser.sprites[i].color = Color.Lerp(Color.white, palette.fogColor, 0.25f);
            }
            for (int i = 0, k = 0; i < products.Length; i++)
            {
                for (int j = 0; j < products[i].Length; j++, k++)
                {
                    sLeaser.sprites[8 + k].color = Color.Lerp(products[i][j].color, palette.blackColor, room.Darkness(pos) * 0.5f);
                }
            }
            Random.state = old;
        }

        private struct Product(int row, int col, float height, Color color)
        {
            public int row = row;
            public int col = col;
            public float height = height;
            public Color color = color;
        }
    }
}
