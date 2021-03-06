﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RWCustom;
using UnityEngine;

public class Preciptator : UpdatableAndDeletable
{
    public Player player;
    public bool isSnow;
    public Room currentRoom;
    public int rainDrops;
    public int snowFlakes;
    public float rainAmount;
    public Vector2 spawn;
    public FloatRect roomBounds;
    public int rainLimit;
    public float ratio;
    public int ceilingCount;
    public float direction;
    public bool spawnDecals;
    public List<Vector2> skyreach;
    public int[] rainReach;
    public Vector2 camPos;
    public List<Vector2> camSkyreach;

    public Preciptator(Room room, bool isSnow)
    {
        this.skyreach = new List<Vector2>();
        this.camSkyreach = new List<Vector2>();
        this.spawnDecals = false;
        this.roomBounds = room.RoomRect;
        this.rainDrops = 0;
        this.snowFlakes = 0;
        this.room = room;
        this.isSnow = isSnow;
        this.ceilingCount = 0;
        this.direction = RainFall.direction;
        this.room.AddObject(new SnowDecal(this.room));
        for (int r = 0; r < this.room.TileWidth; r++)
        {
            if (this.room.Tiles[r, this.room.TileHeight - 1].Solid)
            {
                ceilingCount++;
            }
        }
        Debug.Log("Ceiling Count: " + ceilingCount);
        Debug.Log(this.room.Width);
        Debug.Log(this.room.Width * 0.95);
        Debug.Log((float)this.room.Width * 0.95);

        if (ceilingCount < (this.room.Width * 0.95))
        {
            foreach (Room.Tile tile in this.room.Tiles)
            {
                if ((tile.Solid && this.room.GetTile(tile.X, tile.Y + 1).Terrain == Room.Tile.TerrainType.Air && this.room.GetTile(tile.X, tile.Y + 2).Terrain == Room.Tile.TerrainType.Air) ||
                    tile.Terrain == Room.Tile.TerrainType.Slope && this.room.GetTile(tile.X, tile.Y + 1).Terrain == Room.Tile.TerrainType.Air && this.room.GetTile(tile.X, tile.Y + 2).Terrain == Room.Tile.TerrainType.Air)
                {
                    this.skyreach.Add(this.room.MiddleOfTile(tile.X, tile.Y - 1));
                    //Add snow decals to surfaces
                    if (this.isSnow && Downpour.decals)
                    {
                        if (UnityEngine.Random.value > 0.8f)
                        {
                            this.room.AddObject(new SnowPile(this.room.MiddleOfTile(tile.X, tile.Y - 1), UnityEngine.Random.Range(60f, 80f)));
                        }
                        this.room.AddObject(new SnowPile(this.room.MiddleOfTile(tile.X, tile.Y), UnityEngine.Random.Range(20f, 45f)));
                    }
                }
            }
        }
    }

    public void AddRaindrops(int rainDropsToSpawn)
    {
        if (room != null && this.skyreach != null)
        {
            for (int i = 0; i < rainDropsToSpawn; i++)
            {
                Vector2 rng = this.skyreach[UnityEngine.Random.Range(0, this.skyreach.Count)];
                RainDrop rainDrop = new RainDrop(rng, Color.Lerp(room.game.cameras[0].currentPalette.skyColor, new Color(1f, 1f, 1f), 0.1f), RainFall.rainIntensity, this);
                this.room.AddObject(rainDrop);
                this.rainDrops++;
            }
        }
    }

    public void AddSnowflakes(int snowFlakesToSpawn)
    {
        if (room != null && room.BeingViewed)
        {
            if (this.camPos != null)
            {
                Debug.Log("CAMERA POSITION: " + this.room.game.cameras[0].pos);
                for (int i = 0; i < snowFlakesToSpawn; i++)
                {
                    //Get a random position within range of the RoomCamera
                    Vector2 cam = this.camPos;
                    IntVector2 randomOffset = IntVector2.FromVector2(new Vector2(cam.x + UnityEngine.Random.Range(-700, 700), cam.y + UnityEngine.Random.Range(-500, 500)));
                    Vector2 offset2 = new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f));
                    //If that random position has line of sight with the sky, spawn a snowflake there
                    if (!this.room.RayTraceTilesForTerrain(randomOffset.x, randomOffset.y, randomOffset.x, this.room.TileHeight - 1))
                    {
                        Vector2 spawn = randomOffset.ToVector2();
                        Vector2 spawnPos = spawn + offset2;
                        SnowFlake snowFlake = new SnowFlake(spawnPos, Color.Lerp(room.game.cameras[0].currentPalette.skyColor, new Color(1f, 1f, 1f), 0.1f), RainFall.rainIntensity, this);
                        this.room.AddObject(snowFlake);
                        this.snowFlakes++;
                    }
                }
            }
        }
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.room.BeingViewed)
        {
           this.camPos = this.room.game.cameras[0].pos + new Vector2(this.room.game.rainWorld.screenSize.x / 2, this.room.game.rainWorld.screenSize.y / 2);
        }
        this.isSnow = Downpour.snow;
        if (isSnow)
        {
            this.rainAmount = Mathf.Lerp(Downpour.rainAmount * 0.5f, Downpour.rainAmount, RainFall.rainIntensity);
            this.rainLimit = (int)Mathf.Lerp(this.rainAmount * 30, (this.rainAmount * 40), RainFall.rainIntensity);
        }
        else
        {
            this.rainAmount = Mathf.Lerp(0, Downpour.rainAmount, RainFall.rainIntensity);
            this.rainLimit = (int)Mathf.Lerp(0, (this.rainAmount * 10), RainFall.rainIntensity);
        }
        this.player = (room.game.Players.Count <= 0) ? null : (room.game.Players[0].realizedCreature as Player);

        if (this.room.game != null && this.room != null && !room.abstractRoom.gate && this.room.ReadyForPlayer)
        {
            if (!isSnow)
            {
                this.snowFlakes = 0;
                if (this.rainDrops < ((this.room.Width - this.ceilingCount) * this.rainLimit) / this.room.Width)
                {
                    this.AddRaindrops(rainLimit - this.rainDrops);
                }
            }
            else
            {
                this.rainDrops = 0;
                if (this.snowFlakes < ((this.room.Width - this.ceilingCount) * this.rainLimit) / this.room.Width)
                {
                    this.AddSnowflakes(rainLimit - this.snowFlakes);
                }
            }
        }
        if (Downpour.snow && Downpour.dust && RainFall.rainList.Contains(this.room.abstractRoom.name))
        {
            for (int i = 0; i < this.room.game.Players.Count; i++)
            {
                if (this.room.game.Players[i].realizedCreature != null && this.room.game.Players[i].realizedCreature.room == this.room)
                {
                    for (int j = 0; j < this.room.game.Players[i].realizedCreature.bodyChunks.Length; j++)
                    {
                        if (this.room.game.Players[i].realizedCreature.bodyChunks[j].ContactPoint.y < 0)
                        {
                            if (this.room.game.Players[i].realizedCreature.bodyChunks[j].lastContactPoint.y >= 0 && this.room.game.Players[i].realizedCreature.bodyChunks[j].lastPos.y - this.room.game.Players[i].realizedCreature.bodyChunks[j].pos.y > 5f)
                            {
                                this.room.AddObject(new SnowDust(this.room.game.Players[i].realizedCreature.bodyChunks[j].pos + new Vector2(0f, -this.room.game.Players[i].realizedCreature.bodyChunks[j].rad), Custom.LerpMap(this.room.game.Players[i].realizedCreature.bodyChunks[j].lastPos.y - this.room.game.Players[i].realizedCreature.bodyChunks[j].pos.y, 5f, 10f, 0.5f, 1f)));
                            }
                            else if (UnityEngine.Random.value < 0.1f && Mathf.Abs(this.room.game.Players[i].realizedCreature.bodyChunks[j].lastPos.x - this.room.game.Players[i].realizedCreature.bodyChunks[j].pos.x) > 3f)
                            {
                                this.room.AddObject(new SnowDust(this.room.game.Players[i].realizedCreature.bodyChunks[j].pos + new Vector2(0f, -this.room.game.Players[i].realizedCreature.bodyChunks[j].rad), 0.25f * UnityEngine.Random.value));
                            }
                        }
                    }
                }
            }
        }
    }
}
public class SnowFlake : CosmeticSprite
{
    public Vector2 lastLastLastPos;
    public Vector2 lastLastPos;
    public Color color;
    public float gravity;
    public bool foreground;
    public float screenXPos;
    public float splashCounter;
    public Vector2 dir;
    public bool invert;
    public float dirCounter;
    public bool backgroundDrop;
    public float depth;
    public bool collision;
    public bool reset;
    public Player player;
    public Vector2 resetPos;
    public Preciptator spawner;
    public Vector2 offset;
    public bool randomOffset;
    public bool screenReset;
    public Vector2 shortcutPos;
    public Vector2 currentCamPos;
    public Vector2 defaultPos;
    public float directionAdjust;

    public SnowFlake(Vector2 pos, Color color, float rainIntensity, Preciptator spawner)
    {
        this.screenReset = false;
        this.foreground = false;
        this.splashCounter = 0;
        this.collision = false;
        this.spawner = spawner;
        if (Downpour.rainbow)
        {
            this.color = Custom.HSL2RGB(UnityEngine.Random.Range(0f, 1f), 0.5f, 0.5f);
        }
        else
        {
            this.color = Color.Lerp(color, new Color(1f, 1f, 1f), 0.7f);
        }
        this.defaultPos = pos;
        this.resetPos = new Vector2(pos.x, spawner.room.RoomRect.top + UnityEngine.Random.Range(130f,1700f));
        this.pos = pos;
        this.lastPos = this.pos;
        this.lastLastPos = this.pos;
        this.lastLastLastPos = this.pos;
        this.dir = (new Vector2(UnityEngine.Random.Range(-2f, 0.1f), -5f) * rainIntensity);
        this.gravity = Mathf.Lerp(0.4f, 0.9f, UnityEngine.Random.value);
        this.pos += vel * (40f * rainIntensity);
        this.dir = new Vector2(-this.dir.x + (directionAdjust * RainFall.rainIntensity), this.dir.y);
        this.vel = this.dir;
        this.pos += vel * (4f * rainIntensity);
        dirCounter = UnityEngine.Random.Range(2f, 10f);
        switch (spawner.direction)
        {
            case 1:
                directionAdjust = -1.2f;
                break;
            case 2:
                directionAdjust = 0f;
                break;
            case 3:
                directionAdjust = 1.2f;
                break;
        }
    }
    public override void Update(bool eu)
    {
        if (!Downpour.snow)
        {
            this.Destroy();
        }
        if (!this.room.BeingViewed)
        {
            return;
        }
        this.player = (room.game.Players.Count <= 0) ? null : (room.game.Players[0].realizedCreature as Player);
        if (reset && player != null && player.mainBodyChunk != null && !player.inShortcut && this.room.BeingViewed)
        {
            this.resetPos = new Vector2(player.mainBodyChunk.pos.x + UnityEngine.Random.Range(-1250, 1250), this.spawner.camPos.y + UnityEngine.Random.Range(300,1500));
        }
        if (this.reset)
        {
            if (UnityEngine.Random.value < 0.15f && this.room.world.rainCycle.RainDarkPalette > 0)
            {
                this.Destroy();
            }
            this.dir = (new Vector2(UnityEngine.Random.Range(-4f, 4f) + (directionAdjust * RainFall.rainIntensity), -5f) * RainFall.rainIntensity);
            this.vel = this.dir;
            this.pos = this.resetPos;
            this.lastPos = this.resetPos;
            this.lastLastPos = this.resetPos;
            this.lastLastLastPos = this.resetPos;
            this.collision = false;
            this.foreground = false;
            this.reset = false;
        }
        this.lastLastLastPos = this.lastLastPos;
        this.lastLastPos = this.lastPos;
        this.vel.x = this.vel.x * (1f + (RainFall.rainIntensity * 0.005f));
        this.vel.y = this.vel.y - (this.gravity * RainFall.rainIntensity) * 0.1f;
        if (this.vel.y < Mathf.Lerp((-7f * RainFall.rainIntensity), -15f, this.room.world.rainCycle.RainDarkPalette))
        {
            this.vel.y = Mathf.Lerp((-7f * RainFall.rainIntensity), -15f, this.room.world.rainCycle.RainDarkPalette) + (1f * UnityEngine.Random.value);
        }
        if (spawner.direction == 1)
        {
            if ((vel.x > 1f * RainFall.rainIntensity || vel.x < -4f * RainFall.rainIntensity) && dirCounter <= 0f)
            {
                if(vel.x > 1f)
                {
                    vel.x = vel.x * 0.03f;
                }
                this.dir = new Vector2(-this.dir.x + (directionAdjust * RainFall.rainIntensity), this.dir.y);
                dirCounter = UnityEngine.Random.Range(2f, 10f);
            }
        }
        else if (spawner.direction == 2)
        {
            if ((vel.x > 4f * RainFall.rainIntensity || vel.x < -4f * RainFall.rainIntensity) && dirCounter <= 0f)
            {
                this.dir = new Vector2(-this.dir.x, this.dir.y);
                dirCounter = UnityEngine.Random.Range(2f, 10f);
            }
        }
        else
        {
            if ((vel.x > 4f * RainFall.rainIntensity || vel.x < -1f * RainFall.rainIntensity) && dirCounter <= 0f)
            {
                if (vel.x < -1f)
                {
                    vel.x = vel.x * 0.03f;
                }
                this.dir = new Vector2(-this.dir.x + (directionAdjust * RainFall.rainIntensity), this.dir.y);
                dirCounter = UnityEngine.Random.Range(2f, 10f);
            }
        }
        this.vel += this.dir * 0.02f;
        this.dirCounter = this.dirCounter - (0.1f + (RainFall.rainIntensity * UnityEngine.Random.value));
        if (dirCounter < 0f)
        {
            dirCounter = 0;
        }
        if (this.room.fullyLoaded && this.room.BeingViewed)
        {
            if ((this.room.GetTile(this.pos).Terrain == Room.Tile.TerrainType.Solid || this.room.GetTile(this.pos).Solid || this.room.GetTile(this.pos).AnyWater))
            {
                this.reset = true;
            }
        }
        base.Update(eu);
    }
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        if (UnityEngine.Random.Range(0f, 1f) > 0.6f)
        {
            sLeaser.sprites[0] = new FSprite("SkyDandelion", true);
            sLeaser.sprites[0].scale = 0.15f + (RainFall.rainIntensity * 0.2f);
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["CustomDepth"];
        }
        else
        {
            sLeaser.sprites[0] = new FSprite("deerEyeB", true);
            sLeaser.sprites[0].scale = 0.33f + (RainFall.rainIntensity * 0.2f);
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["CustomDepth"];
        }
        this.AddToContainer(sLeaser, rCam, null);
    }
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (reset)
        {
            sLeaser.sprites[0].alpha = UnityEngine.Random.Range(0.85f, 1f);
        }
        sLeaser.sprites[0].x = Mathf.Lerp(this.lastPos.x, this.pos.x, timeStacker) - camPos.x;
        sLeaser.sprites[0].y = Mathf.Lerp(this.lastPos.y, this.pos.y, timeStacker) - camPos.y;
        sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(Vector2.Lerp(this.lastLastPos, this.lastPos, timeStacker), Vector2.Lerp(this.lastPos, this.pos, timeStacker));
        sLeaser.sprites[0].scaleY = Mathf.Max(0.45f, 0.45f + 0.1f * Vector2.Distance(Vector2.Lerp(this.lastLastPos, this.lastPos, timeStacker), Vector2.Lerp(this.lastPos, this.pos, timeStacker)));
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        if (palette.darkness > 0.5f)
        {
            sLeaser.sprites[0].color = Color.Lerp(palette.skyColor, new Color(0.2f, 0.2f, 0.2f), 0.4f);
        }
        else
        {
            sLeaser.sprites[0].color = Color.Lerp(palette.texture.GetPixel(9, 5), Color.white, 0.32f);
        }
        base.ApplyPalette(sLeaser, rCam, palette);
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        base.AddToContainer(sLeaser, rCam, newContatiner);
    }
}

//Snow Decal (End of Cycle)
public class SnowDecal : CosmeticSprite
{
    public SnowDecal(Room room)
    {
        this.room = room;
    }
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = new FSprite("Futile_White", true);
        sLeaser.sprites[0].alpha = 0f;
        sLeaser.sprites[0].color = Color.white;
        sLeaser.sprites[0].x = rCam.game.rainWorld.screenSize.x / 2f;
        sLeaser.sprites[0].y = rCam.game.rainWorld.screenSize.y / 2f;
        sLeaser.sprites[0].scaleX = rCam.game.rainWorld.screenSize.x;
        sLeaser.sprites[0].scaleY = rCam.game.rainWorld.screenSize.y;
        this.AddToContainer(sLeaser, rCam, null);
    }
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        sLeaser.sprites[0].x = this.pos.x - camPos.x;
        sLeaser.sprites[0].y = this.pos.y - camPos.y;
        sLeaser.sprites[0].alpha = Mathf.Lerp(0f, 0.7f, this.room.world.rainCycle.RainDarkPalette);
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }
    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        if (palette.darkness > 0.5f)
        {
            sLeaser.sprites[0].color = Color.Lerp(palette.skyColor, new Color(0.2f, 0.2f, 0.2f), 0.4f);
        }
        else
        {
            sLeaser.sprites[0].color = Color.Lerp(palette.texture.GetPixel(9, 5), Color.white, 0.5f);
        }
    }
    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        if (newContatiner == null)
        {
            newContatiner = rCam.ReturnFContainer("HUD2");
        }
        base.AddToContainer(sLeaser, rCam, newContatiner);
    }
    public float rad;
    public float flipX;
    public float flipY;
    public float rotat;
    public bool bigSprite;
    public float fade;
}

//Snowdust
public class SnowDust : CosmeticSprite
{
    public SnowDust(Vector2 pos, float size)
    {
        this.pos = pos;
        this.lastPos = pos;
        this.size = size;
        this.lastLife = 1f;
        this.life = 1f;
        this.lifeTime = Mathf.Lerp(40f, 120f, UnityEngine.Random.value) * Mathf.Lerp(0.5f, 1.5f, size);
    }
    public override void Update(bool eu)
    {
        base.Update(eu);
        this.pos.y = this.pos.y + 0.5f;
        this.pos.x = this.pos.x + 0.25f;
        this.lastLife = this.life;
        this.life -= 1f / this.lifeTime;
        if (this.lastLife < 0f)
        {
            this.Destroy();
        }
    }
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = new FSprite("Futile_White", true);
        sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Spores"];
        sLeaser.sprites[0].color = new Color(1f, 1f, 1f);
        this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Background"));
        base.InitiateSprites(sLeaser, rCam);
    }
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        sLeaser.sprites[0].x = Mathf.Lerp(this.lastPos.x, this.pos.x, timeStacker) - camPos.x;
        sLeaser.sprites[0].y = Mathf.Lerp(this.lastPos.y, this.pos.y, timeStacker) - camPos.y;
        sLeaser.sprites[0].scale = 10f * Mathf.Pow(1f - Mathf.Lerp(this.lastLife, this.life, timeStacker), 0.35f) * Mathf.Lerp(0.5f, 1.5f, this.size);
        sLeaser.sprites[0].alpha = Mathf.Lerp(this.lastLife, this.life, timeStacker);
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }
    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        if (palette.darkness > 0.5f)
        {
            sLeaser.sprites[0].color = Color.Lerp(palette.skyColor, new Color(0.2f, 0.2f, 0.2f), 0.4f);
        }
        else
        {
            sLeaser.sprites[0].color = Color.Lerp(palette.texture.GetPixel(9, 5), Color.white, 0.12f);
        }
        base.ApplyPalette(sLeaser, rCam, palette);
    }
    public float life;
    public float lastLife;
    public float lifeTime;
    public float size;
}

//Raindrops
public class RainDrop : CosmeticSprite
{
    public Vector2 lastLastLastPos;
    public Vector2 lastLastPos;
    public Color color;
    public float gravity;
    public float splashCounter;
    public bool timeToDie;
    public Vector2 dir;
    public float dirCounter;
    public bool backgroundDrop;
    public float depth;
    public bool collision;
    public Preciptator spawner;
    public bool reset;
    public Vector2 resetPos;
    public float alpha;
    public Player player;

    public RainDrop(Vector2 pos, Color color, float rainIntensity, Preciptator spawner)
    {
        this.alpha = UnityEngine.Random.Range(0.9f, 1f);
        this.spawner = spawner;
        this.timeToDie = false;
        this.splashCounter = 0;
        this.collision = false;
        //Small chance for any raindrop to be a background drop, assign it a random depth value
        if (UnityEngine.Random.value > 0.8f)
        {
            backgroundDrop = true;
            this.depth = UnityEngine.Random.value;
        }
        else
        {
            backgroundDrop = false;
            this.depth = UnityEngine.Random.Range(0.7f, 1f);
        }
        if (Downpour.rainbow)
        {
            this.color = Custom.HSL2RGB(UnityEngine.Random.value, 0.5f, 0.5f);
        }
        else
        {
            this.color = color;
        }
        this.resetPos = new Vector2(pos.x, spawner.room.RoomRect.top + 150f);
        this.pos = new Vector2(pos.x, UnityEngine.Random.Range(pos.y, spawner.room.RoomRect.top + 150f));
        this.lastPos = this.pos;
        this.lastLastPos = this.pos;
        this.lastLastLastPos = this.pos;
        //Vary starting velocity
        this.vel.y = UnityEngine.Random.Range(-10f * rainIntensity, -20f * rainIntensity);
        //Increase spread of raindrops as rain intensity increases
        this.vel.x = Mathf.Lerp(UnityEngine.Random.Range(-4, 3f), UnityEngine.Random.Range(-12f, 3f), RainFall.rainIntensity);
        this.gravity = Mathf.Lerp(0.8f, 1f, UnityEngine.Random.value);
    }
    public override void Update(bool eu)
    {
        this.player = (room.game.Players.Count <= 0) ? null : (room.game.Players[0].realizedCreature as Player);
        if (this.reset)
        {
            float rng = UnityEngine.Random.value;
            if (rng < 0.05f && this.room.world.rainCycle.RainDarkPalette > 0)
            {
                this.Destroy();
            }
            if (Downpour.snow)
            {
                this.Destroy();
            }
            if (player != null && player.mainBodyChunk != null && !player.inShortcut)
            {
                this.resetPos = new Vector2(UnityEngine.Random.Range(player.mainBodyChunk.pos.x - 1300f, player.mainBodyChunk.pos.x + 1300f), room.RoomRect.top + UnityEngine.Random.Range(100, 200f));
            }
            else
            {
                this.resetPos = new Vector2(UnityEngine.Random.Range(this.room.RoomRect.left - 100f, this.room.RoomRect.right + 100f), room.RoomRect.top + UnityEngine.Random.Range(100, 200f));
            }
            this.pos = this.resetPos;
            this.lastPos = this.resetPos;
            this.lastLastPos = this.resetPos;
            this.lastLastLastPos = this.resetPos;
            this.collision = false;
            this.timeToDie = false;
            if (this.spawner.direction == 1)
            {
                this.vel.x = Mathf.Lerp(UnityEngine.Random.Range(5f, -2f), UnityEngine.Random.Range(-15f, -2f), RainFall.rainIntensity);
            }
            else if (this.spawner.direction == 2)
            {
                this.vel.x = Mathf.Lerp(UnityEngine.Random.Range(2f, -2f), UnityEngine.Random.Range(-7f, 7f), RainFall.rainIntensity);
            }
            else if (this.spawner.direction == 3)
            {
                this.vel.x = Mathf.Lerp(UnityEngine.Random.Range(-5f, 2f), UnityEngine.Random.Range(2f, 15f), RainFall.rainIntensity);
            }
            this.vel.y = UnityEngine.Random.Range(-10f * RainFall.rainIntensity, -18f * RainFall.rainIntensity);
            this.splashCounter = 0f;
            this.reset = false;
        }
        this.lastLastLastPos = this.lastLastPos;
        this.lastLastPos = this.lastPos;
        //Control fall speed of normal and background drops, tied to rain intensity.
        if (backgroundDrop)
        {
            if (collision)
            {
                this.vel.y = 0f;
            }
            else
            {
                this.vel.y = this.vel.y - (this.gravity * 3.9f);
                if (this.vel.y < Mathf.Lerp(-20f, -35f, RainFall.rainIntensity))
                {
                    this.vel.y = Mathf.Lerp(-20f, -35f, RainFall.rainIntensity);
                }
            }
        }
        else
        {
            if (collision)
            {
                this.vel.y = this.vel.y - (this.gravity * 2.5f);
                if (this.vel.y < Mathf.Lerp(-1f, -2f, RainFall.rainIntensity))
                {
                    this.vel.y = Mathf.Lerp(-1f, -2f, RainFall.rainIntensity);
                }
                if (this.vel.x < Mathf.Lerp(-1f, -4f, RainFall.rainIntensity))
                {
                    this.vel.x = Mathf.Lerp(-1f, -4f, RainFall.rainIntensity);
                }
            }
            else
            {
                this.vel.y = this.vel.y - (this.gravity * 9.5f);
                if (this.vel.y < Mathf.Lerp(-32f, -40f, RainFall.rainIntensity))
                {
                    this.vel.y = Mathf.Lerp(-32f, -40f, RainFall.rainIntensity);
                }
            }
        }
        //Decrease splash scale on hit
        if (collision)
        {
            this.splashCounter = this.splashCounter - 0.1f;
            if (splashCounter < 0f)
            {
                splashCounter = 0f;
            }
        }
        //Raindrop hits floor or water
        bool hitWater = this.room.GetTile(this.pos).WaterSurface;
        if (hitWater && Downpour.water)
        {
            if (room.water && UnityEngine.Random.value > 0.98)
            {
                room.waterObject?.Explosion(this.pos, 0.45f, 0.89f);
            }
            this.pos.y = this.room.MiddleOfTile(this.pos).y + UnityEngine.Random.Range(2f, 9f);
            this.vel.y = this.vel.y * -0.01f;
            this.vel.x = this.vel.x * 0.2f;
            timeToDie = true;
            splashCounter = UnityEngine.Random.Range(0.9f, 1.1f);
            collision = true;
        }
        //Raindrop hits floor or water
        if (this.room.GetTile(this.pos).Solid || hitWater)
        {
            //Decrease velocity if raindrop hits a solid surface or water and increase splash counter
            if (UnityEngine.Random.value > 0.01f)
            {
                if (this.vel.y < 0f && !timeToDie)
                {
                    this.pos.y = this.room.MiddleOfTile(this.pos).y + 11f;
                    this.vel.y = this.vel.y * -0.01f;
                    this.vel.x = this.vel.x * 0.2f;
                    timeToDie = true;
                    splashCounter = UnityEngine.Random.Range(0.9f, 1.1f);
                    collision = true;
                }
                else
                {
                    this.reset = true;
                }
            }
        }
        //If raindrop falls below room bottom, or if rain intensity is 0, remove it.
        if (this.pos.y < -100f || RainFall.rainIntensity == 0f)
        {
            this.reset = true;
        }
        base.Update(eu);
    }
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[2];
        TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[] { new TriangleMesh.Triangle(0, 1, 2) };
        TriangleMesh triangleMesh = new TriangleMesh("Futile_White", tris, false, false);
        sLeaser.sprites[0] = triangleMesh;
        sLeaser.sprites[1] = new FSprite("RainSplash", true);
        if (backgroundDrop)
        {
            sLeaser.sprites[1].alpha = Mathf.Lerp(0.6f, 1f, this.depth);
        }
        else
        {
            sLeaser.sprites[1].alpha = UnityEngine.Random.Range(0.82f, 1f);
        }
        sLeaser.sprites[0].alpha = this.alpha;
        sLeaser.sprites[0].color = Color.Lerp(rCam.currentPalette.fogColor, color, this.depth * 0.9f);
        sLeaser.sprites[1].color = Color.Lerp(rCam.currentPalette.fogColor, color, this.depth * 0.9f);
        this.AddToContainer(sLeaser, rCam, null);
    }
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {

        Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
        Vector2 vector2 = Vector2.Lerp(this.lastLastLastPos, this.lastLastPos, timeStacker);
        if (Custom.DistLess(vector, vector2, 9f))
        {
            vector2 = vector + Custom.DirVec(vector, vector2) * 12f;
        }
        vector2 = Vector2.Lerp(vector, vector2, Mathf.InverseLerp(0f, 0.1f, 1f));
        Vector2 a = Custom.PerpendicularVector((vector - vector2).normalized);
        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(0, vector + a * (this.depth + (this.depth * 0.1f)) - camPos);
        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(1, vector - a * (this.depth + (this.depth * 0.1f)) - camPos);
        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(2, vector2 - camPos);
        sLeaser.sprites[1].x = vector.x - camPos.x;
        sLeaser.sprites[1].y = vector.y - camPos.y;
        sLeaser.sprites[1].rotation = UnityEngine.Random.value * 360f;
        if (Downpour.rainbow)
        {
            sLeaser.sprites[0].color = Color.Lerp(color, Color.Lerp(rCam.PixelColorAtCoordinate(this.pos), rCam.PixelColorAtCoordinate(this.lastLastLastPos), 0.5f), 0.36f);
        }
        else
        {
            sLeaser.sprites[0].color = Color.Lerp(new Color(1f, 1f, 1f), Color.Lerp(rCam.currentPalette.fogColor, Color.Lerp(rCam.PixelColorAtCoordinate(this.pos), rCam.PixelColorAtCoordinate(this.lastLastLastPos), 0.5f), 0.83f), 0.95f);
        }
        //If background drops encounter a depth in the room texture lower than their own depth value, treat it as a collision.
        if (backgroundDrop && !reset && !this.collision && rCam.IsViewedByCameraPosition(rCam.cameraNumber, this.pos) && rCam.DepthAtCoordinate(this.pos) < this.depth)
        {
            splashCounter = 1f;
            timeToDie = true;
            sLeaser.sprites[1].color = Color.Lerp(color, rCam.PixelColorAtCoordinate(this.pos), 0.7f);
            this.collision = true;
        }
        if (splashCounter > 0f && !backgroundDrop)
        {
            sLeaser.sprites[1].color = Color.Lerp(color, rCam.PixelColorAtCoordinate(this.pos), 0.2f);
        }
        if (splashCounter > 0f)
        {
            //If splash counter is greater than 0 adjust the scale of the splash sprite based on whether its a background drop or not.
            if (backgroundDrop)
            {
                sLeaser.sprites[1].scale = Mathf.Lerp(0f, this.depth * 0.4f, splashCounter);
                this.vel.y = 0f;
                this.vel.x = 0f;
            }
            else
            {
                sLeaser.sprites[1].scale = Mathf.Lerp(0f, UnityEngine.Random.Range(0.25f, 0.42f), splashCounter);
            }
            sLeaser.sprites[0].alpha = 0f;
        }
        else
        {
            sLeaser.sprites[1].scale = 0f;
            sLeaser.sprites[0].alpha = this.alpha;
        }
        //Delete raindrop if it falls a certain distance below the currently viewed room camera
        if (this.pos.y < (rCam.pos.y - 100f) || (splashCounter <= 0f && timeToDie))
        {
            this.reset = true;
        }
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        base.ApplyPalette(sLeaser, rCam, palette);
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        base.AddToContainer(sLeaser, rCam, newContatiner);
    }
}
