//EDITING TEST


using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;

namespace Pandamonium
{

   

    class Level : IDisposable
    { 
        
        #region Variables & constants

        //These arrays represent the physical structure of the level (tiles etc.)
        public Tile[,] tiles;
        private Layer[] layers;

        public int levelWidth;
        public int levelHeight;

        //Int to determine which layer entities are drawn (player, enemies etc)
        private const int EntityLayer = 2;

        public Player Player
        {
            get { return player; }
        }
        Player player;

        public List<Enemy> enemies = new List<Enemy>();

        // Key location(s) in the level
        private Vector2 start;
        private Point exit = InvalidPosition;
        private static readonly Point InvalidPosition = new Point(-1, -1);

        // Level game state
        private Random random = new Random(354668); // Arbitrary, but constant seed
        private float cameraPosition;

        // A timer
        public TimeSpan TimeRemaining
        {
            get { return timeRemaining; }
        }
        TimeSpan timeRemaining;

        // Determines if the player has reached the level's exit.
        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;

        //Level content
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;



    #endregion

        #region Loading

        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>        
        public Level(IServiceProvider serviceProvider, Stream fileSteam, int levelIndex)
        {
            //create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");

            //Initialize the timer with one minute
            timeRemaining = TimeSpan.FromMinutes(1.0);

            //Loads the tiles.
            LoadTiles(fileSteam);

            // Load background layer textures. for now, all levels must use the same background
            layers = new Layer[3];
            layers[0] = new Layer(Content, "Backgrounds/Layer0", 0.2f);
            layers[1] = new Layer(Content, "Backgrounds/Layer1", 0.5f);
            layers[2] = new Layer(Content, "Backgrounds/Layer2", 0.8f);

            //Load sounds
            //None to load just yet! 
        }

        // Iterates over every tile in the structure file and loads its
        // appearance and behavior. This method also validates that the
        // file is well-formed with a player start point, exit, etc.
        private void LoadTiles(Stream fileStream)
        {
            // Load the level and ensure all the lines are the same length
            int width;

            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = reader.ReadLine();
                width = line.Length;
                while (line != null)
                {
                    lines.Add(line);
                    if (line.Length != width)
                        throw new Exception(String.Format(
                            "The length of line {0} is different from all preceeding lines.", lines.Count));
                    line = reader.ReadLine();
                }
            }

            levelWidth = width * Tile.Width;
            levelHeight = lines.Count * Tile.Height;


            //Allocate the tile grid
            tiles = new Tile[width, lines.Count];

            //Loop over every tile position to load each tile
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; ++x)
                {
                    char tileType = lines[y][x];
                    tiles[x, y] = LoadTile(tileType, x, y);
                }
            }

            // Verify that the level has a beginning and an end.
            if (Player == null)
                throw new NotSupportedException("A level must have a starting point.");
            if (exit == InvalidPosition)
                throw new NotSupportedException("A level must have an exit.");
        }

        //Loads an individual tile's appearance and behaviour
        public Tile LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Blank space
                case '.':
                    return new Tile(null, TileCollision.Passable);

                // A block
                case 'o':
                    return LoadBlock("Block", TileCollision.Impassable);

                // Player one start point
                case '1':
                    return LoadStartTile(x, y);

                // Exit
                case 'x':
                    return LoadExitTile(x, y);

                // Enemy, Using the player sprites temporarily
                case 'Q':
                    return LoadEnemyTile(x, y, "Player");

                default:
                    return new Tile(null, TileCollision.Passable);
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position" + "{1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Creates a new tile. 
        /// </summary>
        private Tile LoadTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }

        /// <summary>
        /// Instantiates a player and places them in the level
        /// </summary>
        private Tile LoadStartTile(int x, int y)
        {
            start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            player = new Player(this, start);

            return new Tile(null, TileCollision.Passable);
        }

        private Tile LoadBlock(string name, TileCollision collision)
        {
            return LoadTile(name, collision);
        }

        /// <summary>
        /// Remembers the location of the level's exit.
        /// </summary>
        private Tile LoadExitTile(int x, int y)
        {
            if (exit != InvalidPosition)
                throw new NotSupportedException("A level may only have one exit.");

            exit = GetBounds(x, y).Center;

            return LoadTile("Exit", TileCollision.Passable);
        }

        private Tile LoadEnemyTile(int x, int y, string spriteSet)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Enemy(this, position, spriteSet));

            return new Tile(null, TileCollision.Passable);
        }

        //Unloads the level content
        public void Dispose()
        {
            Content.Unload();
        }

        #endregion

        #region Bounds and Collision

        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the edges.
        /// </summary>
        public TileCollision GetCollision(int x, int y)
        {
            if ((x < 0 || x >= Width) || (y < 0 || y >= Height))
                return TileCollision.Impassable;

            return tiles[x, y].Collision;
        }

        //The bounding rectangle of a tile
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        //Width of the level measure in tiles.
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        //And the height
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        #endregion

        #region Update
        /// <summary>
        /// Updates all objects in the world and performs collision checks.
        /// Also handles the time limit.
        /// </summary>
        public void Update(
           GameTime gameTime,
           GamePadState gamePadState)
        {
            // Pause while the player is dead or time is expired.
            if (!Player.IsAlive || TimeRemaining == TimeSpan.Zero)
            {
                // Still want to perform physics on the player.
                Player.ApplyPhysics(gameTime, gamePadState);
            }
            else if (ReachedExit)
            {
                // Animate the time being converted into points.
                int seconds = (int)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 100.0f);
                seconds = Math.Min(seconds, (int)Math.Ceiling(TimeRemaining.TotalSeconds));
                timeRemaining -= TimeSpan.FromSeconds(seconds);
            }
            else
            {
                timeRemaining -= gameTime.ElapsedGameTime;
                Player.Update(gameTime, gamePadState);

                // Falling off the bottom of the level kills the player.
                // if (Player.BoundingRectangle.Top >= Height * Tile.Height)
                    //OnPlayerKilled(null);

                UpdateEnemies(gameTime);

                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.
                if (Player.IsAlive &&
                    Player.IsOnGround &&
                    Player.BoundingRectangle.Contains(exit))
                {
                    OnExitReached();
                }
            }

            // Clamp the time remaining at zero.
            if (timeRemaining < TimeSpan.Zero)
                timeRemaining = TimeSpan.Zero;
        }

        private void UpdateEnemies(GameTime gameTime)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].Update(gameTime);

                if (enemies[i].active == false)
                {
                    enemies.RemoveAt(i);
                }
            }
        }

        private void OnExitReached()
        {
            Player.OnReachedExit();
            // exitReachedSound.Play();
            reachedExit = true;
        }

        public void StartNewLife()
        {
            Player.Reset(start);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw everything from background to foregound
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            for (int i = 0; i <= EntityLayer; ++i)
                layers[i].Draw(spriteBatch, cameraPosition);
            spriteBatch.End();

            ScrollCamera(spriteBatch.GraphicsDevice.Viewport);
            Matrix cameraTransform = Matrix.CreateTranslation(-cameraPosition, 0.0f, 0.0f);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default,
                              RasterizerState.CullCounterClockwise, null, cameraTransform);

            DrawTiles(spriteBatch);

            Player.Draw(gameTime, spriteBatch);

            foreach (Enemy enemy in enemies)
                enemy.Draw(gameTime, spriteBatch);

            spriteBatch.End();

            spriteBatch.Begin();
            for (int i = EntityLayer + 1; i < layers.Length; ++i)
                layers[i].Draw(spriteBatch, cameraPosition);
            spriteBatch.End();
        }

        private void ScrollCamera(Viewport viewport)
        {

            const float ViewMargin = 0.5f;

            // Calculate the edges of the screen.
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPosition + marginWidth;
            float marginRight = cameraPosition + viewport.Width - marginWidth;

            // Calculate how far to scroll when the player is near the edges of the screen.
            float cameraMovement = 0.0f;
            if (Player.Position.X < marginLeft)
                cameraMovement = Player.Position.X - marginLeft;
            else if (Player.Position.X > marginRight)
                cameraMovement = Player.Position.X - marginRight;

            // Update the camera position, but prevent scrolling off the ends of the level.
            float maxCameraPosition = Tile.Width * Width - viewport.Width;
            cameraPosition = MathHelper.Clamp(cameraPosition + cameraMovement, 0.0f, maxCameraPosition);
        }

        /// <summary>
        /// Draw the tiles in the level
        /// </summary>
        /// <param name="spriteBatch"></param>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition / Tile.Width);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right, Width - 1);

            // For each tile position
            for (int y = 0; y < Height; ++y)
            {
                for (int x = left; x <= right; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        spriteBatch.Draw(texture, position, Color.White);
                    }
                }
            }
        }

        #endregion
    }
}
