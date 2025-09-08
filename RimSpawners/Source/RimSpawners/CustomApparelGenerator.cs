using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimSpawners
{
    internal class CustomApparelGenerator
    {
        private static readonly int MAX_ATTEMPTS = 100;

        private List<ThingStuffPair> aps = new List<ThingStuffPair>();
        private HashSet<ApparelUtility.LayerGroupPair> lgps = new HashSet<ApparelUtility.LayerGroupPair>();
        private BodyDef body;
        private ThingDef raceDef;
        private Pawn pawn;

        public CustomApparelGenerator(Pawn pawn)
        {
            Reset(pawn);
        }


        public void GenerateApparelFromPool(List<ThingStuffPair> apparelCandidates)
        {
            var validApparelCandidates = apparelCandidates.Where(x => ApparelUtility.HasPartsToWear(pawn, x.thing)).ToList();
            var money = pawn.kindDef.apparelMoney.RandomInRange;
            int numAttempts = 0;
            while (numAttempts < MAX_ATTEMPTS)
            {
                numAttempts++;
                GeneratePossibleWorkingSet(validApparelCandidates, money);
                if (!Covers(BodyPartGroupDefOf.Torso) || !Covers(BodyPartGroupDefOf.Legs))
                {
                    //Log.Message(string.Format($"RimSpawners: {pawn} generated with ${money} without torso or legs coverage, retrying. Attempt {numAttempts}/{MAX_ATTEMPTS}"));
                    continue;
                }
                break;
            }
            if (numAttempts == MAX_ATTEMPTS)
            {
                Log.Warning($"RimSpawners: {pawn} failed to generate apparel after {MAX_ATTEMPTS} attempts");
            }

            for (int i = 0; i < aps.Count; i++)
            {
                Apparel apparel = (Apparel)ThingMaker.MakeThing(aps[i].thing, aps[i].stuff);
                PawnGenerator.PostProcessGeneratedGear(apparel, pawn);
                if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                {
                    pawn.apparel.Wear(apparel, false, false);
                }
            }

            for (int j = 0; j < aps.Count; j++)
            {
                for (int k = 0; k < aps.Count; k++)
                {
                    if (j != k && !ApparelUtility.CanWearTogether(aps[j].thing, aps[k].thing, pawn.RaceProps.body))
                    {
                        Log.Error(string.Format("RimSpawners: {0} generated with apparel that cannot be worn together: {1}, {2}", pawn, aps[j], aps[k]));
                        return;
                    }
                }
            }
        }

        private void GeneratePossibleWorkingSet(List<ThingStuffPair> apparelCandidates, float money)
        {
            Reset(pawn);
            var usableApparel = apparelCandidates.Where(x => !IsApparelOverlapping(x)).ToList();
            while (true)
            {
                if (Rand.Value < 0.1f && money < 9999999f)
                {
                    break;
                }
                ThingStuffPair thingStuffPair;
                if (!usableApparel.TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality, out thingStuffPair))
                {
                    break;
                }
                AddToWorkingSet(thingStuffPair);
                money -= thingStuffPair.Price;
                for (int k = usableApparel.Count - 1; k >= 0; k--)
                {
                    if (usableApparel[k].Price > money || IsApparelOverlapping(usableApparel[k]))
                    {
                        usableApparel.RemoveAt(k);
                    }
                }
            }
        }

        private void Reset(Pawn pawn)
        {
            aps.Clear();
            lgps.Clear();
            this.pawn = pawn;
            BodyDef bodyDef;
            if (pawn == null)
            {
                bodyDef = null;
            }
            else
            {
                RaceProperties raceProps = pawn.RaceProps;
                bodyDef = ((raceProps != null) ? raceProps.body : null);
            }
            body = bodyDef;
            raceDef = (pawn != null) ? pawn.def : null;
        }

        private bool IsApparelOverlapping(ThingStuffPair pair)
        {
            if (!lgps.Any())
            {
                return false;
            }
            for (int i = 0; i < pair.thing.apparel.layers.Count; i++)
            {
                ApparelLayerDef apparelLayerDef = pair.thing.apparel.layers[i];
                BodyPartGroupDef[] interferingBodyPartGroups = pair.thing.apparel.GetInterferingBodyPartGroups(body);
                for (int j = 0; j < interferingBodyPartGroups.Length; j++)
                {
                    if (lgps.Contains(new ApparelUtility.LayerGroupPair(apparelLayerDef, interferingBodyPartGroups[j])))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void AddToWorkingSet(ThingStuffPair pair)
        {
            aps.Add(pair);
            for (int i = 0; i < pair.thing.apparel.layers.Count; i++)
            {
                ApparelLayerDef apparelLayerDef = pair.thing.apparel.layers[i];
                BodyPartGroupDef[] interferingBodyPartGroups = pair.thing.apparel.GetInterferingBodyPartGroups(body);
                for (int j = 0; j < interferingBodyPartGroups.Length; j++)
                {
                    lgps.Add(new ApparelUtility.LayerGroupPair(apparelLayerDef, interferingBodyPartGroups[j]));
                }
            }
        }

        private bool Covers(BodyPartGroupDef bp)
        {
            for (int i = 0; i < aps.Count; i++)
            {
                if ((bp != BodyPartGroupDefOf.Legs || !aps[i].thing.apparel.legsNakedUnlessCoveredBySomethingElse) && aps[i].thing.apparel.bodyPartGroups.Contains(bp))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
