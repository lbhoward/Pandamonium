using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pandamonium
{
    // Determines direction enemy is facing on the x axis.
    enum Direction
    {
        Left = -1,
        Right = 1,
    }

    class Enemy
    {
        public Level Level
        {
            get { return level; }
        }
        Level level;

        // Position
        public Vector2 Position
        {
            get { return position; }
        }
        Vector2 position;

        // Rectangle bounds
        private Rectangle localBounds;
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        //Animations
        private Animation runAnimation;
        private Animation idleAnimation;
        private AnimationPlayer sprite;

        // The direction the enemy is facing and moving along x
        private Direction direction = Direction.Left;

        // How long the enemy waits before turning around
        private float waitTime;
        private const float MaxWaitTime = 0.5f;

        // Enemy speed
        private const float MoveSpeed = 50.0f;

        public bool active;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="level"></param>
        /// <param name="position"></param>
        /// <param name="spriteSet">Which sprite sheet to use for animations. Different monsters etc</param>
        public Enemy(Level level, Vector2 position, string spriteSet)
        {
            this.level = level;
            this.position = position;

            LoadContent(spriteSet);

            active = true;
        }

        public void LoadContent(string spriteSet)
        {
            // Animations
            spriteSet = "Sprites/" + spriteSet + "/";
            runAnimation = new Animation(level.Content.Load<Texture2D>(spriteSet + "Run"), 0.1f, true);
            idleAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Idle"), 0.15f, true);
            sprite.PlayAnimation(idleAnimation);

            // Calculate boinds within texture size
            int width = (int)(idleAnimation.FrameWidth * 0.35);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.7);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Calculate tile position depending which direction the enemy is walking
            // -1 = left, 1 = right.
            float posX = Position.X + localBounds.Width / 2 * (int)direction;
            int tileX = (int)Math.Floor(posX / Tile.Width) - (int)direction;
            int tileY = (int)Math.Floor(Position.Y / Tile.Height);

            if (waitTime > 0)
            {
                // Wait for some amount of time.
                waitTime = Math.Max(0.0f, waitTime - (float)gameTime.ElapsedGameTime.TotalSeconds);
                if (waitTime <= 0.0f)
                {
                    // Then turn around.
                    direction = (Direction)(-(int)direction);
                }
            }
            else
            {
                // If we are about to run into a wall or off a cliff, start waiting.
                if (Level.GetCollision(tileX + (int)direction, tileY - 1) == TileCollision.Impassable ||
                    Level.GetCollision(tileX + (int)direction, tileY) == TileCollision.Passable)
                {
                    waitTime = MaxWaitTime;
                }
                else
                {
                    // Move in the current direction.
                    Vector2 velocity = new Vector2((int)direction * MoveSpeed * elapsed, 0.0f);
                    position = position + velocity;
                }
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Stop running when the game is paused or before turning around
            if (!Level.Player.IsAlive ||
                Level.ReachedExit ||
                Level.TimeRemaining == TimeSpan.Zero ||
                waitTime > 0)
            {
                sprite.PlayAnimation(idleAnimation);
            }
            else
            {
                sprite.PlayAnimation(runAnimation);
            }

            // Draw facing the way the enemy is moving.
            SpriteEffects flip = direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            sprite.Draw(gameTime, spriteBatch, Position, flip);
        }
    }
}
