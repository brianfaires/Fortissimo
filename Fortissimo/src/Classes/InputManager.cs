// Description  : This provides an abstract class that is intended to 
//                overloaded by different input devices.

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
using System.Collections;
#endregion

namespace Fortissimo
{
    public enum OtherKeyType { Up = 0, Down, Left, Right, Select, Cancel, Pause, Power, EndType }
    public abstract class InputManager : GameComponent
    {
        protected ulong _fullPressed;
        protected ulong _fullHeld;
        protected bool _strummed = false;

        protected static int _keyRange;

        public InputManager(Game game)
            : base(game)
        {
        }

        /// <summary>
        /// 64 bit mask of all new notes.
        /// </summary>
        public ulong FullPressed { get { return _fullPressed; } }

        /// <summary>
        /// 64 bit mask of notes continued being held.
        /// </summary>
        public ulong FullHeld { get { return _fullHeld; } }

        /// <summary>
        /// Has the player strummer the instrument.
        /// </summary>
        public bool Strummed { get { return _strummed; } }

        /// <summary>
        /// The valid range for this Input device.
        /// </summary>
        public static int KeyRange { get { return _keyRange; } }
        public abstract int KeyRangeForType();

        /// <summary>
        /// The Input device should map to the same key ids that the
        /// note manager does.
        /// </summary>
        public abstract bool KeyPressed(int keyID);
        public abstract bool KeyHeld(int keyID);

        /// <summary>
        /// Other mapped keys.  These keys are used primary for menu navigation.
        /// </summary>
        public abstract bool OtherKeyPressed(OtherKeyType type);
        public abstract bool OtherKeyHeld(OtherKeyType type);
    }

    // The main goal of this class is to map the correct keys to 
    // notes.
    public class ASDFGInput : InputManager
    {
        KeyboardState keyboardState;
        Keys[] pressedKeys;

        KeyState[] states;
        KeyState[] otherStates;

        enum KeyState { NONE = 0, PRESSED, HELD }

        ExplosionParticleSystem explosion;

        public ASDFGInput(Game game)
            : base(game)
        {
            _keyRange = 5;
            keyboardState = new KeyboardState();
            states = new KeyState[_keyRange];
            otherStates = new KeyState[(int)OtherKeyType.EndType];

            explosion = new ExplosionParticleSystem(game, 100);
            game.Components.Add(explosion);
        }

        public override int KeyRangeForType()
        {
            return ASDFGInput.KeyRange;
        }

        private int KeyToID(Keys k)
        {
            switch (k)
            {
                case Keys.A:
                    return 0;
                case Keys.S:
                    return 1;
                case Keys.D:
                    return 2;
                case Keys.F:
                    return 3;
                case Keys.G:
                    return 4;
            }
            return -1;
        }

        private OtherKeyType OtherKeyToID(Keys k)
        {
            switch (k)
            {
                case Keys.Space:
                    return OtherKeyType.Select;
                case Keys.Escape:
                    return OtherKeyType.Cancel;
                case Keys.Up:
                    return OtherKeyType.Up;
                case Keys.Down:
                    return OtherKeyType.Down;
                case Keys.Left:
                    return OtherKeyType.Left;
                case Keys.Right:
                    return OtherKeyType.Right;
                case Keys.R:
                    return OtherKeyType.Power;
            }

            return OtherKeyType.EndType;
        }

        bool IsStrum(Keys key)
        {
            return key == Keys.Up || key == Keys.Down || key == Keys.Space; // btftest
        }

        public override bool KeyPressed(int keyID)
        {
            return states[keyID] == KeyState.PRESSED;
        }

        public override bool KeyHeld(int keyID)
        {
            return states[keyID] == KeyState.HELD;
        }

        public override bool OtherKeyPressed(OtherKeyType type)
        {
            return otherStates[(int)type] == KeyState.PRESSED;
        }

        public override bool OtherKeyHeld(OtherKeyType type)
        {
            return otherStates[(int)type] == KeyState.HELD;
        }

        public bool fake = true;
        Keys[] fakeKeys = new Keys[0];
        public void UpdateFakeKeys(Keys[] keys)
        {
            fakeKeys = keys;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            keyboardState = Keyboard.GetState();
            Keys[] newKeys = (Keys[])keyboardState.GetPressedKeys();

            if (fake && (fakeKeys.Length > 0))
            {
                int index = 0;
                Keys[] tempKeys = new Keys[(newKeys.Length + fakeKeys.Length)];
                foreach (Keys k in newKeys)
                {
                    tempKeys[index++] = k;
                }

                foreach (Keys k in fakeKeys)
                {
                    tempKeys[index++] = k;
                }

                newKeys = (Keys[])tempKeys.Clone();
                fakeKeys = new Keys[0];
            }

            for (int i = 0; i < states.Length; i++)
                    states[i] = KeyState.NONE;
            for (int i = 0; i < otherStates.Length; i++)
                otherStates[i] = KeyState.NONE;
            _fullPressed = 0UL;
            _fullHeld = 0UL;
            _strummed = false;

            //if (GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed)
                //return OtherKeyType.Select;
            if (pressedKeys != null || GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed)
            {
                foreach (Keys key in newKeys)
                {
                    bool found = false;

                    foreach (Keys oldKey in pressedKeys)
                    {
                        if (key == oldKey)
                        {
                            found = true;
                            break;
                        }
                    }

                    int id = KeyToID(key);
                    if (id != -1)
                    {
                        if (found)
                        {
                            _fullHeld |= 1UL << id;
                            states[id] = KeyState.HELD;
                        }
                        else
                        {
                            _fullPressed |= 1UL << id;
                            states[id] = KeyState.PRESSED;
                        }
                    }

                    OtherKeyType oid = OtherKeyToID(key);
                    if (oid != OtherKeyType.EndType)
                    {
                        if (found)
                            otherStates[(int)oid] = KeyState.HELD;
                        else
                            otherStates[(int)oid] = KeyState.PRESSED;
                        if (oid == OtherKeyType.Cancel)
                            otherStates[(int)OtherKeyType.Pause] = otherStates[(int)OtherKeyType.Cancel];
                        if (oid == OtherKeyType.Select)
                            ((RhythmGame)Game).ActiveInput = this;
                    }

                    if (IsStrum(key))
                        _strummed = !found;
                }
            }

            pressedKeys = newKeys;
        }
    }

    public class GuitarInput : InputManager
    {
        GamePadState gamepadState;
        ButtonState[] pressedButtons;
        ButtonState[] pressedOthers;
        ButtonState[] pressedStrum;

        KeyState[] states;
        KeyState[] otherStates;

        enum KeyState { NONE = 0, PRESSED, HELD }

        ExplosionParticleSystem explosion;

        public GuitarInput(Game game)
            : base(game)
        {
            _keyRange = 5;
            gamepadState = new GamePadState();
            pressedButtons = new ButtonState[_keyRange];
            pressedOthers = new ButtonState[(int)OtherKeyType.EndType];
            pressedStrum = new ButtonState[2];

            states = new KeyState[_keyRange];
            otherStates = new KeyState[(int)OtherKeyType.EndType];

            explosion = new ExplosionParticleSystem(game, 100);
            game.Components.Add(explosion);
        }

        public override int KeyRangeForType()
        {
            return GuitarInput.KeyRange;
        }

        bool IsStrum(Keys key)
        {
            return key == Keys.Up || key == Keys.Down;
        }

        public override bool KeyPressed(int keyID)
        {
            return states[keyID] == KeyState.PRESSED;
        }

        public override bool KeyHeld(int keyID)
        {
            return states[keyID] == KeyState.HELD;
        }

        public override bool OtherKeyPressed(OtherKeyType type)
        {
            return otherStates[(int)type] == KeyState.PRESSED;
        }

        public override bool OtherKeyHeld(OtherKeyType type)
        {
            return otherStates[(int)type] == KeyState.HELD;
        }

        public void StoreKeyValue(int idx, int otherIdx, bool isStrum, ButtonState state)
        {
            if ( idx != -1 && !isStrum )
            {
                if (state == ButtonState.Pressed)
                {
                    if (pressedButtons[idx] == ButtonState.Pressed)
                    {
                        _fullHeld |= 1UL << idx;
                        states[idx] = KeyState.HELD;
                    }
                    else
                    {
                        _fullPressed |= 1UL << idx;
                        states[idx] = KeyState.PRESSED;
                    }
                }
                pressedButtons[idx] = state;
            }

            if ( otherIdx != -1 )
            {
                if (state == ButtonState.Pressed)
                {
                    if (pressedOthers[otherIdx] == ButtonState.Pressed)
                        otherStates[otherIdx] = KeyState.HELD;
                    else
                        otherStates[otherIdx] = KeyState.PRESSED;
                }
                pressedOthers[otherIdx] = state;
            }

            if (isStrum)
            {
                if (state == ButtonState.Pressed)
                {
                    if (pressedStrum[idx] != ButtonState.Pressed)
                        _strummed = true;
                }
                pressedStrum[idx] = state;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.gamepadState = GamePad.GetState(PlayerIndex.One);

            for (int i = 0; i < states.Length; i++)
                states[i] = KeyState.NONE;
            for (int i = 0; i < otherStates.Length; i++)
                otherStates[i] = KeyState.NONE;

            _fullPressed = 0UL;
            _fullHeld = 0UL;
            _strummed = false;

            StoreKeyValue(0, (int)OtherKeyType.Select, false, gamepadState.Buttons.A);
            StoreKeyValue(1, (int)OtherKeyType.Cancel, false, gamepadState.Buttons.B);
            StoreKeyValue(2, -1, false, gamepadState.Buttons.Y);
            StoreKeyValue(3, -1, false, gamepadState.Buttons.X);
            StoreKeyValue(4, -1, false, gamepadState.Buttons.LeftShoulder);

            if (otherStates[(int)OtherKeyType.Select] == KeyState.PRESSED)
                ((RhythmGame)Game).ActiveInput = this;

            StoreKeyValue(0, (int)OtherKeyType.Down, true, gamepadState.DPad.Down);
            StoreKeyValue(1, (int)OtherKeyType.Up, true, gamepadState.DPad.Up);
            StoreKeyValue(-1, (int)OtherKeyType.Left, false, gamepadState.DPad.Down);
            StoreKeyValue(-1, (int)OtherKeyType.Right, false, gamepadState.DPad.Up);
            StoreKeyValue(-1, (int)OtherKeyType.Pause, false, gamepadState.Buttons.Start);
            //StoreKeyValue(-1, (int)OtherKeyType.Power, false, gamepadState.Buttons.LeftStick);
            if (gamepadState.ThumbSticks.Right.Y > .6f)
            {
                if (pressedOthers[(int)OtherKeyType.Power] == ButtonState.Pressed)
                    otherStates[(int)OtherKeyType.Power] = KeyState.HELD;
                else
                    otherStates[(int)OtherKeyType.Power] = KeyState.PRESSED;
                pressedOthers[(int)OtherKeyType.Power] = ButtonState.Pressed;
            }
            else
                pressedOthers[(int)OtherKeyType.Power] = ButtonState.Released;

        }
    }
}
