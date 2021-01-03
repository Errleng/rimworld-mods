using System;

using RimWorld;
using UnityEngine;

namespace Rimternet
{

    public class MainButton_HelpMenuDef : MainButtonDef
    {

        public Vector2 windowSize = new Vector2(MainTabWindow_ModHelp.MinWidth, MainTabWindow_ModHelp.MinHeight);
        public float listWidth = MainTabWindow_ModHelp.MinListWidth;
        public bool pauseGame = false;
    }
}
