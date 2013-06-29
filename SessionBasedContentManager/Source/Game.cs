using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AssetBasedContentManager
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class ContentLoadingGame : Game
    {
        GraphicsDeviceManager mGraphics;
        ThreadWorker mBackgroundLoadingThreadWorker;

        AssetManager mAssetManager;
        AssetRequestBatch mRequestBatch;

        SpriteBatch mSpriteBatch;
        SpriteFont mSpriteFont;
        ReferenceObject<Texture2D> mTexture;

        public ContentLoadingGame()
        {
            // Create the graphics device manager.
            mGraphics = new GraphicsDeviceManager(this);

            // Create the thread worker responsible for loading assets in the background.
#if XBOX
            // Core 2, hyperthread 2.
            mBackgroundLoadingThreadWorker = new ThreadWorker(3);
#else
            mBackgroundLoadingThreadWorker = new ThreadWorker();
#endif // XBOX

            // Create the asset manager.
            mAssetManager = new AssetManager(mBackgroundLoadingThreadWorker, Services);
            mAssetManager.RootDirectory = "Content";
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            mSpriteBatch = new SpriteBatch(GraphicsDevice);

            // Load our permanent resources, the permanent session cannot be freed.
            mSpriteFont = mAssetManager.Load<SpriteFont>("SpriteFont", AssetSession.Permanent);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            GamePadState _gamePadState = GamePad.GetState(PlayerIndex.One);
            if (_gamePadState.Buttons.Back == ButtonState.Pressed)
            {
                this.Exit();
            }

            KeyboardState _keyBoardState = Keyboard.GetState();
            if (mRequestBatch == null
                && (_keyBoardState.IsKeyDown(Keys.Enter) || _gamePadState.Buttons.A == ButtonState.Pressed))
            {
                // Start loading the texture asynchronously.

                // First get a free asset session.
                AssetSession _assetSession = mAssetManager.GetNewSession();

                // Create a request batch.
                mRequestBatch = new AssetRequestBatch(_assetSession);
                mTexture = new ReferenceObject<Texture2D>(null);

                // Add the texture to be loaded. We can add multiple files to be loaded asynchronously here.
                mRequestBatch.AddItem<Texture2D>(mTexture, "Granny");

                // Add the request batch to the asset manager
                mAssetManager.AddRequestBatch(mRequestBatch);

                // Flush the session so that the content starts loading.
                mAssetManager.FlushRequestBatches(_assetSession);
            }
            else if (mRequestBatch != null
                && _keyBoardState.IsKeyDown(Keys.Back) || _gamePadState.Buttons.B == ButtonState.Pressed)
            {
                // Unload the texture.

                // First free the session.
                mAssetManager.FreeSession(mRequestBatch.mSession);
                mRequestBatch = null;
                mTexture = null;

                // Next unload all unused assets. Only freed sessions will be unloaded.
                mAssetManager.UnloadUnusedAssets();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            mSpriteBatch.Begin();

            if (mRequestBatch == null)
            {
                mSpriteBatch.DrawString(mSpriteFont, "Press A or Enter to load the texture", Vector2.One, Color.Black);
                mSpriteBatch.DrawString(mSpriteFont, "Press A or Enter to load the texture", Vector2.Zero, Color.White);
            }
            else if (mRequestBatch.IsLoaded())
            {
                mSpriteBatch.Draw(mTexture.mValue, Vector2.Zero, Color.White);
                mSpriteBatch.DrawString(mSpriteFont, "Press B or Backspace to unload the texture", Vector2.One, Color.Black);
                mSpriteBatch.DrawString(mSpriteFont, "Press B or Backspace to unload the texture", Vector2.Zero, Color.White);
            }
            else
            {
                mSpriteBatch.DrawString(mSpriteFont, "Loading texture", Vector2.One, Color.Black);
                mSpriteBatch.DrawString(mSpriteFont, "Loading texture", Vector2.Zero, Color.White);
            }

            mSpriteBatch.End();
            base.Draw(gameTime);
        }

        /// <summary>
        /// Disposes of allocated resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of the background thread worker.
                mBackgroundLoadingThreadWorker.Dispose();

                // Dispose of our AssetManager.
                mAssetManager.Unload();
                mAssetManager.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
