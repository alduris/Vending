using RWCustom;
using UnityEngine;

namespace VendingMod
{
    public class FivePebbsi : PlayerCarryableItem, IDrawable
    {
        public Color bodyColor;
        public Color logoColor;
        private Vector2 lastRotation;
        private Vector2 rotation;
        private Vector2? setRotation = null;
        private float darkness;

        public FivePebbsi(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            bodyChunks = [new BodyChunk(this, 0, new Vector2(0f, 0f), 8f, 0.2f)];
            bodyChunkConnections = [];
            airFriction = 0.999f;
            gravity = 0.9f;
            bounce = 0.2f;
            surfaceFriction = 0.7f;
            collisionLayer = 1;
            waterFriction = 0.95f;
            buoyancy = 1.1f;

            Random.State old = Random.state;
            Random.InitState(abstractPhysicalObject.ID.RandomSeed);
            switch (Random.value)
            {
                case < 0.2f:
                    // navy & white
                    bodyColor = new Color32(0x01, 0x35, 0x67, 0xff);
                    logoColor = Color.white;
                    break;
                case < 0.55f:
                    // red & white
                    bodyColor = new Color32(0xee, 0x39, 0x42, 0xff);
                    logoColor = Color.white;
                    break;
                case < 0.9f:
                    // blue & white
                    bodyColor = new Color32(0x00, 0x68, 0xa9, 0xff);
                    logoColor = Color.white;
                    break;
                default:
                    // white & gold
                    bodyColor = Color.white;
                    logoColor = RainWorld.SaturatedGold;
                    break;
            }
            Random.state = old;
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            firstChunk.HardSetPosition(placeRoom.MiddleOfTile(abstractPhysicalObject.pos));
            rotation = Custom.RNV();
            lastRotation = rotation;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (room.game.devToolsActive && Input.GetKey("b"))
            {
                firstChunk.vel += Custom.DirVec(firstChunk.pos, Futile.mousePosition) * 3f;
            }
            lastRotation = rotation;
            if (grabbedBy.Count > 0)
            {
                rotation = Custom.PerpendicularVector(Custom.DirVec(firstChunk.pos, grabbedBy[0].grabber.mainBodyChunk.pos));
                rotation.y = Mathf.Abs(rotation.y);
            }
            if (setRotation != null)
            {
                rotation = setRotation.Value;
                setRotation = null;
            }
            if (firstChunk.ContactPoint.y < 0)
            {
                rotation = (rotation - Custom.PerpendicularVector(rotation) * 0.1f * firstChunk.vel.x).normalized;
                firstChunk.vel.x *= 0.8f;
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = [
                new FSprite("pixel") { scaleX = 12f, scaleY = 18f },
                new FSprite("pixel") { scaleX = 12f, scaleY = 16f },
                new FSprite("FlowerMarker") { scale = 10f / 23f },
            ];
            AddToContainer(sLeaser, rCam, null);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer("Items");

            sLeaser.RemoveAllSpritesFromContainer();
            foreach (var sprite in sLeaser.sprites)
            {
                newContatiner.AddChild(sprite);
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            Vector2 rot = Vector3.Slerp(lastRotation, rotation, timeStacker);
            darkness = rCam.room.Darkness(pos) * (1f - rCam.room.LightSourceExposure(pos));
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            foreach (var sprite in sLeaser.sprites)
            {
                sprite.SetPosition(pos - camPos);
                sprite.rotation = Custom.VecToDeg(rot);
            }

            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            bool flag = blink > 0 && Random.value < 0.5f;
            sLeaser.sprites[0].color = flag ? blinkColor : palette.blackColor;
            sLeaser.sprites[1].color = flag ? blinkColor : Color.Lerp(bodyColor, palette.blackColor, Mathf.Pow(darkness, 1.5f));
            sLeaser.sprites[2].color = flag ? blinkColor : Color.Lerp(logoColor, palette.blackColor, Mathf.Pow(darkness, 1.5f));
        }
    }
}
