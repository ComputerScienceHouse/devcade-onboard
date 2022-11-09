using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;


namespace onboard
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private SpriteFont _devcadeMenuBig;

        private Menu _mainMenu;
        private DevcadeClient _client;

        private bool _loading = false;
        
        private string state = "launch";
        private float fadeColor = 0f;

        KeyboardState lastState;

        private Texture2D cardTexture;
        private Texture2D[] loadingFrames = new Texture2D[25];
        private Texture2D[] BGFrames = new Texture2D[40];
        private Texture2D titleTexture;
        private Texture2D backgroundTexure;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _mainMenu = new Menu(_graphics);
            _client = new DevcadeClient();
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            if (GraphicsDevice == null)
            {
                _graphics.ApplyChanges();
            }

            _mainMenu.updateDims(_graphics);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _devcadeMenuBig = Content.Load<SpriteFont>("devcade-menu-big");
            cardTexture = Content.Load<Texture2D>("card");
            titleTexture = Content.Load<Texture2D>("tansparent-logo");
            backgroundTexure = Content.Load<Texture2D>("background");

            // TODO: use this.Content to load your game content here
            _mainMenu.setGames(_client.ListBucketContentsAsync("devcade-games").Result);
            _mainMenu.setCards();

            // These for loops load each of the frames of both the loading animation and background loop.
            // Currently, loading 100+ images at once is a little slow for startup 
                // Using every other frame for bg loop is better, do that instead.
                // Don't forget to actually remove those extra frames from the Content folder instead of skipping them in the for loop
            for(int i=1; i<26; i++)
            {
                string name = "Loading_" + i.ToString();
                loadingFrames[i-1] = Content.Load<Texture2D>(name);
            }

            for(int i=1; i<81; i+=2)
            {
                string name = "BG" + i.ToString();
                BGFrames[(i-1)/2] = Content.Load<Texture2D>(name);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Tab))
                Exit();

            // Keyboard control code
            KeyboardState myState = Keyboard.GetState();

            if (lastState == null)
                lastState = Keyboard.GetState(); // god i hate video games

            // If the state is loading, it is still taking input as though it is in the input state..?
            switch(state)
            {
                // Fade in when the app launches
                case "launch":
                    if(fadeColor < 1f)
                    {
                        fadeColor += (float)(gameTime.ElapsedGameTime.TotalSeconds);
                    }
                    else 
                    {
                        // Once the animation completes, begin tracking input
                        state = "input";
                    }

                    break;

                case "loading":
                    // Check for process that matches last launched game and display loading screen if it's running 
                    //_loading = Util.IsProcessOpen(_mainMenu.gameSelected());
                    
                    if(fadeColor < 1f)
                    {
                        fadeColor += (float)(gameTime.ElapsedGameTime.TotalSeconds);
                    } 
                    
                    if(!_loading)
                    {
                        fadeColor = 0f;
                        state = "launch";
                    }
                    break;
                
                // In this state, the user is able to scroll through the menu and launch games
                // TODO: Update _itemSelected  and top/bottom of list check to be a part of Menu.cs
                case "input":
                    if (myState.IsKeyDown(Keys.Down) && lastState.IsKeyUp(Keys.Down) && _mainMenu.itemSelected < _mainMenu.gamesLen() - 1)
                    {
                        _mainMenu.beginAnimUp();
                    }

                    if (myState.IsKeyDown(Keys.Up) && lastState.IsKeyUp(Keys.Up) && _mainMenu.itemSelected > 0)
                    {
                        _mainMenu.beginAnimDown();
                    }

                    if (myState.IsKeyDown(Keys.Enter) && lastState.IsKeyUp(Keys.Enter))
                    {
                        //Console.WriteLine("Running game!!!");
                        //_client.runGame(_mainMenu.gameSelected());

                        fadeColor = 0f;
                        _loading = !_loading;
                        state = "loading";
                    }
                    _mainMenu.animate(gameTime);
                    break;
            }

            lastState = Keyboard.GetState();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();
            GraphicsDevice.Clear(Color.Black);
            
            // Draw the screen normally, until a game is launched, then play loading animation
            if (!_loading)
            {

                // TODO: Add your drawing code here
                //int maxItems = 5;
                _mainMenu.drawBackground(_spriteBatch, BGFrames, fadeColor);
                _mainMenu.drawTitle(_spriteBatch, titleTexture, fadeColor);
                //_mainMenu.drawGames(_devcadeMenuBig, _spriteBatch, _itemSelected, maxItems);
                //_mainMenu.drawSelection(_spriteBatch, _itemSelected % maxItems);
                //_mainMenu.drawGameCount(_devcadeMenuBig, _spriteBatch, _itemSelected + 1, _mainMenu.gamesLen());
                _mainMenu.drawCards(_spriteBatch, cardTexture, _devcadeMenuBig);
            }
            else
            {
               _mainMenu.drawLoading(_spriteBatch, loadingFrames, fadeColor);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

// TODO: Add error handling!!!
