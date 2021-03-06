﻿using System;
using System.Collections.Generic;
using System.Linq;

using Verse;

namespace Rimternet
{

    public class HelpCategoryDef : Def
    {

        #region XML Data

        public string ModName;

        #endregion

        [Unsaved]

        #region Instance Data

        readonly List<HelpDef> _cachedHelpDefs = new List<HelpDef>();

        public string keyDef;

        #endregion

        #region Query State

        public List<HelpDef> HelpDefs
        {
            get
            {
                return _cachedHelpDefs;
            }
        }

        public bool ShouldDraw { get; set; }

        public bool Expanded { get; set; }

        public bool MatchesFilter(string filter)
        {
            return filter == "" || LabelCap.ToString().ToUpper().Contains(filter.ToUpper());
        }

        public bool ThisOrAnyChildMatchesFilter(string filter)
        {
            return MatchesFilter(filter) || HelpDefs.Any(hd => hd.MatchesFilter(filter));
        }

        public void Filter(string filter, bool force = false)
        {
            ShouldDraw = force || ThisOrAnyChildMatchesFilter(filter);
            Expanded = filter != "" && (force || ThisOrAnyChildMatchesFilter(filter));
            foreach (HelpDef hd in HelpDefs)
            {
                hd.Filter(filter, force || MatchesFilter(filter));
            }
        }



        #endregion

        #region Process State

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            Recache();
        }

        public void Recache()
        {
            _cachedHelpDefs.Clear();
            foreach (var def in (
                from t in DefDatabase<HelpDef>.AllDefs
                where t.category == this
                select t))
            {
                _cachedHelpDefs.Add(def);
            }
            _cachedHelpDefs.Sort();

        }

        #endregion

    }

}
