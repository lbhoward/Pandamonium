using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Pandamonium
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    class Pandamonium : Microsoft.Xna.Framework.Game
    {
        // Resources for drawing
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Global content
        private SpriteFont font;

        // Level game state
        private int levelIndex = -1;
        private Level level;
        private bool wasContinuePressed;

        // When the remaining time is less than the warning time, it blinks and looks cool!
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(10);

        private GamePadState gamePadState;

        public Viewport viewport;



        // The number of levels in the level directory.
        private const int numberOfLevels = 3;
        public Pandamonium()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            viewport = new Viewport();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            font = Content.Load<SpriteFont>("Font");

            LoadNextLevel();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            // update the level, passing down the gametime along with all the input states
            level.Update(gameTime, gamePadState);

            base.Update(gameTime);
        }

        private void HandleInput()
        {
            // get all of the input states
            gamePadState = GamePad.GetState(PlayerIndex.One);

            // Allow the game to exit
            if (gamePadState.Buttons.Back == ButtonState.Pressed)
                Exit();

            bool continuePressed =
                gamePadState.IsButtonDown(Buttons.A);

            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!wasContinuePressed && continuePressed)
            {
                if (!level.Player.IsAlive)
                {
                    level.StartNewLife();
                }
                else if (level.TimeRemaining == TimeSpan.Zero)
                {
                    if (level.ReachedExit)
                        LoadNextLevel();
                    else
                        ReloadCurrentLevel();
                }
            }

            wasContinuePressed = continuePressed;
        }

        private void LoadNextLevel()
        {
            // move to the next level
            levelIndex = (levelIndex + 1) % numberOfLevels;

            // Unload the content of the current level, then load the next one
            if (level != null)
                level.Dispose();

            //Load the new level
            string levelPath = string.Format("Content/Levels/{0}.txt", levelIndex);
            using (Stream fileStream = TitleContainer.OpenStream(levelPath))
                level = new Level(Services, fileStream, levelIndex);
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            level.Draw(gameTime, spriteBatch);

            DrawHud();


            base.Draw(gameTime);
        }

        /// <summary>
        /// Debugging Text only at the moment
        /// </summary>
        private void DrawHud()
        {
            spriteBatch.Begin();

            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            if (level.ReachedExit)
                spriteBatch.DrawString(font, "Press A to continue", new Vector2(400, 200), Color.Red);

            spriteBatch.End();
        }
    }
}
