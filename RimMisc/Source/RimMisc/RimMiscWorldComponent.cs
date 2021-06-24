using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace RimMisc
{
    class RimMiscWorldComponent : WorldComponent
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
                int currentTicks = Find.TickManager.TicksGame;
                if (currentTicks % AUTO_CLOSE_LETTERS_CHECK_TICKS == 0)
                {
                    List<Letter> letters = Find.LetterStack.LettersListForReading;

                    for (int i = letters.Count - 1; i >= 0; i--)
                    {
                        Letter letter = letters[i];
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
