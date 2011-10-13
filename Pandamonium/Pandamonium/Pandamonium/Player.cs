using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pandamonium
{
    class Player
    {
        #region variables and constants

        SpriteFont font;

        // Animations
        private Animation idleAnimation;
        private Animation runAnimation;
        private Animation jumpAnimation;
        private Animation celebrateAnimation;
        private AnimationPlayer sprite;
        private SpriteEffects flip = SpriteEffects.None;

        private float Rotation;

        // Pick-up countdowns etc
        /// <summary>
        /// These are remnants from colour block arena, left in as an example for implementing powerups.
        /// </summary>
        /// bool padlockCollision;
        /// int padlockLocationX;
        /// int padlockLocationY;
        /// public TimeSpan padlockReset;

        // Sounds
        private SoundEffect bulletSound;

        public Level Level
        {
            get { return level; }
        }
        Level level;

        public bool IsAlive
        {
            get { return isAlive; }
        }
        bool isAlive;

        // Physics state
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        private float previousBottom;

        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        Vector2 velocity;

        // constants for horizontal movement
        private const float MoveAcceleration = 13000.0f;
        private const float MaxMoveSpeed = 1750.0f;
        private const float GroundDragFactor = 0.48f;
        private const float AirDragFactor = 0.55f;

        // Constants for vertical movement
        private const float MaxJumpTime = 0.30f;
        private const float JumpLaunchVelocity = -3500.0f;
        private const float GravityAcceleration = 3400.0f;
        private const float MaxFallSpeed = 550.0f;
        private const float JumpControlPower = 0.14f;

        // Input configuration
        private const float MoveStickScale = 1.0f;
        private const float AccelerometerScale = 1.5f;
        private const Buttons JumpButton = Buttons.A;
        private const Buttons ShootButton = Buttons.RightTrigger;

        // Cool down for weapons
        private float shootHeat = 0.0f;
        private bool shootOverheat = false;

        public bool IsOnGround
        {
            get { return isOnGround; }
        }
        bool isOnGround;

        //current user movement input
        private float movement;

        // Jumping state
        private bool isJumping;
        private bool wasJumping;
        private float jumpTime;

        // For shooting
        TimeSpan fireTime;
        TimeSpan previousFireTime;
        public List<Bullet> bullets;
        Texture2D bulletTexture;

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this player in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        #endregion

        #region Loading & Initialise

        public Player(Level level, Vector2 position)
        {
            this.level = level;
            LoadContent();
            Reset(position);
        }

        /// <summary>
        /// Load player sprites and sounds
        /// </summary>
        public void LoadContent()
        {
            // load animations
            idleAnimation = new Animation(level.Content.Load<Texture2D>("Sprites/Player/Idle"), 0.1f, true);
            runAnimation = new Animation(level.Content.Load<Texture2D>("Sprites/Player/Run"), 0.1f, true);
            jumpAnimation = new Animation(level.Content.Load<Texture2D>("Sprites/Player/Jump"), 0.1f, true);
            celebrateAnimation = new Animation(level.Content.Load<Texture2D>("Sprites/Player/Celebrate"), 0.1f, false);

            // Load textures
            bulletTexture = level.Content.Load<Texture2D>("Sprites/Bullet/BulletBlue");

            //Calculate bounds within texture size
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.8);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            // load sounds
            bulletSound = Level.Content.Load<SoundEffect>("Sounds/BulletSound");

            bullets = new List<Bullet>();
            fireTime = TimeSpan.FromSeconds(.07f);

            font = level.Content.Load<SpriteFont>("Font");
        }

        /// <summary>
        /// Resets the player and brings them to life
        /// </summary>
        /// <param name="position">The position the player spawns</param>
        public void Reset(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            isAlive = true;
            sprite.PlayAnimation(idleAnimation);
        }

        #endregion

        #region Update
        /// <summary>
        /// Handles input, performs physics and animates the player
        /// </summary>
        public void Update(GameTime gameTime, GamePadState gamePadState)
        {
            GetInput(gamePadState, gameTime);

            ApplyPhysics(gameTime, gamePadState);

            UpdateBullets();

            UpdateBulletCollisions();

            if (IsAlive && IsOnGround)
            {
                if (Math.Abs(Velocity.X) - 0.02f > 0)
                {
                    sprite.PlayAnimation(runAnimation);
                }
                else
                {
                    sprite.PlayAnimation(idleAnimation);
                }
            }

            // Clear input
            movement = 0.0f;
            isJumping = false;
        }

        private void GetInput(GamePadState gamePadState, GameTime gameTime)
        {
            Vector2 leftStick = gamePadState.ThumbSticks.Right;
            Vector2 rightStick = gamePadState.ThumbSticks.Right;
            if (rightStick != Vector2.Zero)
            {
                Rotation = (float)Math.Atan2(rightStick.X, rightStick.Y);
            }

            //Get analog horizontal movement
            movement = gamePadState.ThumbSticks.Left.X * MoveStickScale;

            //Ignore small movements to prevent running in place
            if (Math.Abs(movement) < 0.5f)
                movement = 0.0f;

            // If any digital horizontal movement is found, override the analog movement
            if (gamePadState.IsButtonDown(Buttons.DPadLeft))
                movement = -1.0f;
            else if (gamePadState.IsButtonDown(Buttons.DPadRight))
                movement = 1.0f;

            // Check if the player wants to jump
            isJumping = gamePadState.IsButtonDown(JumpButton);

            // Check if the player wants to shoot
            if (gameTime.TotalGameTime - previousFireTime > fireTime)
            {
                if (gamePadState.IsButtonUp(ShootButton))
                {
                    if (gamePadState.IsButtonDown(Buttons.LeftTrigger))
                    {
                        if (shootHeat < 0)
                            shootHeat = 0;

                        shootHeat -= 0.1f;
                    }
                }

                if (gamePadState.IsButtonDown(ShootButton))
                {
                    if (shootHeat > 5)
                    {
                        shootHeat = 5;
                        shootOverheat = true;
                    }

                    if (shootHeat < 0 || shootHeat == 0)
                    {
                        shootHeat = 0;
                        shootOverheat = false;
                    }

                    if (shootOverheat == false)
                    {
                        AddBullet(Vector2.Zero);
                        previousFireTime = gameTime.TotalGameTime;
                        shootHeat += 0.2f;
                    }
                }
            }
        }

        private void AddBullet(Vector2 position)
        {
            int direction = 0;
            if (velocity.X < 0)
                direction = -1;
            else if (velocity.X >= 0)
                direction = 1;
            Bullet bullet = new Bullet();
            bullet.Initialize(bulletTexture, new Vector2(Position.X, Position.Y - 32), direction);
            bullets.Add(bullet);
            bulletSound.Play();
        }

        private void UpdateBullets()
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update();

                //If the bullet is off the screen, destroy it.
                if ((bullets[i].Position.X > level.levelWidth) || (bullets[i].Position.X < 0) ||
                    (bullets[i].Position.Y > level.levelHeight) || (bullets[i].Position.Y < 0))
                    bullets[i].active = false;

                //If the bullet is not active, destroy it.
                if (bullets[i].active == false)
                {
                    bullets.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Apply physics and jumping logic
        /// <summary>
        /// Updates the player's velocity and position based on input, gravity etc etc
        /// </summary>
        public void ApplyPhysics(GameTime gameTime, GamePadState gamePadState)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 previousPosition = Position;

            // Base velocity = horizontal movement and acceleration due to gravity
            velocity.X += movement * MoveAcceleration * elapsed;
            velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed,
                -MaxFallSpeed, MaxFallSpeed);

            velocity.Y = DoJump(velocity.Y, gameTime, gamePadState);

            // Apply pseudo-drag horizontally
            if (IsOnGround)
                velocity.X *= GroundDragFactor;
            else
                velocity.X *= AirDragFactor;

            // Prevent the player from running faster than the top speed
            velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);

            // Apply velocity
            Position += velocity * elapsed;
            Position = new Vector2((float)Math.Round(position.X), (float)Math.Round(position.Y));

            // If the player is colliding with the level, sort it out!
            HandleCollisions(gameTime);

            // If the collision stopped us from moving, reset the velocity to zero
            if (Position.X == previousPosition.X)
                velocity.X = 0;

            if (Position.Y == previousPosition.Y)
                velocity.Y = 0;
        }

        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime, GamePadState gamePadState)
        {
            // If the player wants to jump
            if (isJumping)
            {
                // Begin or continue a jump
                if ((!wasJumping && IsOnGround) || jumpTime > 0.0f)
                {
                    // if (jumpTime == 0.0f)
                    //    jumpSound.Play();
                    GamePad.SetVibration(PlayerIndex.One, 0.1f, 0.1f);
                    jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    sprite.PlayAnimation(jumpAnimation);
                }
                else
                    GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);

                // if the player is in the ascent of a jump
                if (0.0f < jumpTime && jumpTime <= MaxJumpTime)
                {
                    // Fully override the vertical velocity with a power curve that 
                    // gives players more control over the top of the jump
                    velocityY = JumpLaunchVelocity * (
                        1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));

                    GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
                }
                else
                {
                    // reached the top of a jump
                    jumpTime = 0.0f;
                }
            }
            else
            {
                // continues not jumping or cancels a jump in progress
                jumpTime = 0.0f;
            }

            wasJumping = isJumping;

            return velocityY;
        }

        #endregion

        #region Collisions

        private void UpdateBulletCollisions()
        {
            Rectangle bulletRectangle;
            Rectangle blockRectangle;
            int blocks = level.Width * level.Height;



            for (int i = 0; i < bullets.Count; i++)
            {
                // Add a bounding rectangle to each bullet
                bulletRectangle = new Rectangle((int)bullets[i].Position.X - bullets[i].Width / 2,
                            (int)bullets[i].Position.Y - bullets[i].Height / 2,
                            bullets[i].Width, bullets[i].Height);

                for (int j = 0; j < level.enemies.Count; j++)
                {
                    if (bulletRectangle.Intersects(level.enemies[j].BoundingRectangle))
                    {
                        bullets[i].active = false;
                        level.enemies[j].active = false;
                    }
                }

                for (int x = 0; x < level.Width; x++)
                {
                    for (int y = 0; y < level.Height; y++)
                    {
                        TileCollision collision = level.GetCollision(x, y);
                        blockRectangle = level.GetBounds(x, y);
                        // If the tile is Impassable ie Collidable
                        if (collision != TileCollision.Passable)
                        {
                            // Check for a collision between the blocks and bullets
                            if (bulletRectangle.Intersects(blockRectangle))
                            {
                                bullets[i].active = false; // Remove the bullet from memory
                            }
                        }
                    }
                }
            }
        }

        private void HandleCollisions(GameTime gameTime)
        {
            // Get the player's bounding rectangle and find neighbouring tiles.
            Rectangle bounds = BoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            // reset flag to search for ground collision
            isOnGround = false;

            // for each potentially colliding tile...
            // This is quickly becoming confusing! NEED A BETTER WAY OF DOING THIS!!!
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable
                    TileCollision collision = Level.GetCollision(x, y);
                    if (collision != TileCollision.Passable)
                    {
                        // Determine collision depth and magnitude
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);
                            // Resolve the collision along the shallow axis
                            if (absDepthY < absDepthX || collision == TileCollision.Platform)
                            {
                                // If the top of the tile has been crossed, the player is on the ground
                                if (previousBottom <= tileBounds.Top)
                                    isOnGround = true;

                                // Ignore platforms, unless on the ground.
                                if (collision == TileCollision.Impassable || IsOnGround || collision == TileCollision.Paintable)
                                {
                                    // Resolve collision along the Y axis
                                    Position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds
                                    bounds = BoundingRectangle;
                                }
                            }
                            else if (collision == TileCollision.Impassable || collision == TileCollision.Paintable)
                            {
                                // Resolve the collision along the X axis.
                                Position = new Vector2(Position.X + depth.X, Position.Y);

                                // Perform further collisions with the new bounds.
                                bounds = BoundingRectangle;
                            }
                        }
                    }
                }
            }
            // Save the new bounds bottom
            previousBottom = bounds.Bottom;
        }

        public void OnReachedExit()
        {
            sprite.PlayAnimation(celebrateAnimation);
        }

        #endregion

        #region Draw
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Flip the sprite to face the way we are moving.
            if (Velocity.X > 0)
                flip = SpriteEffects.FlipHorizontally;
            else if (Velocity.X < 0)
                flip = SpriteEffects.None;

            for (int i = 0; i < bullets.Count; i++)
            {
                bullets[i].Draw(spriteBatch);
            }

            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, flip);

            spriteBatch.DrawString(font, Convert.ToString(bullets.Count), Vector2.Zero, Color.White);

            spriteBatch.DrawString(font, Convert.ToString(shootHeat), new Vector2(0, 60), Color.DarkBlue);
            spriteBatch.DrawString(font, Convert.ToString(level.levelHeight), new Vector2(0, 40), Color.White);
        }
        #endregion
    }
}
