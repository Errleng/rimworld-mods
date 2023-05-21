using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace RimMisc
{
    internal class RimMiscWorldComponent : WorldComponent
    {
        public static readonly int AUTO_CLOSE_LETTERS_CHECK_TICKS = GenTicks.SecondsToTicks(10);
        public static readonly int KILL_DOWNED_PAWNS_TICKS = GenTicks.SecondsToTicks(60);

        private static readonly Dictionary<Letter, int> letterStartTimes = new Dictionary<Letter, int>();

        public RimMiscWorldComponent(World world) : base(world)
        {
            RimMisc.Settings.ApplySettings();
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            var currentTicks = Find.TickManager.TicksGame;
            if (RimMisc.Settings.autoCloseLetters)
            {
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
            if (RimMisc.Settings.killDownedPawns)
            {
                if (currentTicks % KILL_DOWNED_PAWNS_TICKS == 0)
                {
                    var pawns = PawnsFinder.AllMapsAndWorld_Alive;
                    int count = 0;
                    foreach (var pawn in pawns)
                    {
                        if (pawn.Faction != null &&
                            !pawn.Faction.IsPlayer &&
                            !pawn.IsPrisonerOfColony &&
                            pawn.Faction.HostileTo(Faction.OfPlayer) &&
                            pawn.Downed
                            )
                        {
                            pawn.Kill(null);
                            ++count;
                        }
                    }

                    if (count > 0)
                    {
                        Log.Message($"Killed {count} downed hostile pawns");
                    }
                }
            }
        }
    }
}