using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using SetTheory;

using Rnd = UnityEngine.Random;

public class SetTheoryScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] ButtonSels;
    public KMSelectable SubmitSel;
    public Texture[] SetTexturesUnsel;
    public Texture[] SetTexturesSel;
    public MeshRenderer[] ButtonRends;
    public TextMesh Display;
    public MeshRenderer[] StageArrows;
    public Material[] ArrowMats;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _buttonsLocked;
    private bool _moduleSolved;

    private SetSymbol[] _buttonSymbols;
    private SetSymbol[] _setASymbols;
    private SetSymbol[] _setBSymbols;
    private SetSymbol[] _setCSymbols;

    private EquationTerm[] _equations;
    private SetSymbol[][] _solutions;
    private int _stage;
    private HashSet<SetSymbol> _curInput = new HashSet<SetSymbol>();

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);
        SubmitSel.OnInteract += SubmitPress;

        _buttonSymbols = (SetSymbol[])Enum.GetValues(typeof(SetSymbol)).Shuffle();
        for (int i = 0; i < _buttonSymbols.Length; i++)
            ButtonRends[i].material.mainTexture = SetTexturesUnsel[(int)_buttonSymbols[i]];

        Debug.LogFormat("[S.E.T. Theory #{0}] The symbols on the modules are: {1}.", _moduleId, _buttonSymbols.Join(", "));

        // Set A
        _setASymbols = _buttonSymbols.Where(i => SetAValidity(i)).ToArray();
        Debug.LogFormat("[S.E.T. Theory #{0}] Symbols present in Set A: {1}", _moduleId, _setASymbols.Join(", "));

        // Set B
        var setBPositions = new SetSymbol[] { SetSymbol.H, SetSymbol.Pacman, SetSymbol.Arrow, SetSymbol.Diamond, SetSymbol.X, SetSymbol.Star, SetSymbol.Teepee, SetSymbol.Triangle, SetSymbol.Shirt };
        var validSetBPartOne = Enumerable.Range(0, 9).Select(i => _buttonSymbols[i] == setBPositions[i]).ToArray();
        var setBSnChars = new string[] { "WO4", "BS3", "HA1", "TR5", "CN2", "EX9", "LU7", "FI6", "PJ8" };
        var validSetBPartTwo = setBSnChars.Select(i => i.All(j => !BombInfo.GetSerialNumber().Contains(j))).ToArray();

        _setBSymbols = Enumerable.Range(0, 9).Where(i => validSetBPartOne[i] != validSetBPartTwo[i]).Select(i => _buttonSymbols[i]).ToArray();
        Debug.LogFormat("[S.E.T. Theory #{0}] Symbols present in Set B: {1}", _moduleId, _setBSymbols.Join(", "));

        // Set C
        var listC = new List<SetSymbol>();
        int index = (int)_buttonSymbols[8];
        int x = index % 3;
        int y = index / 3;
        listC.Add(_buttonSymbols[8]);
        for (int i = 7; i >= 0; i--)
        {
            int ix = (int)_buttonSymbols[i];
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

        _equations = Enumerable.Range(0, 4).Select(GenerateEquationForStage).ToArray();
        var sets = new[] { _setASymbols, _setBSymbols, _setCSymbols };
        _solutions = _equations.Select(eq => eq.Evaluate(sets)).ToArray();

        for (var stage = 0; stage < 4; stage++)
        {
            Debug.LogFormat("[S.E.T. Theory #{0}] Stage {1} equation: {2}", _moduleId, stage + 1, _equations[stage]);
            Debug.LogFormat("[S.E.T. Theory #{0}] Stage {1} solution: {2}", _moduleId, stage + 1, _solutions[stage].Join(", "));
        }

        DisplayStage(_stage);
    }

    private void DisplayStage(int stage)
    {
        Display.text = _equations[stage].ToString();
        Display.fontSize = stage < 2 ? 150 : stage == 2 ? 110 : 75;
        for (var btn = 0; btn < 9; btn++)
            ButtonRends[btn].material.mainTexture = _curInput.Contains(_buttonSymbols[btn]) ? SetTexturesSel[(int)_buttonSymbols[btn]] : SetTexturesUnsel[(int)_buttonSymbols[btn]];
    }

    private bool SubmitPress()
    {
        SubmitSel.AddInteractionPunch(0.5f);
        Audio.PlaySoundAtTransform("Stamp", SubmitSel.transform);
        if (_buttonsLocked)
            return false;
        if (_curInput.SetEquals(_solutions[_stage]))
        {
            StageArrows[_stage].sharedMaterial = ArrowMats[0];
            Debug.LogFormat("[S.E.T. Theory #{0}] Stage {1} solved.", _moduleId, _stage + 1);
            _stage++;
            if (_stage == 4)
            {
                _buttonsLocked = true;
                StartCoroutine(ArrowAnimation());
                StartCoroutine(TextAnimation());
                Debug.LogFormat("[S.E.T. Theory #{0}] Module solved.", _moduleId);
            }
            else
            {
                _curInput.Clear();
                DisplayStage(_stage);
            }
        }
        else
        {
            Module.HandleStrike();
            Debug.LogFormat("[S.E.T. Theory #{0}] In stage {1}, you submitted {2}. Strike!", _moduleId, _stage + 1, _curInput.Join(", "));
        }
        return false;
    }

    private IEnumerator ArrowAnimation()
    {
        yield return new WaitForSeconds(0.4f);
        for (int i = 0; i < 9; i++)
            ButtonRends[i].material.mainTexture = SetTexturesUnsel[(int)_buttonSymbols[i]];
        for (int i = 0; i < 4; i++)
        {
            StageArrows[i].sharedMaterial = ArrowMats[1];
            Audio.PlaySoundAtTransform("Stamp", transform);
            yield return new WaitForSeconds(0.4f);
        }
        Audio.PlaySoundAtTransform("Chime", transform);
        Module.HandlePass();
        _moduleSolved = true;
    }

    private IEnumerator TextAnimation()
    {
        var str = Display.text;
        while (str.Length != 0)
        {
            str = str.Remove(Rnd.Range(0, str.Length), 1);
            Display.text = str;
            yield return new WaitForSeconds(0.05f);
        }
        Display.fontSize = 120;
        for (int i = 0; i < 6; i++)
        {
            Display.text = "SOLVED".Substring(0, i + 1);
            yield return new WaitForSeconds(0.2f);
        }
    }

    private KMSelectable.OnInteractHandler ButtonPress(int i)
    {
        return delegate ()
        {
            ButtonSels[i].AddInteractionPunch(0.5f);
            if (_buttonsLocked)
                return false;

            var symbolPressed = _buttonSymbols[i];
            if (_curInput.Contains(symbolPressed))
            {
                _curInput.Remove(symbolPressed);
                Audio.PlaySoundAtTransform("Deselect", transform);
            }
            else
            {
                _curInput.Add(symbolPressed);
                Audio.PlaySoundAtTransform("Stamp", transform);
            }
            DisplayStage(_stage);
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

    private EquationTerm GenerateEquationForStage(int stage)
    {
        var variables = new[] { 0, 1, 2 }
            .Select(i => (EquationTerm)new EquationTermVariable(i))
            .ToArray()
            .Shuffle();

        switch (stage)
        {
            case 0:
                // Two different variables, one operation that is not Complement
                return GenerateSubequation(useComplement: false, terms: variables);

            case 1:
                // Two different variables, one operation that is not Complement, but a variable or the whole thing might be Complemented
                return GenerateSubequation(useComplement: true, terms: variables);

            case 2:
                // All three variables, two operations, possibly randomly complemented
                var subeq = GenerateSubequation(useComplement: true, terms: variables);
                return GenerateSubequation(useComplement: true, terms: new[] { subeq, variables[2] });

            case 3:
                // All three variables plus a duplicate, three operations, possibly randomly complemented
                var subeq1 = GenerateSubequation(useComplement: true, terms: variables);
                var subeq2 = GenerateSubequation(useComplement: true, terms: new[] { variables[2], variables[Rnd.Range(0, 2)] });
                return GenerateSubequation(useComplement: true, terms: new[] { subeq1, subeq2 });

            default: throw new InvalidOperationException("Invalid stage.");
        }
    }

    private static EquationTerm GenerateSubequation(bool useComplement, EquationTerm[] terms)
    {
        var op = ((SetOperation[])Enum.GetValues(typeof(SetOperation))).Except(new[] { SetOperation.Complement }).PickRandom();
        var swapped = Rnd.Range(0, 2) != 0;
        var left = swapped ? terms[1] : terms[0];
        var right = swapped ? terms[0] : terms[1];
        return new EquationTermOperation(op, useComplement ? RandomlyComplement(left) : left, useComplement ? RandomlyComplement(right) : right);
    }

    private static EquationTerm RandomlyComplement(EquationTerm eq)
    {
        return Rnd.Range(0, 2) != 0 ? new EquationTermOperation(SetOperation.Complement, eq) : eq;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} select pacman x triangle teepee shirt arrow diamond h star [Select these shapes.] | !{0} submit [Press the submit button.]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            SubmitSel.OnInteract();
            yield break;
        }
        var parameters = command.Split(' ');
        if (parameters.Length < 2 || parameters[0] != "select")
            yield break;
        var thingsToToggle = new List<int>();
        for (int i = 1; i < parameters.Length; i++)
        {
            var shapes = new string[] { "pacman", "x", "triangle", "teepee", "shirt", "arrow", "diamond", "h", "star" };
            var ix = Array.IndexOf(shapes, parameters[i]);
            if (ix == -1)
                yield break;
            thingsToToggle.Add(ix);
        }
        yield return null;
        for (int i = 0; i < 9; i++)
        {
            if ((thingsToToggle.Contains((int)_buttonSymbols[i]) && !_curInput.Contains(_buttonSymbols[i])) || (!thingsToToggle.Contains((int)_buttonSymbols[i]) && _curInput.Contains(_buttonSymbols[i])))
            {
                ButtonSels[i].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        for (int st = _stage; st < 4; st++)
        {
            for (int i = 0; i < 9; i++)
            {
                if ((_solutions[st].Contains(_buttonSymbols[i]) && !_curInput.Contains(_buttonSymbols[i]))|| (!_solutions[st].Contains(_buttonSymbols[i]) && _curInput.Contains(_buttonSymbols[i])))
                {
                    ButtonSels[i].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            SubmitSel.OnInteract();
            yield return new WaitForSeconds(0.2f);
        }
        while (!_moduleSolved)
            yield return true;
    }
}
