namespace RimMisc
{
    //[HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    //internal class Patch_MakeDowned
    //{
    //    private static bool Prefix(Pawn_HealthTracker __instance, Pawn ___pawn)
    //    {
    //        var pawn = ___pawn;
    //        if (RimMisc.Settings.killDownedPawns &&
    //            pawn.Faction != null &&
    //            !pawn.Faction.IsPlayer &&
    //            !pawn.IsPrisonerOfColony &&
    //            pawn.Faction.HostileTo(Faction.OfPlayer) &&
    //            !pawn.IsOnHoldingPlatform
    //            )
    //        {
    //            pawn.Kill(null);
    //            return false;
    //        }

    //        return true;
    //    }
    //}
}
