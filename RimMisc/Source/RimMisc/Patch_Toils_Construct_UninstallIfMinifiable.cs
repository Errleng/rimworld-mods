//using HarmonyLib;
//using RimWorld;
//using Verse;
//using Verse.AI;

//namespace RimMisc
//{
//    [HarmonyPatch(typeof(Toils_Construct), "UninstallIfMinifiable")]
//    class Patch_Toils_Construct_UninstallIfMinifiable
//    {
//        private static Toil Postfix(Toil toil)
//        {
//            if (RimMisc.Settings.disableEnemyUninstall)
//            {
//                var oldInitAction = toil.initAction;
//                toil.initAction = () =>
//                {
//                    var actor = toil.actor;
//                    var curDriver = actor.jobs.curDriver;
//                    //Log.Message($"initAction actor: {actor.Name} is a colonist: {actor.IsColonistPlayerControlled}");
//                    if (actor.IsColonistPlayerControlled)
//                        oldInitAction();
//                    else
//                        curDriver.ReadyForNextToil();
//                };
//                //var oldTickAction = toil.tickAction;
//                //toil.tickAction = () =>
//                //{
//                //    var actor = toil.actor;
//                //    var curDriver = actor.jobs.curDriver;
//                //    //Log.Message($"tickAction actor: {actor.Name} is a colonist: {actor.IsColonistPlayerControlled}");
//                //    if (actor.IsColonistPlayerControlled)
//                //        oldTickAction();
//                //    else
//                //        curDriver.ReadyForNextToil();
//                //};
//            }

//            return toil;
//        }
//    }
//}

