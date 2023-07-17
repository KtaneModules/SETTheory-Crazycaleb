using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using NUnit.Framework.Constraints;

public class SetTheoryScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] Buttonage;
    public KMSelectable submit;
    public Sprite[] set;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
    }
}
