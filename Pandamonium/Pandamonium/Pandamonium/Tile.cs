using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pandamonium
{
    //Controls the collision detection and behaviour of a tile
    enum TileCollision
    {
        //Does not prevent player movement e.g. a pick up or power-up.
        Passable = 0,

        //A solid tile
        Impassable = 1,

        //Allows the player to jump up through it, but not down.
        Platform = 2,

        //A solid, paintable tile
        Paintable = 3,

        //A Padlock pickup
        Padlock = 4
    }

    //Stores the appearance and collision behaviour of a tile
    struct Tile
    {
        public Texture2D Texture;
        public TileCollision Collision;

        public const int Width = 32;
        public const int Height = 32;

        public static readonly Vector2 Size = new Vector2(Width, Height);

        //Construct a new tile
        public Tile(Texture2D texture, TileCollision collision)
        {
            Texture = texture;
            Collision = collision;
        }
    }
}
