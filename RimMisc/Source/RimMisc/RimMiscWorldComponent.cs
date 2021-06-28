using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RimMisc
{
    internal class RimMiscWorldComponent : WorldComponent
    {
        public static readonly int AUTO_CLOSE_LETTERS_CHECK_TICKS = GenTicks.SecondsToTicks(10);

        private static readonly Dictionary<Letter, int> letterStartTimes = new Dictionary<Letter, int>();

        public RimMiscWorldComponent(World world) : base(world)
        {
            RimMisc.Settings.ApplySettings();
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (RimMisc.Settings.autoCloseLetters)
            {
                var currentTicks = Find.TickManager.TicksGame;
                if (currentTicks % AUTO_CLOSE_LETTERS_CHECK_TICKS == 0)
                {
                    var letters = Find.LetterStack.LettersListForReading;

                    for (var i = letters.Count - 1; i >= 0; i--)
                    {
                        var letter = letters[i];
                        if (!letterStartTimes.ContainsKey(letter))
                        {
                            letterStartTimes.Add(letter, currentTicks);
                        }
                        else
                        {
                            if (letterStartTimes[letter] + RimMisc.Settings.autoCloseLettersSeconds.SecondsToTicks() < currentTicks)
                            {
                                Find.LetterStack.RemoveLetter(letter);
                                letterStartTimes.Remove(letter);
                            }
                        }
                    }
                }
            }
        }
    }
}