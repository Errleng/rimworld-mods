﻿using System;
using System.Collections.Generic;
using System.Text;

using Verse;

namespace Rimternet
{

    public class HelpDef : Def, IComparable
    {

        #region XML Data

        public HelpCategoryDef category;

        #endregion

        [Unsaved]

        #region Instance Data

        public Def keyDef;
        public Def secondaryKeyDef;

        #endregion

        #region Process State

        public int CompareTo(object obj)
        {
            var d = obj as HelpDef;
            return
                (d != null)
                ? string.Compare(d.label, label) * -1
                : 1;
        }

        #endregion

        #region Help details

        public List<HelpDetailSection> HelpDetailSections = new List<HelpDetailSection>();

        public string Description
        {
            get
            {
                StringBuilder s = new StringBuilder();
                s.AppendLine(description);
                foreach (HelpDetailSection section in HelpDetailSections)
                {
                    s.AppendLine(section.GetString());
                }
                return s.ToString();
            }
        }

        #endregion

        #region Filter

        public bool ShouldDraw { get; set; }

        public void Filter(string filter, bool force = false)
        {
            ShouldDraw = force || MatchesFilter(filter);
        }

        public bool MatchesFilter(string filter)
        {
            return filter == "" || LabelCap != null && LabelCap.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

    }

}
