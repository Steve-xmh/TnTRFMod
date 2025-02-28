using UnityEngine.InputSystem;

namespace TnTRFMod.Utils;

public class KeyConversion
{
    private static readonly Dictionary<char, Key> chartoKey = new()
    {
        //-------------------------LOGICAL mappings-------------------------

        //Lower Case Letters
        { 'a', Key.A },
        { 'b', Key.B },
        { 'c', Key.C },
        { 'd', Key.D },
        { 'e', Key.E },
        { 'f', Key.F },
        { 'g', Key.G },
        { 'h', Key.H },
        { 'i', Key.I },
        { 'j', Key.J },
        { 'k', Key.K },
        { 'l', Key.L },
        { 'm', Key.M },
        { 'n', Key.N },
        { 'o', Key.O },
        { 'p', Key.P },
        { 'q', Key.Q },
        { 'r', Key.R },
        { 's', Key.S },
        { 't', Key.T },
        { 'u', Key.U },
        { 'v', Key.V },
        { 'w', Key.W },
        { 'x', Key.X },
        { 'y', Key.Y },
        { 'z', Key.Z },


        //KeyPad Numbers
        { '1', Key.Digit1 },
        { '2', Key.Digit2 },
        { '3', Key.Digit3 },
        { '4', Key.Digit4 },
        { '5', Key.Digit5 },
        { '6', Key.Digit6 },
        { '7', Key.Digit7 },
        { '8', Key.Digit8 },
        { '9', Key.Digit9 },
        { '0', Key.Digit0 },

        //Other Symbols
        { '\'', Key.Quote },
        { ',', Key.Comma },
        { '-', Key.Minus },
        { '.', Key.Period },
        { '/', Key.Slash },
        { ';', Key.Semicolon },
        { '=', Key.Equals },
        { '[', Key.LeftBracket },
        { '\\', Key.Backslash }, //remember the special forward slash rule... this isnt wrong
        { ']', Key.RightBracket }
    };

    public static Key CharToKey(char c)
    {
        return chartoKey.GetValueOrDefault(char.ToLower(c), Key.None);
    }
}