using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pandamonium
{
    class Bullet
    {
        public Texture2D Texture;
        public Vector2 Position;
        public float Speed;
        public int Direction;
        public Vector2 bulletOrigin;
        public bool active;

        public int Width
        {
            get { return Texture.Width; }
        }

        public int Height
        {
            get { return Texture.Height; }
        }

        public void Initialize(Texture2D texture, Vector2 position, int direction)
        {
            Texture = texture;
            Position = position;
            Direction = direction;

            active = true;
            bulletOrigin = new Vector2(Width / 2, Height / 2);
            Speed = 6.0f;
        }

        public void Update()
        {
            //Move the bullets
            Position.X += (Speed * Direction);
            // Position.Y -= (Speed * (float)Math.Cos(Rotation));
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position, null, Color.White, 0.0f,
                new Vector2(Width / 2, Height / 2), 1f, SpriteEffects.None, 0f);
        }
    }
}
