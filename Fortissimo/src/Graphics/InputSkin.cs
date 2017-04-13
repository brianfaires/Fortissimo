#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using SongDataIO;
using LightningSample;
using System.IO;
#endregion

namespace Fortissimo
{
    /// <summary>
    /// This describes an available instrument to play.  It is in charge
    /// of drawing itself and requesting any additional details from
    /// the input device connected to it.
    /// </summary>
    public class InputSkin : DrawableGameComponent
    {
        /// <summary>
        /// A compatible input device for the instrument.
        /// </summary>
        protected InputManager _input;
        protected int _tracks;

        public InputSkin(Game game, InputManager input) : base(game)
        {
            this._input = input;
        }

        /// <summary>
        /// Overridable check to see if an instrument is compatible with
        /// the given input device.
        /// </summary>
        public virtual bool isCompatible(InputManager input)
        {
            return (input.KeyRangeForType() == _tracks);
        }

        public virtual void ReplaceBackground(String location)
        {
        }
    }

    public class GuitarASDFG : InputSkin
    {
        SpriteFont spriteFont;

        Texture2D background;

        enum KeyState { NONE = 0, PRESSED, HELD }

        ExplosionParticleSystem explosion;

        int itemCount = 5;

        LightningBolt[] long_pressed;
        bool[] long_pressed_vis;

        Random rand = new Random();

        int first_note = 0;

        bool[] draw_pressed;

        Matrix view;
        Matrix projection;

        float rotation;

        VertexPositionTexture[] fret;
        VertexPositionTexture[] bar;
        VertexPositionTexture[] guitar_string;
        VertexPositionTexture[] long_note;
        Texture2D long_note_color;

        int fret_board_count = 2;
        int current_fret_board = 0;
        Texture2D[] fret_board;
        Texture2D g_string;
        Texture2D bar_text;

        Model[] draw_notes;
        Model[] hopoModels;
        Model pickup;

        Model[] pickups;

        float fret_y_1;
        float fret_y_2;

        VertexDeclaration textureDeclaration;
        VertexDeclaration colorDeclaration;

        BasicEffect FretEffectA;
        BasicEffect FretEffectB;
        BasicEffect StringEffect;
        BasicEffect longEffect;
        BasicEffect barEffect;

        public GuitarASDFG(InputManager input, Game game)
            : base(game, input)
        {
            explosion = new ExplosionParticleSystem(game, 100);
            game.Components.Add(explosion);

            draw_pressed = new bool[5];
            for (int i = 0; i < 5; i++)
            {
                draw_pressed[i] = false;
            }

            long_pressed = new LightningBolt[itemCount];
            long_pressed_vis = new bool[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                long_pressed_vis[i] = false;
            }

            LightningDescriptor ld = new LightningDescriptor();
            ld.Topology.Clear();
            ld.Topology.Add(LightningSubdivisionOp.Jitter);
            ld.Topology.Add(LightningSubdivisionOp.Jitter);
            ld.Topology.Add(LightningSubdivisionOp.Jitter);
            ld.ForkForwardDeviation = new Range(-1, 1);
            ld.ForkLengthPercentage = 0.6f;
            ld.ExteriorColor = Color.Blue;
            ld.JitterDeviationRadius = 0.1f;
            ld.JitterDecayRate = 1.0f;
            ld.IsGlowEnabled = true;
            ld.GlowIntensity = 3.0f;
            ld.BaseWidth = 0.2f;
            long_pressed[0] = new LightningBolt(game, new Vector3(0, 12, 0), new Vector3(0, -10, 0), ld);
            long_pressed[1] = new LightningBolt(game, new Vector3(0, 12, 0), new Vector3(0, -10, 0), ld);
            long_pressed[2] = new LightningBolt(game, new Vector3(0, 12, 0), new Vector3(0, -10, 0), ld);
            long_pressed[3] = new LightningBolt(game, new Vector3(0, 12, 0), new Vector3(0, -10, 0), ld);
            long_pressed[4] = new LightningBolt(game, new Vector3(0, 12, 0), new Vector3(0, -10, 0), ld);


            // 3D stuff
            Vector3 pos = new Vector3(0,0,5);
            Vector3 target = new Vector3(0,0,0);
            view = Matrix.CreateLookAt(pos, target, Vector3.Up);
            projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                (float)Game.Window.ClientBounds.Width /
                (float)Game.Window.ClientBounds.Height,
                1, 7);

            rotation = -(MathHelper.PiOver4 + MathHelper.PiOver4 / 2);

            // Initialize vertices
            fret = new VertexPositionTexture[4];
            fret[0] = new VertexPositionTexture(new Vector3(-0.8f, 3, 0), new Vector2(0, 0));
            fret[1] = new VertexPositionTexture(new Vector3(0.8f, 3, 0), new Vector2(1, 0));
            fret[2] = new VertexPositionTexture(new Vector3(-0.8f, -3, 0), new Vector2(0, 1));
            fret[3] = new VertexPositionTexture(new Vector3(0.8f, -3, 0), new Vector2(1, 1));
            
            // Initialize vertices
            guitar_string = new VertexPositionTexture[4];
            guitar_string[0] = new VertexPositionTexture(new Vector3(-0.01f, 3, 0.01f), new Vector2(0, 0));
            guitar_string[1] = new VertexPositionTexture(new Vector3(0.01f, 3, 0.01f), new Vector2(1, 0));
            guitar_string[2] = new VertexPositionTexture(new Vector3(-0.01f, -2.3f, 0.01f), new Vector2(0, 1));
            guitar_string[3] = new VertexPositionTexture(new Vector3(0.01f, -2.3f, 0.01f), new Vector2(1, 1));
            
            // Initialize vertices
            bar = new VertexPositionTexture[4];
            bar[0] = new VertexPositionTexture(new Vector3(-0.8f, 0.02f, 0.007f), new Vector2(0, 0));
            bar[1] = new VertexPositionTexture(new Vector3(0.8f, 0.02f, 0.007f), new Vector2(1, 0));
            bar[2] = new VertexPositionTexture(new Vector3(-0.8f, -0.02f, 0.007f), new Vector2(0, 1));
            bar[3] = new VertexPositionTexture(new Vector3(0.8f, -0.02f, 0.007f), new Vector2(1, 1));

            // Initialize long notes
            long_note = new VertexPositionTexture[4];

            fret_y_1 = 0.0f;
            fret_y_2 = 6.0f;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            // Temporary sprite font.  Should be removed.
            spriteFont = Game.Content.Load<SpriteFont>("defaultSpriteFont");

            background = Game.Content.Load<Texture2D>("Skins/Guitar/Background");

            // randomnly choose a fretboard
            current_fret_board = rand.Next(2);
            fret_board = new Texture2D[fret_board_count * 2];
            fret_board[0] = Game.Content.Load<Texture2D>("Skins/Guitar/fret_one");
            fret_board[1] = Game.Content.Load<Texture2D>("Skins/Guitar/fret_one_invert");
            fret_board[2] = Game.Content.Load<Texture2D>("Skins/Guitar/fret_two");
            fret_board[3] = Game.Content.Load<Texture2D>("Skins/Guitar/fret_two_invert");

            g_string = Game.Content.Load<Texture2D>("Skins/Guitar/string");
            bar_text = Game.Content.Load<Texture2D>("Skins/Guitar/bar");

            draw_notes = new Model[5];
            draw_notes[0] = Game.Content.Load<Model>("Models/note_one");
            draw_notes[1] = Game.Content.Load<Model>("Models/note_two");
            draw_notes[2] = Game.Content.Load<Model>("Models/note_three");
            draw_notes[3] = Game.Content.Load<Model>("Models/note_four");
            draw_notes[4] = Game.Content.Load<Model>("Models/note_five");

            hopoModels = new Model[5];
            hopoModels[0] = Game.Content.Load<Model>("Models/hopo_one");
            hopoModels[1] = Game.Content.Load<Model>("Models/hopo_two");
            hopoModels[2] = Game.Content.Load<Model>("Models/hopo_three");
            hopoModels[3] = Game.Content.Load<Model>("Models/hopo_four");
            hopoModels[4] = Game.Content.Load<Model>("Models/hopo_five");

            pickup = Game.Content.Load<Model>("Models/pickup");

            pickups = new Model[5];
            pickups[0] = Game.Content.Load<Model>("Models/pickup_green");
            pickups[1] = Game.Content.Load<Model>("Models/pickup_red");
            pickups[2] = Game.Content.Load<Model>("Models/pickup_yellow");
            pickups[3] = Game.Content.Load<Model>("Models/pickup_blue");
            pickups[4] = Game.Content.Load<Model>("Models/pickup_orange");

            textureDeclaration = new VertexDeclaration(VertexPositionTexture.VertexDeclaration.GetVertexElements()); // 4.0change

            colorDeclaration = new VertexDeclaration(VertexPositionTexture.VertexDeclaration.GetVertexElements()); // 4.0change

            FretEffectA = new BasicEffect(GraphicsDevice); // 4.0change
            FretEffectA.World = Matrix.Identity * Matrix.CreateTranslation(0, fret_y_1, 0.0f) * Matrix.CreateRotationX(rotation);
            FretEffectA.View = view;
            FretEffectA.Projection = projection;
            FretEffectA.Texture = fret_board[current_fret_board*2];
            FretEffectA.TextureEnabled = true;
            
            FretEffectB = new BasicEffect(GraphicsDevice); // 4.0change
            FretEffectB.World = Matrix.Identity * Matrix.CreateTranslation(0, fret_y_2, 0.0f) * Matrix.CreateRotationX(rotation);
            FretEffectB.View = view;
            FretEffectB.Projection = projection;
            FretEffectB.Texture = fret_board[current_fret_board*2+1];
            FretEffectB.TextureEnabled = true;
            
            StringEffect = new BasicEffect(GraphicsDevice); // 4.0change
            StringEffect.World = Matrix.Identity;
            StringEffect.View = view;
            StringEffect.Projection = projection;
            StringEffect.Texture = g_string;
            StringEffect.TextureEnabled = true;

            barEffect = new BasicEffect(GraphicsDevice); // 4.0change
            barEffect.World = Matrix.Identity;
            barEffect.View = view;
            barEffect.Projection = projection;
            barEffect.Texture = bar_text;
            barEffect.TextureEnabled = true;

            longEffect = new BasicEffect(GraphicsDevice); // 4.0change
            longEffect.World = Matrix.Identity;
            longEffect.View = view;
            longEffect.Projection = projection;
            longEffect.Texture = long_note_color;
            longEffect.TextureEnabled = true;

            long_note_color = Game.Content.Load<Texture2D>("long_note_red");
        }

        public override void ReplaceBackground(String location)
        {
            Texture2D replacement = null;
            try
            {
                if (location != null && !location.Equals(""))
                {
                    replacement = Texture2D.FromStream(RhythmGame.GameInstance.GraphicsDevice, new FileStream(location, FileMode.Open));
                    if (replacement.Width != 800 || replacement.Height != 600)
                        replacement = null;
                }
            }
            catch (Exception) {}
            if (replacement != null)
                background = replacement;
        }

        public Color GetDefaultColor(ulong idx)
        {
            switch (idx)
            {
                case 0:
                    return Color.Green;
                case 1:
                    return Color.Red;
                case 2:
                    return Color.Yellow;
                case 3:
                    return Color.Blue;
                case 4:
                    return Color.Orange;
            }
            return Color.White;
        }

        public override void Update(GameTime gameTime)
        {
            IPlayerService playerService =
                (IPlayerService)Game.Services.GetService(typeof(IPlayerService));
            NoteManager notes = playerService.Notes;

            if (notes.SongTime.TotalMilliseconds < 0)
            {
                fret_y_1 = -(6 + (float)(notes.SongTime.TotalMilliseconds / 500) % 12.0f);
                if (fret_y_1 > 0)
                    fret_y_2 = fret_y_1 - 6.0f;
                else
                    fret_y_2 = fret_y_1 + 6.0f;
            }
            else
            {
                fret_y_1 = -(float)(notes.SongTime.TotalMilliseconds / 500) % 12.0f;
                fret_y_1 += 6.0f;
                if (fret_y_1 > 0)
                    fret_y_2 = fret_y_1 - 6.0f;
                else
                    fret_y_2 = fret_y_1 + 6.0f;
            }

            SongDataIO.SongData.NoteSet[] noteSet = notes.NoteSet;

            if (noteSet != null)
            {
                for (int i = first_note; i < noteSet.Length; i++)
                {
                    SongData.NoteSet n = noteSet[i];

                    float y = (float)(n.time - notes.SongTime.TotalMilliseconds - 100) / 500f - 2.3f;

                    if (y > 3)
                        break;

                    for (ulong k = 1, trueId = 0; k <= noteSet[i].type; k *= 2, trueId++)
                    {
                        if (n.length > 0)
                        {
                            ulong id = noteSet[i].type & k;
                            if (id == 0 || trueId > 4)
                                continue;
                            id = trueId;

                            if ((n.burning & k) != 0)
                            {
                                float note_length = n.length / 500f;

                                long_pressed_vis[id] = true;

                                float x = (float)(-0.6 + id * 0.3);

                                if (y + note_length < -2.3f)
                                {
                                    n.exploding = 0L;
                                    continue;
                                }

                                float d_y = y+note_length;
                                float s_y = -2.20f;
                                long_pressed[id].Destination = new Vector3(4f * x, 4f * d_y, 0);
                                long_pressed[id].Source = new Vector3(4f * x, 4f * s_y, 0);

                                long_pressed[id].Update(gameTime);
                            }
                        }
                    }
                }
            }
            
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            IPlayerService playerService =
                (IPlayerService)Game.Services.GetService(typeof(IPlayerService));
            NoteManager notes = playerService.Notes;

            ISpriteBatchService spriteBatchService =
                (ISpriteBatchService)Game.Services.GetService(typeof(ISpriteBatchService));
            SpriteBatch spriteBatch = spriteBatchService.SpriteBatch;

            // generate the lightning stuff first
            Matrix world = Matrix.Identity * Matrix.CreateScale(0.25f) * Matrix.CreateRotationX(rotation);
            for (int i = 0; i < itemCount; i++)
            {
                if (long_pressed_vis[i])
                {
                    long_pressed[i].GenerateTexture(gameTime, world, view, projection);
                }
            }

            // Draw background
            spriteBatch.Begin(0, BlendState.AlphaBlend); // 4.0change
            spriteBatch.Draw(background, Vector2.Zero, Color.White);
            spriteBatch.End();

            // 4.0changesx3
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead; // 4.0change; esp this one
            //GraphicsDevice.VertexDeclaration = colorDeclaration; // 4.0change AND THIS ONE! Should do SetSource(vertexBuffer) ?
            
            // btftest
            DrawNeck();
            DrawBars(playerService, notes);
            DrawStrings();
            DrawNotes(notes);
            DrawHitBoxes();
            DrawLongHitNotes(playerService, spriteBatch);
        }

        private void DrawNotes(NoteManager notes)
        {
            SongDataIO.SongData.NoteSet[] noteSet = notes.NoteSet;
            if (noteSet != null)
            {
                for (int i = first_note; i < noteSet.Length; i++)
                {
                    bool exit_loop = false;
                    bool ceaseExploding = false;
                    SongData.NoteSet n = noteSet[i];

                    // TODO: Total hack for end of project... should be using isHOPO instead.
                    bool isHopoNote = (noteSet[i].type & (((ulong)1) << 5)) != 0;
                    for (ulong k = 1, trueId = 0; k <= noteSet[i].type; k *= 2, trueId++)
                    {
                        ulong id = noteSet[i].type & k;
                        if (id == 0 || trueId > 4)
                            continue;
                        id = trueId;

                        float y = (float)(n.time - notes.SongTime.TotalMilliseconds) / 500f - 2.3f;
                        float x = (float)(-0.6 + id * 0.3);

                        if (y > 3)
                        {
                            exit_loop = true;
                            break;
                        }

                        if (y < -4 && ((n.length / 500f) + y) < -4)
                        {
                            if (notes.SongTime.TotalMilliseconds > 0.0)
                                first_note = i;
                            break;
                        }

                        if (n.length > 0)
                        {
                            //GraphicsDevice.VertexDeclaration = textureDeclaration; // 4.0change
                            longEffect.World = Matrix.Identity * Matrix.CreateScale(0.1f, 1.0f, 1.0f) *
                                Matrix.CreateTranslation(x, y, 0.0f) * Matrix.CreateRotationX(rotation);

                            if ((n.burning & k) == 0 && (n.visible[id] == SongData.NoteSet.VIS_STATE.VISIBLE || !n.HasStrummed()))
                            {
                                longEffect.Texture = long_note_color;

                                float long_end = n.length / 500f;

                                // btftest
                                long_note[0] = new VertexPositionTexture(new Vector3(-1, long_end, 0.015f), new Vector2(0, 0));
                                long_note[1] = new VertexPositionTexture(new Vector3(1, long_end, 0.015f), new Vector2(1, 0));
                                long_note[2] = new VertexPositionTexture(new Vector3(-1, 0f, 0.015f), new Vector2(0, 1));
                                long_note[3] = new VertexPositionTexture(new Vector3(1, 0f, 0.015f), new Vector2(1, 1));
                                
                                // Begin effect and draw for each pass
                                //longEffect.Begin();
                                foreach (EffectPass pass in longEffect.CurrentTechnique.Passes)
                                {
                                    pass.Apply();
                                    /*GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>
                                    (PrimitiveType.TriangleFan, long_note, 0, 2);
                                    pass.End();*/
                                    // 4.0change
                                    GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>
                                        (PrimitiveType.TriangleStrip, long_note, 0, 2); // 4.0change: strip or list? Include long_note?
                                }
                                //longEffect.End();
                            }
                        }

                        if (n.visible[id] == SongData.NoteSet.VIS_STATE.INVISIBLE)
                        {
                            if ((n.exploding & k) != 0)
                            {
                                explosion.AddParticles(new Vector2(260 + 70 * id, 520), GetDefaultColor(id));
                                ceaseExploding = true;
                            }
                            continue;
                        }

                        if (isHopoNote)
                        {
                            foreach (ModelMesh mesh in hopoModels[id].Meshes)
                            {
                                foreach (BasicEffect be in mesh.Effects)
                                {
                                    be.EnableDefaultLighting();
                                    be.Projection = projection;
                                    be.View = view;
                                    be.World = Matrix.Identity * Matrix.CreateRotationX(1.6f) * mesh.ParentBone.Transform *
                                        Matrix.CreateScale(0.2f) * Matrix.CreateTranslation(x, y, 0) * Matrix.CreateRotationX(rotation);
                                }
                                mesh.Draw();
                            }
                        }
                        else
                        {
                            foreach (ModelMesh mesh in draw_notes[id].Meshes)
                            {
                                foreach (BasicEffect be in mesh.Effects)
                                {
                                    be.EnableDefaultLighting();
                                    be.Projection = projection;
                                    be.View = view;
                                    be.World = Matrix.Identity * Matrix.CreateRotationX(1.6f) * mesh.ParentBone.Transform *
                                        Matrix.CreateScale(0.2f) * Matrix.CreateTranslation(x, y, 0) * Matrix.CreateRotationX(rotation);
                                }
                                mesh.Draw();
                            }
                        }
                    }
                    if (ceaseExploding)
                        n.exploding = 0L;
                    if (exit_loop)
                        break;
                }
            }
        }

        private void DrawNeck()
        {
            
            // Create effect and set properties
            FretEffectA.World = Matrix.Identity * Matrix.CreateTranslation(0, fret_y_1, 0) * Matrix.CreateRotationX(rotation);

            // Begin effect and draw for each pass
            //FretEffectA.Begin();
            foreach (EffectPass pass in FretEffectA.CurrentTechnique.Passes)
            {
                pass.Apply(); // 4.0change
                GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>
                (PrimitiveType.TriangleStrip, fret, 0, 2); // 4.0change TriangleFan - TO DO:Strip or List?

                //pass.End();
            }
            //FretEffectA.End();
            
            
            FretEffectB.World = Matrix.Identity * Matrix.CreateTranslation(0, fret_y_2, 0.0f) * Matrix.CreateRotationX(rotation);

            // Begin effect and draw for each pass
            //FretEffectB.Begin(); // 4.0change

            foreach (EffectPass pass in FretEffectB.CurrentTechnique.Passes)
            {
                pass.Apply(); // 4.0change
                GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>
                (PrimitiveType.TriangleStrip, fret, 0, 2); // 4.0change TriangleFan - TO DO: Strip or List?
                //pass.End();
            }
            //FretEffectB.End();

            // GraphicsDevice.VertexDeclaration = textureDeclaration; // 4.0change
             
        }

        private void DrawBars(IPlayerService playerService, NoteManager notes)
        {
            //barEffect.Begin(); // 4.0change
            SongData.Barline[] bars = playerService.Notes.barlines;
            for (int i = 0; i < bars.Length; ++i)
            {
                float y = (float)(bars[i].time - notes.SongTime.TotalMilliseconds) / 500f - 2.3f; // btf MAGIC NUMBER!

                if (y < -3)
                    continue;
                if (y > 4)
                    break;

                barEffect.World = Matrix.CreateTranslation(0, y, 0) * Matrix.CreateRotationX(rotation);
                foreach (EffectPass pass in barEffect.CurrentTechnique.Passes)
                {
                    pass.Apply(); // 4.0change
                    GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>
                    (PrimitiveType.TriangleStrip, bar, 0, 2); // 4.0change TriangleFan
                    //pass.End();
                }
            }
            //barEffect.End();
        }

        private void DrawStrings()
        {
            //StringEffect.Begin(); // 4.0change

            for (int i = 0; i < 5; ++i)
            {
                float x = (float)(-0.6 + i * 0.3);
                StringEffect.World = Matrix.CreateTranslation(x, 0, 0) * Matrix.CreateRotationX(rotation);
                foreach (EffectPass pass in StringEffect.CurrentTechnique.Passes)
                {
                    pass.Apply(); // 4.0change
                    GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>
                    (PrimitiveType.TriangleStrip, guitar_string, 0, 2); // 4.0change TriangleFan - TO DO: Strip or List?
                    //pass.End();
                }
            }
            //StringEffect.End();
        }

        private void DrawLongHitNotes(IPlayerService playerService, SpriteBatch spriteBatch)
        {
            Rectangle rect = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            for (int i = 0; i < itemCount; i++)
            {
                if (long_pressed_vis[i])
                {
                    spriteBatch.Draw(long_pressed[i].LightningTexture, rect, Color.White);

                    long_pressed_vis[i] = false;
                }
            }
            spriteBatch.End();

            InputManager input = playerService.Input;
            ulong held = input.FullHeld;

            for (ulong k = 1, trueId = 0; k <= held; k *= 2, trueId++)
            {
                ulong id = held & k;
                if (id == 0 || trueId > 4)
                    continue;
                id = trueId;
                draw_pressed[id] = true;
            }
        }

        private void DrawHitBoxes()
        {
            for (int i = 0; i < 5; ++i)
            {
                float y = (float)-2.35f;
                float x = (float)(-0.6 + i * 0.3);

                Model temp = draw_pressed[i] ? pickups[i] : pickup;

                foreach (ModelMesh mesh in temp.Meshes)
                {
                    foreach (BasicEffect be in mesh.Effects)
                    {
                        be.EnableDefaultLighting();
                        be.Projection = projection;
                        be.View = view;
                        be.World = Matrix.Identity * Matrix.CreateRotationX(1.6f) * mesh.ParentBone.Transform *
                            Matrix.CreateScale(0.1f) * Matrix.CreateTranslation(x, y, 0.1f) * Matrix.CreateRotationX(rotation);
                    }
                    mesh.Draw();
                }
                draw_pressed[i] = false;
            }
        }
    }
}
