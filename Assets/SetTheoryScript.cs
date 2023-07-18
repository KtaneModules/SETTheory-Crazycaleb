using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

public class SetTheoryScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] ButtonSels;
    public KMSelectable SubmitSel;
    public Sprite[] SetSprites;
    public SpriteRenderer[] ButtonSprites;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    public enum SetSymbol
    {
        Pacman,
        X,
        Triangle,
        Teepee,
        Shirt,
        Arrow,
        Diamond,
        H,
        Star
    }

    public enum SetOperations
    {
        Union,
        Intersection,
        Difference,
        SymmetricDifference,
        Complement
    }

    private SetSymbol[] _buttonSprites;

    private SetSymbol[] _setASymbols;
    private SetSymbol[] _setBSymbols;
    private SetSymbol[] _setCSymbols;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);
        SubmitSel.OnInteract += SubmitPress;

        _buttonSprites = (SetSymbol[])Enum.GetValues(typeof(SetSymbol)).Shuffle();
        for (int i = 0; i < _buttonSprites.Length; i++)
            ButtonSprites[i].sprite = SetSprites[(int)_buttonSprites[i]];

        // Set A
        _setASymbols = _buttonSprites.Where(i => SetAValidity(i)).ToArray();
        Debug.LogFormat("[S.E.T. Theory #{0}] Symbols present in Set A: {1}", _moduleId, _setASymbols.Join(", "));

        // Set B
        var setBPositions = new SetSymbol[] { SetSymbol.H, SetSymbol.Pacman, SetSymbol.Arrow, SetSymbol.Diamond, SetSymbol.X, SetSymbol.Star, SetSymbol.Teepee, SetSymbol.Triangle, SetSymbol.Shirt };
        var validSetBPartOne = Enumerable.Range(0, 9).Select(i => _buttonSprites[i] == setBPositions[i]).ToArray();
        var setBSnChars = new string[] { "WO4", "BS3", "HA1", "TR5", "CN2", "EX9", "LU7", "FI6", "PJ8" };
        var validSetBPartTwo = setBSnChars.Select(i => i.All(j => !BombInfo.GetSerialNumber().Contains(j))).ToArray();

        _setBSymbols = Enumerable.Range(0, 9).Where(i => validSetBPartOne[i] != validSetBPartTwo[i]).Select(i => _buttonSprites[i]).ToArray();
        Debug.LogFormat("[S.E.T. Theory #{0}] Symbols present in Set B: {1}", _moduleId, _setBSymbols.Join(", "));

        // Set C
        var listC = new List<SetSymbol>();
        int index = (int)_buttonSprites[8];
        int x = index % 3;
        int y = index / 3;
        listC.Add(_buttonSprites[8]);
        for (int i = 7; i >= 0; i--)
        {
            int ix = (int)_buttonSprites[i];
            if (ix == 4)
                continue;
            int dx = (ix % 3) - 1;
            int dy = (ix / 3) - 1;
            x = (x + dx + 3) % 3;
            y = (y + dy + 3) % 3;
            SetSymbol val = (SetSymbol)(y * 3 + x);
            if (listC.Contains(val))
                break;
            listC.Add(val);
        }
        _setCSymbols = listC.ToArray();
        Debug.LogFormat("[S.E.T. Theory #{0}] Symbols present in Set C: {1}", _moduleId, _setCSymbols.Join(", "));
    }

    private bool SubmitPress()
    {
        // do more stuff
        return false;
    }

    private KMSelectable.OnInteractHandler ButtonPress(int i)
    {
        return delegate ()
        {
            // do stuff
            return false;
        };
    }

    private bool SetAValidity(SetSymbol symbol)
    {
        var mods = BombInfo.GetModuleNames().Select(i => i.ToUpperInvariant());
        switch (symbol)
        {
            case SetSymbol.Pacman: return BombInfo.GetBatteryCount() >= 3;
            case SetSymbol.X: return mods.Count(i => i.Contains("FORGET")) > 0;
            case SetSymbol.Triangle: return BombInfo.GetSerialNumberNumbers().Sum() % 3 == 0;
            case SetSymbol.Teepee: return new[] { 2, 3, 5, 7 }.Contains(BombInfo.GetSerialNumber()[5] - '0');
            case SetSymbol.Shirt: return BombInfo.GetPortPlates().Any(pp => pp.Length == 0);
            case SetSymbol.Arrow: return mods.Count(i => i.Contains("ARROWS")) > 0;
            case SetSymbol.Diamond: return BombInfo.GetModuleNames().Count() - BombInfo.GetSolvableModuleNames().Count > 0;
            case SetSymbol.H: return BombInfo.GetSerialNumber().Any(i => "SET".Contains(i));
            case SetSymbol.Star: return BombInfo.GetOffIndicators().Count() == 0;
        }
        return false;
    }

    private SetSymbol[] CalculateOperation(SetOperations op, SetSymbol[] a, SetSymbol[] b = null)
    {
        switch (op)
        {
            case SetOperations.Union:
                return a.Union(b).ToArray();
            case SetOperations.Intersection:
                return a.Intersect(b).ToArray();
            case SetOperations.Difference:
                return a.Except(b).ToArray();
            case SetOperations.SymmetricDifference:
                return ((SetSymbol[])Enum.GetValues(typeof(SetSymbol))).Where(i => a.Contains(i) != b.Contains(i)).ToArray();
            case SetOperations.Complement:
                return ((SetSymbol[])Enum.GetValues(typeof(SetSymbol))).Where(i => !a.Contains(i)).ToArray();
            default:
                throw new InvalidOperationException("Invalid set operation.");
        }
    }
}
