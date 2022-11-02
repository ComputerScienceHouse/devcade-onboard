using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace onboard
{
    public class Menu
    {
        private GraphicsDeviceManager _device;

        // Synonymous with cards list from my project
        // Will have to convert this to a list of MenuCards
        private List<string> gameTitles;
        private List<MenuCard> cards = new List<MenuCard>();
        private int loadingFrame = 0;

        private int _sWidth;
        private int _sHeight;

        private float moveTime = 0.15f; // This is the total time the scrolling animation takes
        private float timeRemaining = 0f;

        public bool movingUp;
        public bool movingDown;

        public Menu(GraphicsDeviceManager graph)
        {
            _device = graph;
        }

        public void updateDims(GraphicsDeviceManager _graphics) 
        {
            // This will be the apect ratio of the screen on the machine
            _sWidth = 720;
            _sHeight = 1280;

            _graphics.PreferredBackBufferHeight = _sHeight;
            _graphics.PreferredBackBufferWidth = _sWidth;
            _graphics.ApplyChanges();
        }

        public void getGames() {
            gameTitles = new List<String> { "bankshot", "spacewar", "brickbreaker", "Meatball", "meatball2", "Wilson", "Are you Wilson?", "TSCHOMBPFTHPFHP", "shreck 13", "Meatball, but again", "aaaaaaaaaa", "kubernetes", "Wilson again", "Caffeine", "Wheeeee", "Something actually scary" };
            //gameTitles = new List<String> { "Flappy Meatball", "Lightspeed", "Bank Shot", "Brick Breaker", "Minecraft 2" };

        }

        public void setGames(List<string> theGames)
        {
            gameTitles = theGames;
        }

        public void setCards()
        {
            for(int i=0; i<gameTitles.Count; i++)
            {
                cards.Add(new MenuCard(i*-1,gameTitles[i]));
            }
        }

        public int gamesLen()
        {
            return gameTitles.Count;
        }

        public string gameAt(int at)
        {
            return gameTitles.ElementAt(at);
        }

        public void drawTitle(SpriteFont font, SpriteBatch _spriteBatch)
        {
            string welcome = "Welcome to Devcade";
            Vector2 welcomeSize = font.MeasureString(welcome);
            _spriteBatch.DrawString(font, welcome, new Vector2(_sWidth / 2 - welcomeSize.X / 2, _sHeight / 5 - welcomeSize.Y), Color.Black);
                         

            string wares = "Come enjoy our wares";
            Vector2 waresSize = font.MeasureString(wares);
            _spriteBatch.DrawString(font, wares, new Vector2(_sWidth / 2 + welcomeSize.X / 8, (_sHeight / 4.2f)), Color.Yellow, -0.3f, new Vector2(0, 0), new Vector2(0.5f, 0.5f), SpriteEffects.None, 1);
        }

        public void drawLoading(SpriteFont font, SpriteBatch _spriteBatch, Texture2D[] loadingFrames)
        {
            // Cycles through all the frames of the loading animation. 
            // At 60 fps this goes way too fast, find a way to fix that
            if(loadingFrame > 24)
            {
                loadingFrame = 0;
            }

            _spriteBatch.Draw(
                loadingFrames[loadingFrame],
                new Rectangle(60,0, 536, _sHeight),
                Color.White
            );

            loadingFrame++;
        }

        public void drawSelection(SpriteBatch _spriteBatch, int menuItemSelected)
        {
            int rectLength = 300;
            int rectHeight = 40;
            RectangleSprite.DrawRectangle(_spriteBatch, new Rectangle(
                    _sWidth / 2 - rectLength/2,
                    ((_sHeight / 5) + (_sHeight / 10)) + ((_sHeight / 10) * menuItemSelected),
                    rectLength,
                    rectHeight
                ),
                Color.White, 3);
        }

        public void drawGameCount(SpriteFont font, SpriteBatch _spriteBatch, int itemSelected, int totalItems)
        {
            _spriteBatch.DrawString(font, itemSelected + " / " + totalItems, new Vector2(50, 50), Color.White);
        }

        public void drawGames(SpriteFont font, SpriteBatch _spriteBatch, int itemSelected, int maxItems)
        {
            int startPosition = (int)(Math.Floor(itemSelected / (double)maxItems) * maxItems);
            for (int i = 0; i < maxItems; i++)
            {
                if (startPosition + i > gameTitles.Count - 1)
                    break;
                string gameTitle = gameTitles.ElementAt(startPosition+i);
                Vector2 gameTitleSize = font.MeasureString(gameTitle);
                _spriteBatch.DrawString(font, gameTitle, new Vector2(_sWidth / 2 - gameTitleSize.X / 2, ((_sHeight / 5) + (_sHeight / 10)) + ((_sHeight / 10) * i)), Color.White);
            }
            /*
            int index = 0;
            foreach (String gameTitle in gameTitles)
            {
                Vector2 gameTitleSize = font.MeasureString(gameTitle);
                _spriteBatch.DrawString(font, gameTitle, new Vector2(_sWidth / 2 - gameTitleSize.X / 2, ((_sHeight / 5) + (_sHeight / 10)) + ((_sHeight / 10) * index)), Color.White);
                index++;
            }    */
        }

        public void drawCards(SpriteBatch _spriteBatch, Texture2D cardTexture, SpriteFont font)
        {
            // I still have no idea why the layerDepth does not work
            foreach(MenuCard card in cards)
            {
                if(Math.Abs(card.listPos) == 4)
                {
                   card.DrawSelf(_spriteBatch, cardTexture, font, _sHeight);
                }
                
            }
            foreach(MenuCard card in cards)
            {
                if(Math.Abs(card.listPos) == 3)
                {
                   card.DrawSelf(_spriteBatch, cardTexture, font, _sHeight);
                }
                
            }
            foreach(MenuCard card in cards)
            {
                if(Math.Abs(card.listPos) == 2)
                {
                   card.DrawSelf(_spriteBatch, cardTexture, font, _sHeight);
                }
                
            }
            foreach(MenuCard card in cards)
            {
                if(Math.Abs(card.listPos) == 1)
                {
                   card.DrawSelf(_spriteBatch, cardTexture, font, _sHeight);
                }
                
            }
            foreach(MenuCard card in cards)
            {
                if(Math.Abs(card.listPos) == 0)
                {
                   card.DrawSelf(_spriteBatch, cardTexture, font, _sHeight);
                }
                
            }
        }

        public void beginAnimUp()
        {
            if (!(movingUp || movingDown))
                {
                    foreach(MenuCard card in cards)
                    {
                        card.listPos++;
                        //card.layer = (float)Math.Abs(card.listPos) / 4;
                    }
                    timeRemaining = moveTime; // Time remaining in the animation begins at the total expected move time
                    movingUp = true;
                }
        }

        public void beginAnimDown()
        {
            if (!(movingDown || movingUp))
                {
                    foreach (MenuCard card in cards)
                    {
                        card.listPos--;
                        //card.layer = (float)Math.Abs(card.listPos) / 4;
                    }
                    timeRemaining = moveTime; // Time remaining in the animation begins at the total expected move time
                    movingDown = true;
                }
        }

        public void animate(GameTime gameTime)
        {
            if (timeRemaining > 0) // Continues to execute the following code as long as the animation is playing AND max time isn't reached
            {
                if(movingUp)
                {
                    foreach(MenuCard card in cards)
                    {
                        card.moveUp((float)gameTime.ElapsedGameTime.TotalSeconds);
                    }
                }

                else if(movingDown)
                {
                    foreach(MenuCard card in cards)
                    {
                        card.moveDown((float)gameTime.ElapsedGameTime.TotalSeconds);
                    }
                }

                timeRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds; // Decrement time until it reaches zero
            }

            else // Once timeleft reaches 0, finish anim. 
            {
                movingUp = false;
                movingDown = false;
            }
        }
    }
}
